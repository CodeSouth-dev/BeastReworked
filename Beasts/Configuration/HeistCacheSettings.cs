using System.Collections.Generic;

namespace Beasts.Configuration
{
    /// <summary>
    /// Settings for comprehensive heist cache farming
    /// </summary>
    public class HeistCacheSettings
    {
        // Enable/disable heist cache farming
        public bool Enabled { get; set; } = true;

        // Priority over beast hunting
        public bool HighPriorityMode { get; set; } = true;

        // Detection settings
        public float MaxDetectionRange { get; set; } = 120f;  // Scan entire map
        public bool ScanEntireMapOnEntry { get; set; } = true;

        // Route optimization
        public bool OptimizeRoute { get; set; } = true;
        public RouteOptimizationMode RouteMode { get; set; } = RouteOptimizationMode.NearestFirst;

        // Combat behavior around caches
        public bool ClearEnemiesBeforeOpening { get; set; } = true;
        public float EnemyClearRange { get; set; } = 25f;

        // Completion behavior
        public bool CompleteAllCachesBeforeOtherActivities { get; set; } = true;
        public bool LogCacheProgress { get; set; } = true;

        // Item priority for heist cache contents
        public HeistItemPriority ItemPriority { get; set; } = new HeistItemPriority();

        // Performance settings
        public int MaxRetryAttemptsPerCache { get; set; } = 5;
        public int MaxNavigationFailures { get; set; } = 10;
        public float InteractionTimeout { get; set; } = 3.0f;

        // Cache type filtering (if we can distinguish different types)
        public bool LootLargeCaches { get; set; } = true;
        public bool LootSmallCaches { get; set; } = true;
        public bool LootSpecialCaches { get; set; } = true;

        // Skip conditions
        public bool SkipCachesNearBosses { get; set; } = false;
        public bool SkipCachesInDangerousAreas { get; set; } = false;
        public float MinDistanceFromBoss { get; set; } = 50f;
    }

    /// <summary>
    /// Route optimization strategies
    /// </summary>
    public enum RouteOptimizationMode
    {
        NearestFirst,      // Always go to nearest cache (greedy)
        ClusteredAreas,    // Group nearby caches together
        LinearSweep,       // Sweep across map systematically
        OptimalTSP         // Attempt traveling salesman optimization
    }

    /// <summary>
    /// Item priority settings for heist cache contents
    /// </summary>
    public class HeistItemPriority
    {
        // Heist-specific items (highest priority)
        public int BlueprintPriority { get; set; } = 200;
        public int ContractPriority { get; set; } = 150;
        public int MarkerPriority { get; set; } = 100;
        public int HeistGearPriority { get; set; } = 80;

        // General valuable items
        public int CurrencyPriority { get; set; } = 75;
        public int UniquePriority { get; set; } = 50;
        public int RarePriority { get; set; } = 25;
        public int DivinationCardPriority { get; set; } = 60;

        // Maps and fragments
        public int MapPriority { get; set; } = 40;
        public int FragmentPriority { get; set; } = 35;

        // Skip conditions
        public bool SkipLowValueItems { get; set; } = true;
        public int MinItemValue { get; set; } = 20; // Skip items below this priority
    }

    /// <summary>
    /// Advanced heist cache detection settings
    /// </summary>
    public class AdvancedDetectionSettings
    {
        // Metadata keywords for enhanced detection
        public HashSet<string> PrimaryKeywords { get; set; } = new HashSet<string>
        {
            "heistchest",
            "heistlargechest", 
            "heistsmallchest",
            "smugglercache",
            "smuggler"
        };

        public HashSet<string> SecondaryKeywords { get; set; } = new HashSet<string>
        {
            "contraband",
            "stash",
            "cache",
            "blueprint",
            "contract"
        };

        // Name patterns for detection
        public HashSet<string> NamePatterns { get; set; } = new HashSet<string>
        {
            "smuggler's cache",
            "smuggler cache", 
            "heist chest",
            "contraband stash"
        };

        // Exclusion patterns (things to NOT consider as heist caches)
        public HashSet<string> ExclusionPatterns { get; set; } = new HashSet<string>
        {
            "strongbox",
            "arcanist",
            "cartographer",
            "diviner"
        };
    }
}
