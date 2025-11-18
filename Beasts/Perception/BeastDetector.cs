using System.Linq;
using System.Collections.Generic;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Configuration;
using Beasts.Core;
using Beasts.Helpers;
using log4net;

namespace Beasts.Perception
{
    /// <summary>
    /// Detects and prioritizes beasts for capture
    /// </summary>
    public class BeastDetector : IPerceptionModule
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();
        // Use the high-value whitelist from settings
        private static readonly HashSet<string> _whitelistedBeastPaths = BeastFilterSettings.HighValueBeastPaths;

        public string Name => "BeastDetector";

        public Monster BestBeast { get; private set; }

        public void Update()
        {
            if (!BeastRoutineSettings.Instance.BeastCapture.Enabled)
            {
                BestBeast = null;
                return;
            }

            // Find all nearby beasts
            var beasts = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Where(m => m != null &&
                           m.IsValid &&
                           IsBeast(m) &&
                           ShouldCapture(m) &&
                           !Blacklist.Contains(m.Id) &&
                           m.Distance < BeastRoutineSettings.Instance.BeastCapture.MaxDetectionRange)
                .OrderByDescending(m => GetBeastPriority(m))
                .ThenBy(m => m.Distance)
                .ToList();

            BestBeast = beasts.FirstOrDefault();

            if (BestBeast != null)
            {
                Log.DebugFormat("[BeastDetector] Found beast: {0} (Rarity: {1}, Distance: {2})",
                    BestBeast.Name, BestBeast.Rarity, BestBeast.Distance);
            }
        }

        /// <summary>
        /// Checks if a monster is a whitelisted high-value beast
        /// Uses metadata path matching for accurate detection
        /// </summary>
        private bool IsBeast(Monster m)
        {
            if (m.Metadata == null)
                return false;

            // Use helper to check if metadata matches any whitelisted path
            bool isWhitelisted = MetadataPathMatcher.IsMatch(m.Metadata, _whitelistedBeastPaths);
            
            if (isWhitelisted)
            {
                Log.InfoFormat("[BeastDetector] WHITELISTED BEAST FOUND: {0} (Path: {1})", m.Name, m.Metadata);
            }

            return isWhitelisted;
        }

        /// <summary>
        /// Determines if this beast should be captured based on settings
        /// </summary>
        private bool ShouldCapture(Monster m)
        {
            var settings = BeastRoutineSettings.Instance.BeastCapture;

            // First check rarity settings
            bool rarityMatch = false;
            switch (m.Rarity)
            {
                case Rarity.Unique:
                    rarityMatch = settings.CaptureUnique;
                    break;
                case Rarity.Rare:
                    rarityMatch = settings.CaptureRare;
                    break;
                case Rarity.Magic:
                    rarityMatch = settings.CaptureMagic;
                    break;
                case Rarity.Normal:
                    rarityMatch = settings.CaptureNormal;
                    break;
            }

            if (!rarityMatch)
                return false;

            // Apply filter mode logic
            var filterMode = settings.FilterMode;
            var filter = settings.Filter;

            switch (filterMode)
            {
                case BeastFilterMode.CaptureAll:
                    // Capture all beasts matching rarity settings
                    return true;

                case BeastFilterMode.Whitelist:
                    // Check exact name match first (highest priority)
                    if (filter.BeastNames.Contains(m.Name))
                    {
                        Log.InfoFormat("[BeastDetector] Beast {0} is in name whitelist - CAPTURING", m.Name);
                        return true;
                    }
                    
                    // Check if beast family/genus is in whitelist (partial name match)
                    foreach (var family in filter.BeastFamilies)
                    {
                        if (m.Name != null && m.Name.Contains(family))
                        {
                            Log.DebugFormat("[BeastDetector] Beast {0} matches family {1} - capturing", m.Name, family);
                            return true;
                        }
                    }
                    
                    // Check metadata for family matching
                    if (m.Metadata != null)
                    {
                        foreach (var family in filter.BeastFamilies)
                        {
                            if (m.Metadata.Contains(family))
                            {
                                Log.DebugFormat("[BeastDetector] Beast {0} metadata matches family {1} - capturing", m.Name, family);
                                return true;
                            }
                        }
                    }
                    
                    // Not in whitelist - skip
                    Log.DebugFormat("[BeastDetector] Beast {0} not in whitelist - skipping", m.Name);
                    return false;

                case BeastFilterMode.Blacklist:
                    // Check exact name blacklist
                    if (filter.BeastNames.Contains(m.Name))
                    {
                        Log.DebugFormat("[BeastDetector] Beast {0} is blacklisted by name - skipping", m.Name);
                        return false;
                    }
                    
                    // Check family blacklist
                    foreach (var family in filter.BeastFamilies)
                    {
                        if (m.Name != null && m.Name.Contains(family))
                        {
                            Log.DebugFormat("[BeastDetector] Beast {0} blacklisted by family {1} - skipping", m.Name, family);
                            return false;
                        }
                    }
                    
                    // Not blacklisted - capture
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Calculate priority for beast selection
        /// Higher value = higher priority
        /// </summary>
        private int GetBeastPriority(Monster m)
        {
            int priority = 0;

            // Rarity priority (base priority)
            switch (m.Rarity)
            {
                case Rarity.Unique:
                    priority += 100;
                    break;
                case Rarity.Rare:
                    priority += 50;
                    break;
                case Rarity.Magic:
                    priority += 25;
                    break;
                case Rarity.Normal:
                    priority += 10;
                    break;
            }

            // All whitelisted beasts get massive priority boost
            if (m.Metadata != null && MetadataPathMatcher.IsMatch(m.Metadata, _whitelistedBeastPaths))
            {
                priority += 500;
                Log.DebugFormat("[BeastDetector] Whitelisted beast detected: {0} (priority +500)", m.Name);
            }

            return priority;
        }

        /// <summary>
        /// Checks if a beast is ready to be captured
        /// Note: No HP threshold needed - Einhar captures automatically when killed
        /// Keeping this method for potential future use (e.g., prioritizing low HP beasts)
        /// </summary>
        public bool IsBeastReadyToCapture(Monster beast)
        {
            if (beast == null || !beast.IsAliveHostile)
                return false;

            // Simply return true if beast is valid and alive
            // Einhar will capture it when we kill it
            return beast.IsValid;
        }
    }
}

