using System;
using System.Collections.Generic;
using System.Linq;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Configuration;
using Beasts.Core;
using Beasts.Services;
using log4net;

namespace Beasts.Perception
{
    /// <summary>
    /// Detects and prioritizes loot on the ground
    /// </summary>
    public class LootPerception : IPerceptionModule
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();
        private static bool _poeNinjaInitialized = false;

        public string Name => "LootPerception";

        public List<WorldItem> ValuableItems { get; private set; }

        public void Update()
        {
            // Initialize poe.ninja service on first update
            if (!_poeNinjaInitialized)
            {
                InitializePoeNinja();
                _poeNinjaInitialized = true;
            }

            var lootRange = BeastRoutineSettings.Instance.Loot.MaxLootRange;

            // Find valuable items on ground
            ValuableItems = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
                .Where(wi => wi != null &&
                            wi.IsValid &&
                            IsValuable(wi) &&
                            !Blacklist.Contains(wi.Id) &&
                            wi.Distance < lootRange)
                .OrderByDescending(wi => GetItemValue(wi))
                .ThenBy(wi => wi.Distance)
                .ToList();
        }

        private void InitializePoeNinja()
        {
            var settings = BeastRoutineSettings.Instance.Loot;
            if (settings.UsePoeNinjaFiltering)
            {
                var league = string.IsNullOrEmpty(settings.PoeNinjaLeague) ? "Standard" : settings.PoeNinjaLeague;
                Log.InfoFormat("[LootPerception] Initializing poe.ninja service for league: {0}", league);
                PoeNinjaService.Initialize(league);
            }
        }

        /// <summary>
        /// Determines if an item is valuable enough to pick up
        /// Uses poe.ninja API for price checking when enabled
        /// </summary>
        private bool IsValuable(WorldItem wi)
        {
            var item = wi.Item;
            if (item == null)
                return false;

            var settings = BeastRoutineSettings.Instance.Loot;
            var itemName = item.Name ?? item.FullName ?? "Unknown";

            // === MANDATORY LOOT (ALWAYS PICKED UP) ===
            
            // Blueprints are ALWAYS looted (core purpose of the bot)
            if (item.Metadata != null && item.Metadata.Contains("HeistBlueprint"))
            {
                if (settings.PickupBlueprints)
                {
                    Log.DebugFormat("[LootPerception] Blueprint (mandatory): {0}", itemName);
                    return true;
                }
            }

            // Contracts are ALWAYS looted
            if (item.Metadata != null && item.Metadata.Contains("HeistContract"))
            {
                if (settings.PickupContracts)
                {
                    Log.DebugFormat("[LootPerception] Contract (mandatory): {0}", itemName);
                    return true;
                }
            }

            // Rogue's Markers (Heist currency)
            if (item.Metadata != null && item.Metadata.Contains("HeistCurrency"))
            {
                if (settings.PickupMarkers)
                {
                    Log.DebugFormat("[LootPerception] Markers (mandatory): {0}", itemName);
                    return true;
                }
            }

            // === POE.NINJA FILTERED LOOT ===
            
            if (settings.UsePoeNinjaFiltering)
            {
                // Check poe.ninja value
                double? chaosValue = PoeNinjaService.GetItemValue(itemName, item.Class);

                if (chaosValue.HasValue)
                {
                    bool isValuable = chaosValue.Value >= settings.MinimumChaosValue;
                    
                    if (isValuable)
                    {
                        Log.InfoFormat("[LootPerception] Valuable item ({0}c): {1}", 
                            Math.Round(chaosValue.Value, 2), itemName);
                    }
                    else
                    {
                        Log.DebugFormat("[LootPerception] Low value ({0}c < {1}c): {2}", 
                            Math.Round(chaosValue.Value, 2), settings.MinimumChaosValue, itemName);
                    }
                    
                    return isValuable;
                }
                else
                {
                    // Item not found in poe.ninja - use fallback logic
                    Log.DebugFormat("[LootPerception] Unknown value (not in poe.ninja): {0}", itemName);
                    return FallbackValueCheck(item, itemName, settings);
                }
            }
            else
            {
                // Poe.ninja filtering disabled - use basic filters
                return BasicValueCheck(item, settings);
            }
        }

        /// <summary>
        /// Fallback value check when item not found in poe.ninja
        /// </summary>
        private bool FallbackValueCheck(Item item, string itemName, LootSettings settings)
        {
            // Currency: Always pick up unknown currency (usually valuable)
            if ((item.Class == "Currency" || item.Class == "StackableCurrency") && 
                settings.AlwaysPickupUnknownCurrency)
            {
                Log.InfoFormat("[LootPerception] Unknown currency (fallback pickup): {0}", itemName);
                return true;
            }

            // For other items, use basic rarity-based filtering as fallback
            // This is conservative - only pick up things that are likely valuable

            if (item.Rarity == Rarity.Unique && settings.PickupUniques)
            {
                // Unknown unique - might be valuable, pick it up
                Log.DebugFormat("[LootPerception] Unknown unique (fallback pickup): {0}", itemName);
                return true;
            }

            // Don't pick up other items without price data (too risky)
            return false;
        }

        /// <summary>
        /// Basic value check when poe.ninja filtering is disabled
        /// </summary>
        private bool BasicValueCheck(Item item, LootSettings settings)
        {
            // Currency
            if ((item.Class == "Currency" || item.Class == "StackableCurrency") && settings.PickupCurrency)
                return true;

            // Rare items
            if (item.Rarity == Rarity.Rare && settings.PickupRares)
            {
                // Check ilvl if specified
                if (item.ItemLevel >= settings.MinRareItemLevel)
                    return true;
            }

            // Unique items
            if (item.Rarity == Rarity.Unique && settings.PickupUniques)
                return true;

            // Maps
            if (item.Class == "Map" && settings.PickupMaps)
                return true;

            // Divination cards
            if (item.Class == "DivinationCard" && settings.PickupDivinationCards)
                return true;

            return false;
        }

        /// <summary>
        /// Calculates item value for prioritization
        /// Uses poe.ninja prices when available, falls back to rarity-based system
        /// </summary>
        private int GetItemValue(WorldItem wi)
        {
            var item = wi.Item;
            var settings = BeastRoutineSettings.Instance.Loot;
            var itemName = item.Name ?? item.FullName ?? "Unknown";

            // Blueprints and contracts are ALWAYS highest priority
            if (item.Metadata != null && item.Metadata.Contains("HeistBlueprint"))
                return 10000; // Blueprints are primary target

            if (item.Metadata != null && item.Metadata.Contains("HeistContract"))
                return 9000; // Contracts second priority

            // Use poe.ninja value if available
            if (settings.UsePoeNinjaFiltering)
            {
                double? chaosValue = PoeNinjaService.GetItemValue(itemName, item.Class);
                if (chaosValue.HasValue)
                {
                    // Convert chaos value to integer priority
                    // Multiply by 10 to preserve decimal precision in sorting
                    return (int)(chaosValue.Value * 10);
                }
            }

            // Fallback to rarity-based value
            int value = 0;

            switch (item.Rarity)
            {
                case Rarity.Unique:
                    value += 100;
                    break;
                case Rarity.Rare:
                    value += 50;
                    break;
                case Rarity.Magic:
                    value += 20;
                    break;
            }

            // Special item types
            if (item.Class == "Currency" || item.Class == "StackableCurrency")
                value += 75;

            if (item.Metadata != null && item.Metadata.Contains("HeistCurrency"))
                value += 80; // Markers

            return value;
        }
    }
}
