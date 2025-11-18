using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using SimpleMapBot.Configuration;
using SimpleMapBot.Core;
using SimpleMapBot.Services;
using log4net;

namespace SimpleMapBot.Tasks
{
    public class ExploreAndClearTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private readonly Stopwatch _mapTimer = new Stopwatch();
        private Vector2i _startPosition = Vector2i.Zero;
        private readonly HashSet<Vector2i> _exploredPositions = new HashSet<Vector2i>();

        public string Name => "ExploreAndClearTask";
        public string Description => "Explores map, kills monsters, loots items";
        public bool IsEnabled => true;

        public async Task<bool> Run()
        {
            var settings = SimpleMapBotSettings.Instance;
            var cwa = LokiPoe.CurrentWorldArea;

            // Only run in maps (not hideout or town)
            if (cwa.IsHideoutArea || cwa.IsTown)
            {
                _mapTimer.Stop();
                _mapTimer.Reset();
                _exploredPositions.Clear();
                return false;
            }

            // Start timer on first run
            if (!_mapTimer.IsRunning)
            {
                _mapTimer.Restart();
                _startPosition = LokiPoe.MyPosition;
                Log.InfoFormat("[ExploreAndClearTask] Starting exploration in {0}", cwa.Name);
            }

            // Check if we should leave
            if (ShouldLeaveMap(settings))
            {
                Log.Info("[ExploreAndClearTask] Map complete, ready to leave");
                _mapTimer.Stop();
                return false;
            }

            // Priority order:
            // 1. Loot nearby items
            // 2. Kill nearby enemies (if combat enabled)
            // 3. Explore new areas

            // 1. Loot
            if (settings.EnableLooting)
            {
                if (await HandleLooting())
                    return true; // Continue looting
            }

            // 2. Combat
            if (settings.EnableCombat)
            {
                if (await HandleCombat(settings))
                    return true; // Continue fighting
            }

            // 3. Explore
            await HandleExploration(settings);

            return true;
        }

        private bool ShouldLeaveMap(SimpleMapBotSettings settings)
        {
            // Update exploration percentage estimate
            MapState.ExplorationPercent = Math.Min(100, (_exploredPositions.Count * 100) / 150);

            // Time limit reached - mark complete
            if (_mapTimer.Elapsed.TotalSeconds >= settings.MaxMapTimeSeconds)
            {
                Log.InfoFormat("[ExploreAndClearTask] Time limit reached ({0}s)", settings.MaxMapTimeSeconds);
                MapState.MapComplete = true;
                Statistics.Instance.OnMapCompleted();
                return true;
            }

            // Monsters remaining threshold reached - mark complete
            int monstersRemaining = LokiPoe.InstanceInfo.MonstersRemaining;
            if (monstersRemaining >= 0 && monstersRemaining <= settings.MonstersRemainingThreshold)
            {
                Log.InfoFormat("[ExploreAndClearTask] Monsters remaining threshold reached ({0}/{1})",
                    monstersRemaining, settings.MonstersRemainingThreshold);
                MapState.MapComplete = true;
                Statistics.Instance.OnMapCompleted();
                return true;
            }

            // Inventory full - leave but DON'T mark complete (will return)
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory != null && settings.ReturnWhenInventoryFull)
            {
                int totalSlots = inventory.Width * inventory.Height;
                int usedSlots = inventory.AvailableItems.Count;
                int freeSlots = totalSlots - usedSlots;

                if (freeSlots <= 3)
                {
                    Log.Info("[ExploreAndClearTask] Inventory nearly full - will return after banking");
                    // Don't set MapComplete - we want to return!
                    return true;
                }
            }

            // Low health - leave but DON'T mark complete
            if (LokiPoe.Me.HealthPercent < settings.ReturnHealthPercent)
            {
                Log.InfoFormat("[ExploreAndClearTask] Low health ({0}%) - will return after healing", LokiPoe.Me.HealthPercent);
                // Don't set MapComplete - we want to return!
                return true;
            }

            // Exploration target reached - mark complete
            if (_exploredPositions.Count > 100) // Arbitrary threshold
            {
                // Check if there are any nearby unexplored areas
                bool hasNearbyUnexplored = false;
                var myPos = LokiPoe.MyPosition;

                for (int x = -50; x <= 50; x += 10)
                {
                    for (int y = -50; y <= 50; y += 10)
                    {
                        var testPos = myPos + new Vector2i(x, y);
                        if (ExilePather.IsWalkable(testPos) && !_exploredPositions.Contains(testPos))
                        {
                            hasNearbyUnexplored = true;
                            break;
                        }
                    }
                    if (hasNearbyUnexplored) break;
                }

                if (!hasNearbyUnexplored)
                {
                    Log.Info("[ExploreAndClearTask] No more areas to explore - map complete");
                    MapState.MapComplete = true;
                    Statistics.Instance.OnMapCompleted();
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> HandleLooting()
        {
            var settings = SimpleMapBotSettings.Instance;

            // Find nearby loot
            var loot = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
                .Where(wi => wi != null && wi.IsValid &&
                           wi.Distance < settings.MaxLootRange &&
                           IsItemValuable(wi.Item, settings))
                .OrderBy(wi => wi.Distance)
                .FirstOrDefault();

            if (loot == null)
                return false;

            Log.InfoFormat("[ExploreAndClearTask] Looting: {0} at {1:F1}m", loot.Item.Name, loot.Distance);

            // Move to loot
            if (loot.Distance > 10)
            {
                PlayerMoverManager.Current.MoveTowards(loot.Position);
                await Coroutine.Sleep(50);
                return true;
            }

            // Pick up
            if (await Coroutines.InteractWith(loot))
            {
                // Track statistics
                Statistics.Instance.OnItemLooted();

                if (loot.Item.Class == "Currency")
                    Statistics.Instance.OnCurrencyLooted();
                else if (loot.Item.Class == "Maps")
                    Statistics.Instance.OnMapLooted();

                await Coroutine.Sleep(100);
            }

            return true;
        }

        private bool IsItemValuable(Item item, SimpleMapBotSettings settings)
        {
            if (item == null)
                return false;

            var itemName = item.Name ?? item.FullName ?? "Unknown";

            // Always pick up currency (unless disabled)
            if (settings.PickupCurrency && item.Class == "Currency")
                return true;

            // Always pick up divination cards (unless disabled)
            if (settings.PickupDivinationCards && item.Class == "Divination Card")
            {
                // If using poe.ninja, check value
                if (settings.UsePoeNinjaFiltering)
                {
                    var value = PoeNinjaService.GetItemValue(itemName, item.Class);
                    if (value.HasValue && value.Value >= settings.MinItemValueChaos)
                    {
                        if (settings.DebugLogging)
                        {
                            Log.InfoFormat("[ExploreAndClearTask] Valuable card: {0} ({1}c)", itemName, value.Value);
                        }
                        return true;
                    }
                    // Skip low-value cards if poe.ninja is enabled
                    return false;
                }
                // If not using poe.ninja, pick up all cards
                return true;
            }

            // Always pick up maps (unless disabled)
            if (settings.PickupMaps && item.Class == "Maps")
                return true;

            // Check poe.ninja for other valuable items
            if (settings.UsePoeNinjaFiltering)
            {
                var value = PoeNinjaService.GetItemValue(itemName, item.Class);
                if (value.HasValue && value.Value >= settings.MinItemValueChaos)
                {
                    if (settings.DebugLogging)
                    {
                        Log.InfoFormat("[ExploreAndClearTask] Valuable item: {0} ({1}c)", itemName, value.Value);
                    }
                    return true;
                }
            }

            // If using game filter, check if highlighted
            if (settings.OnlyPickupHighlightedItems)
            {
                // Game marks valuable items - check if allocated
                return item.IsAllocatedToMe;
            }

            return false;
        }

        private async Task<bool> HandleCombat(SimpleMapBotSettings settings)
        {
            // Find nearby enemies
            var enemy = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Where(m => m.IsAliveHostile && m.Distance < settings.CombatLeashRange)
                .OrderBy(m => m.Distance)
                .FirstOrDefault();

            if (enemy == null)
                return false;

            // Combat routine will handle the actual fighting
            // Just ensure we're in range
            if (enemy.Distance > 30)
            {
                PlayerMoverManager.Current.MoveTowards(enemy.Position);
                await Coroutine.Sleep(50);
            }

            // Let combat routine handle the rest
            await Coroutine.Sleep(50);
            return false; // Don't block exploration
        }

        private async Task HandleExploration(SimpleMapBotSettings settings)
        {
            var myPos = LokiPoe.MyPosition;

            // Mark current position as explored
            _exploredPositions.Add(myPos);

            // Find unexplored direction
            Vector2i targetPos = FindUnexploredPosition(myPos, settings.ExplorationRadius);

            if (targetPos != Vector2i.Zero)
            {
                if (settings.DebugLogging)
                {
                    Log.InfoFormat("[ExploreAndClearTask] Moving to explore: {0}", targetPos);
                }

                PlayerMoverManager.Current.MoveTowards(targetPos);
                await Coroutine.Sleep(50);
            }
            else
            {
                // No unexplored areas nearby, try random direction
                int angle = LokiPoe.Random.Next(0, 360);
                int distance = settings.ExplorationRadius;
                int x = (int)(distance * Math.Cos(angle * Math.PI / 180));
                int y = (int)(distance * Math.Sin(angle * Math.PI / 180));
                targetPos = myPos + new Vector2i(x, y);

                PlayerMoverManager.Current.MoveTowards(targetPos);
                await Coroutine.Sleep(50);
            }
        }

        private Vector2i FindUnexploredPosition(Vector2i fromPos, int maxDistance)
        {
            // Spiral search pattern
            for (int dist = 20; dist <= maxDistance; dist += 10)
            {
                for (int angle = 0; angle < 360; angle += 45)
                {
                    int x = (int)(dist * Math.Cos(angle * Math.PI / 180));
                    int y = (int)(dist * Math.Sin(angle * Math.PI / 180));
                    var testPos = fromPos + new Vector2i(x, y);

                    if (ExilePather.IsWalkable(testPos) && !_exploredPositions.Contains(testPos))
                    {
                        return testPos;
                    }
                }
            }

            return Vector2i.Zero;
        }
    }
}
