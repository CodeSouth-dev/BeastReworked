using System.Collections.Generic;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;

namespace Beasts.Core
{
    public class GameContext
    {
        public PlayerContext Player { get; set; }
        public CombatContext Combat { get; set; }
        public MechanicContext Mechanics { get; set; }
        public LootContext Loot { get; set; }
        public NavigationContext Navigation { get; set; }
        public MapDeviceContext MapDevice { get; set; }
        public Vector2i MyPosition { get; set; }
        public DatWorldAreaWrapper CurrentArea { get; set; }

        // Reference to PerceptionManager for phases that need to update shared state
        // (e.g., KillBossPhase marking boss as killed)
        public object PerceptionManager { get; set; }
    }

    public class PlayerContext
    {
        public Vector2i Position { get; set; }
        public int Level { get; set; }
        public float HealthPercent { get; set; }
        public float ManaPercent { get; set; }
        public float EnergyShieldPercent { get; set; }
        public bool HasRequiredBuffs { get; set; }
        public bool InventoryFull { get; set; }
        public int PortalScrollCount { get; set; }
        public bool IsInHideout { get; set; }
        public bool IsInMap { get; set; }
        public bool IsInTown { get; set; }
        public bool HasMap { get; set; }
        public bool HasScarabs { get; set; }
        public bool HasPortalScrolls { get; set; }
        public bool HasBestiary { get; set; }
        public int InventorySpaceUsed { get; set; }
        public int InventorySpaceTotal { get; set; }
        public float InventoryFullnessPercent => InventorySpaceTotal > 0 
            ? (float)InventorySpaceUsed / InventorySpaceTotal * 100f 
            : 0f;
        public float MapExplorationPercent { get; set; }
    }

    /// <summary>Combat situation information</summary>
    public class CombatContext
    {
        public List<Monster> NearbyEnemies { get; set; }
        public Monster BestTarget { get; set; }
        public int EnemyCount { get; set; }
        public bool AnyEnemiesNearby => EnemyCount > 0;
        public bool EnemiesNearMechanic { get; set; }
        public bool BossNearby { get; set; }
        public Monster CurrentBoss { get; set; }
        public bool InBossRoom { get; set; }
        public bool BossWasKilled { get; set; }
    }

    public class MechanicContext
    {
        public Monster BestBeast { get; set; }
        public List<Monster> TargetBeasts { get; set; }
        public bool BeastNearby => BestBeast != null;
        public bool BeastReadyToCapture { get; set; }
        public float BeastHealthPercent { get; set; }
        public Chest BestCache { get; set; }
        public bool CacheNearby => BestCache != null;
        public List<Chest> HeistCaches { get; set; }
        public bool HeistCachesNearby => HeistCaches != null && HeistCaches.Count > 0;
        public NetworkObject CurrentTarget { get; set; }
        public bool HasTarget => CurrentTarget != null;
        public bool InRange { get; set; }
        public float DistanceToTarget { get; set; }
    }

    public class LootContext
    {
        public List<WorldItem> ValuableItems { get; set; }
        public bool ValuableItemsNearby => ValuableItems != null && ValuableItems.Count > 0;
        public WorldItem ClosestItem { get; set; }
    }

    public class NavigationContext
    {
        public bool CanSeeTarget { get; set; }
        public float PathDistanceToTarget { get; set; }
        public bool BlockedByDoor { get; set; }
    }

    public class MapDeviceContext
    {
        public bool IsFound { get; set; }
        public bool IsInRange { get; set; }
        public bool IsActive { get; set; }
        public bool HasMap { get; set; }
        public bool HasAllScarabs { get; set; }
        public bool PortalAvailable { get; set; }
        public Vector2i Position { get; set; }
        public float Distance { get; set; }
    }
}
