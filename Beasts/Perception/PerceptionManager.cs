using System.Collections.Generic;
using System.Linq;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Core;
using log4net;

namespace Beasts.Perception
{
    /// <summary>
    /// Orchestrates all perception modules and builds the GameContext
    /// </summary>
    public class PerceptionManager
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        // Individual perception modules
        private readonly BeastDetector _beastDetector;
        private readonly CacheDetector _cacheDetector;
        private readonly HeistCacheDetector _heistCacheDetector;
        private readonly CombatPerception _combatPerception;
        private readonly LootPerception _lootPerception;
        private readonly PlayerPerception _playerPerception;

        // Boss tracking state (persists across ticks)
        private bool _bossKilledThisMap = false;

        public PerceptionManager()
        {
            _beastDetector = new BeastDetector();
            _cacheDetector = new CacheDetector();
            _heistCacheDetector = new HeistCacheDetector();
            _combatPerception = new CombatPerception();
            _lootPerception = new LootPerception();
            _playerPerception = new PlayerPerception();
        }

        /// <summary>
        /// Builds a complete GameContext by querying all perception modules
        /// </summary>
        public GameContext BuildContext()
        {
            // Update all perception modules
            _playerPerception.Update();
            _combatPerception.Update();
            _beastDetector.Update();
            _cacheDetector.Update();
            _heistCacheDetector.Update();
            _lootPerception.Update();

            // Detect boss
            var (bossNearby, currentBoss, inBossRoom) = DetectBoss();

            // Build context object
            var context = new GameContext
            {
                // Cache common data
                MyPosition = LokiPoe.MyPosition,
                CurrentArea = LokiPoe.CurrentWorldArea,

                // Reference to this PerceptionManager for phases that need to update shared state
                PerceptionManager = this,

                // Player state
                Player = BuildPlayerContext(),

                // Combat state with boss detection
                Combat = new CombatContext
                {
                    NearbyEnemies = _combatPerception.NearbyEnemies,
                    BestTarget = _combatPerception.BestTarget,
                    EnemyCount = _combatPerception.NearbyEnemies?.Count ?? 0,
                    BossNearby = bossNearby,
                    CurrentBoss = currentBoss,
                    InBossRoom = inBossRoom,
                    BossWasKilled = _bossKilledThisMap,
                    EnemiesNearMechanic = false // Will be set below
                },

                // Mechanic state (beasts/caches)
                Mechanics = BuildMechanicContext(),

                // Loot state
                Loot = new LootContext
                {
                    ValuableItems = _lootPerception.ValuableItems,
                    ClosestItem = _lootPerception.ValuableItems?.Count > 0 ? _lootPerception.ValuableItems[0] : null
                },

                // Navigation state
                Navigation = new NavigationContext
                {
                    CanSeeTarget = true,
                    PathDistanceToTarget = 0,
                    BlockedByDoor = false
                },

                // Map Device context
                MapDevice = new MapDeviceContext
                {
                    IsFound = false,
                    IsInRange = false,
                    IsActive = false,
                    HasMap = false,
                    HasAllScarabs = false,
                    PortalAvailable = false,
                    Position = Vector2i.Zero,
                    Distance = 0f
                }
            };

            // Check if enemies are near mechanic target
            if (context.Mechanics.CurrentTarget != null)
            {
                context.Combat.EnemiesNearMechanic = _combatPerception.AreEnemiesNear(
                    context.Mechanics.CurrentTarget.Position,
                    30f
                );
            }

            return context;
        }

        /// <summary>
        /// Reset boss tracking when entering new map
        /// </summary>
        public void OnMapEntered()
        {
            _bossKilledThisMap = false;
            Log.Info("[Perception] Boss tracking reset for new map");
        }

        /// <summary>
        /// Mark boss as killed in this map
        /// </summary>
        public void MarkBossKilled()
        {
            _bossKilledThisMap = true;
            Log.Info("[Perception] Boss marked as killed in this map");
        }

        /// <summary>
        /// Build player context with location detection
        /// </summary>
        private PlayerContext BuildPlayerContext()
        {
            var currentArea = LokiPoe.CurrentWorldArea;

            return new PlayerContext
            {
                Position = LokiPoe.MyPosition,
                Level = LokiPoe.Me.Level,
                HealthPercent = _playerPerception.HealthPercent,
                ManaPercent = _playerPerception.ManaPercent,
                EnergyShieldPercent = _playerPerception.EnergyShieldPercent,
                InventoryFull = _playerPerception.InventoryFull,
                PortalScrollCount = _playerPerception.PortalScrollCount,
                HasRequiredBuffs = true,

                // Location detection
                IsInHideout = IsInHideout(currentArea),
                IsInMap = IsInMap(currentArea),
                IsInTown = IsInTown(currentArea),

                // Map workflow items
                HasMap = HasMapInInventory(),
                HasScarabs = HasScarabsInInventory(),
                HasPortalScrolls = HasPortalScrollsInInventory(),
                HasBestiary = true,

                // Inventory tracking
                InventorySpaceUsed = _playerPerception.InventorySpaceUsed,
                InventorySpaceTotal = _playerPerception.InventorySpaceTotal,

                // Map exploration
                MapExplorationPercent = CalculateMapExplorationPercent()
            };
        }

        /// <summary>
        /// Builds mechanic context, prioritizing beasts over caches
        /// </summary>
        private MechanicContext BuildMechanicContext()
        {
            var context = new MechanicContext
            {
                BestBeast = _beastDetector.BestBeast,
                TargetBeasts = new List<Monster>(), // BeastDetector doesn't expose TargetBeasts list
                BestCache = _cacheDetector.BestCache,
                HeistCaches = _heistCacheDetector.AllHeistCaches
            };

            // Prioritize beast if available
            if (context.BestBeast != null)
            {
                context.CurrentTarget = context.BestBeast;
                context.DistanceToTarget = context.BestBeast.Distance;
                context.InRange = context.BestBeast.Distance < 20f;
                context.BeastHealthPercent = context.BestBeast.HealthPercentTotal;
                context.BeastReadyToCapture = _beastDetector.IsBeastReadyToCapture(context.BestBeast);
            }
            // Otherwise use cache
            else if (context.BestCache != null)
            {
                context.CurrentTarget = context.BestCache;
                context.DistanceToTarget = context.BestCache.Distance;
                context.InRange = context.BestCache.Distance < 10f;
            }

            return context;
        }

        /// <summary>
        /// Get heist cache detector
        /// </summary>
        public HeistCacheDetector GetHeistCacheDetector()
        {
            return _heistCacheDetector;
        }

        /// <summary>
        /// Get heist cache statistics
        /// </summary>
        public CacheStatistics GetHeistCacheStatistics()
        {
            return _heistCacheDetector.GetCacheStatistics();
        }

        /// <summary>
        /// Detect boss monsters in the current area
        /// </summary>
        private (bool bossNearby, Monster currentBoss, bool inBossRoom) DetectBoss()
        {
            var bosses = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Where(m => m.IsValid &&
                           m.IsAliveHostile &&
                           m.Rarity == Rarity.Unique)
                .OrderBy(m => m.Distance)
                .ToList();

            if (bosses.Any())
            {
                var closestBoss = bosses.First();
                bool inBossRoom = DetectBossRoom();

                Log.DebugFormat("[Perception] Boss detected: {0} (Distance: {1:F1}, Rarity: {2}, InBossRoom: {3})",
                    closestBoss.Name,
                    closestBoss.Distance,
                    closestBoss.Rarity,
                    inBossRoom);

                return (true, closestBoss, inBossRoom);
            }

            return (false, null, false);
        }

        /// <summary>
        /// Detect if player is in a boss room
        /// </summary>
        private bool DetectBossRoom()
        {
            var currentArea = LokiPoe.CurrentWorldArea;
            if (currentArea == null) return false;

            string areaName = currentArea.Name.ToLower();

            // Check for boss-specific area names
            return areaName.Contains("arena") ||
                   areaName.Contains("sanctum") ||
                   areaName.Contains("chamber") ||
                   areaName.Contains("throne") ||
                   areaName.Contains("lair") ||
                   areaName.Contains("shaper") ||
                   areaName.Contains("elder") ||
                   areaName.Contains("maven") ||
                   areaName.Contains("sirus") ||
                   areaName.Contains("aspirant") ||
                   areaName.Contains("boss");
        }

        /// <summary>
        /// Calculate approximate map exploration percentage
        /// </summary>
        private float CalculateMapExplorationPercent()
        {
            var currentArea = LokiPoe.CurrentWorldArea;
            if (currentArea == null || !IsInMap(currentArea)) return 0f;

            // TODO: Fix TerrainData access - TerrainData is a type, not an instance property
            // Need to find correct API to access revealed cells/tiles
            // Simple heuristic based on revealed tiles
            // int revealedTiles = LokiPoe.TerrainData?.RevealedCells?.Count ?? 0;
            //
            // if (revealedTiles > 800) return 80f;
            // if (revealedTiles > 600) return 60f;
            // if (revealedTiles > 400) return 40f;
            // if (revealedTiles > 200) return 20f;

            return 0f; // Placeholder until correct API is found
        }

        // Location detection helpers
        private bool IsInHideout(DatWorldAreaWrapper area)
        {
            if (area == null) return false;
            var name = area.Name?.ToLower() ?? "";
            return name.Contains("hideout");
        }

        private bool IsInMap(DatWorldAreaWrapper area)
        {
            if (area == null) return false;
            var name = area.Name?.ToLower() ?? "";
            // A map is an area that is not a town or hideout
            // Check for common town names
            return !IsInTown(area) && !IsInHideout(area);
        }

        private bool IsInTown(DatWorldAreaWrapper area)
        {
            if (area == null) return false;
            var name = area.Name?.ToLower() ?? "";
            // Check for known town names
            return name.Contains("lioneye") ||
                   name.Contains("encampment") ||
                   name.Contains("highgate") ||
                   name.Contains("oriath") ||
                   name.Contains("karui shores") ||
                   name.Contains("bridge") ||
                   name.Contains("overseer's tower");
        }

        // Inventory detection helpers
        private bool HasMapInInventory()
        {
            try
            {
                var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
                if (inventory == null)
                    return false;

                return inventory.Items.Any(item =>
                    item != null &&
                    item.IsValid &&
                    item.Class != null &&
                    item.Class.ToLower().Contains("map"));
            }
            catch (System.Exception ex)
            {
                Log.Error($"[PerceptionManager] Error checking for maps: {ex.Message}");
                return false;
            }
        }

        private bool HasScarabsInInventory()
        {
            try
            {
                var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
                if (inventory == null)
                    return false;

                return inventory.Items.Any(item =>
                    item != null &&
                    item.IsValid &&
                    item.Name != null &&
                    item.Name.ToLower().Contains("scarab"));
            }
            catch (System.Exception ex)
            {
                Log.Error($"[PerceptionManager] Error checking for scarabs: {ex.Message}");
                return false;
            }
        }

        private bool HasPortalScrollsInInventory()
        {
            try
            {
                var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
                if (inventory == null)
                    return false;

                return inventory.Items.Any(item =>
                    item != null &&
                    item.IsValid &&
                    item.Metadata != null &&
                    (item.Metadata.ToLower().Contains("portalscroll") ||
                     item.Name?.ToLower().Contains("portal scroll") == true));
            }
            catch (System.Exception ex)
            {
                Log.Error($"[PerceptionManager] Error checking for portal scrolls: {ex.Message}");
                return false;
            }
        }
    }
}
