using System.ComponentModel;
using DreamPoeBot.Loki.Common;

namespace Beasts.Configuration
{
    /// <summary>
    /// Main configuration class for BeastRoutine
    /// UPDATED: Added ComprehensiveFarming settings for simultaneous multi-target farming
    /// </summary>
    public class BeastRoutineSettings : JsonSettings
    {
        private static BeastRoutineSettings _instance;

        public static BeastRoutineSettings Instance => _instance ?? (_instance = new BeastRoutineSettings());

        public BeastRoutineSettings() : base(GetSettingsFilePath("DreamPoeBot", "BeastRoutine.json"))
        {
            // Initialize nested settings objects
            if (BeastCapture == null) BeastCapture = new BeastCaptureSettings();
            if (CacheSettings == null) CacheSettings = new CacheSettings();
            if (Combat == null) Combat = new CombatSettings();
            if (Loot == null) Loot = new LootSettings();
            if (ExitConditions == null) ExitConditions = new ExitConditions();
            if (ComprehensiveFarming == null) ComprehensiveFarming = new ComprehensiveFarmingSettings(); // NEW
            if (MapDevice == null) MapDevice = new MapDeviceSettings();
            if (Stashing == null) Stashing = new StashingSettings();
        }

        // Main settings groups
        public BeastCaptureSettings BeastCapture { get; set; }
        public CacheSettings CacheSettings { get; set; }
        public CombatSettings Combat { get; set; }
        public LootSettings Loot { get; set; }
        public ExitConditions ExitConditions { get; set; }

        // NEW: Comprehensive farming settings
        public ComprehensiveFarmingSettings ComprehensiveFarming { get; set; }

        // Map device and stashing settings
        public MapDeviceSettings MapDevice { get; set; }
        public StashingSettings Stashing { get; set; }

        // General settings
        [DefaultValue(100)]
        public int ExplorationRange { get; set; } = 100;

        [DefaultValue(true)]
        public bool EnableLogging { get; set; } = true;

        [DefaultValue(false)]
        public bool EnableDebugLogging { get; set; } = false;

        [DefaultValue(true)]
        [Description("Enable process hook manager for bot actions (required for IBot operation)")]
        public bool ShouldEnableHooks { get; set; } = true;
    }

    /// <summary>
    /// NEW: Configuration for the ComprehensiveFarmingPhase
    /// Controls simultaneous beast hunting, heist cache farming, and map clearing
    /// </summary>
    public class ComprehensiveFarmingSettings : JsonSettings
    {
        public ComprehensiveFarmingSettings() : base(GetSettingsFilePath("DreamPoeBot", "ComprehensiveFarming.json"))
        {
        }

        [DefaultValue(true)]
        [Description("Enable comprehensive farming mode (recommended)")]
        public bool Enabled { get; set; } = true;

        [DefaultValue(true)]
        [Description("Enable simultaneous beast hunting while farming")]
        public bool EnableBeastHunting { get; set; } = true;

        [DefaultValue(true)]
        [Description("Enable heist cache farming")]
        public bool EnableHeistCaches { get; set; } = true;

        [DefaultValue(true)]
        [Description("Enable proactive enemy clearing")]
        public bool EnableMapClearing { get; set; } = true;

        [DefaultValue(80f)]
        [Description("Maximum distance to travel for heist caches")]
        public float MaxCacheDistance { get; set; } = 80f;

        [DefaultValue(60f)]
        [Description("Maximum distance to travel for beasts")]
        public float MaxBeastDistance { get; set; } = 60f;

        [DefaultValue(40f)]
        [Description("Range to engage enemies proactively")]
        public float ProactiveCombatRange { get; set; } = 40f;

        [DefaultValue(150f)]
        [Description("How much extra priority heist caches get over other targets")]
        public float HeistCachePriorityBonus { get; set; } = 150f;

        [DefaultValue(100f)]
        [Description("How much extra priority ready-to-capture beasts get")]
        public float ReadyBeastPriorityBonus { get; set; } = 100f;

        [DefaultValue(5)]
        [Description("Maximum number of targets to track simultaneously")]
        public int MaxSimultaneousTargets { get; set; } = 5;

        [DefaultValue(30)]
        [Description("Seconds to spend on comprehensive farming before rescanning")]
        public int RescanInterval { get; set; } = 30;

        [DefaultValue(true)]
        [Description("Prioritize clearing enemies near valuable targets")]
        public bool ClearEnemiesNearTargets { get; set; } = true;

        [DefaultValue(25f)]
        [Description("Range to clear enemies around important targets")]
        public float TargetClearingRange { get; set; } = 25f;

        [DefaultValue(true)]
        [Description("Enable opportunistic looting while moving")]
        public bool OpportunisticLooting { get; set; } = true;

        [DefaultValue(15f)]
        [Description("Range to pick up items opportunistically")]
        public float OpportunisticLootRange { get; set; } = 15f;

        [DefaultValue(false)]
        [Description("Enable detailed logging for comprehensive farming")]
        public bool EnableDetailedLogging { get; set; } = false;
    }
    /// <summary>
    /// Map device configuration for opening maps with specific requirements
    /// </summary>
    public class MapDeviceSettings : JsonSettings
    {
        public MapDeviceSettings() : base(GetSettingsFilePath("DreamPoeBot", "MapDevice.json"))
        {
        }

        [DefaultValue("")]
        [Description("Map to use (e.g., 'Tropical Island Map', 'Strand Map'). Leave empty to use any map.")]
        public string PreferredMap { get; set; } = "";

        [DefaultValue("Heist")]
        [Description("Map receptacle type: Standard, Heist, Expedition, etc.")]
        public string ReceptacleType { get; set; } = "Heist";

        [DefaultValue(1)]
        [Description("Minimum number of Bestiary Scarab of Duplicating required")]
        public int MinDuplicatingScarabs { get; set; } = 1;

        [DefaultValue(2)]
        [Description("Minimum number of Bestiary Scarab of The Herd required")]
        public int MinHerdScarabs { get; set; } = 2;

        [DefaultValue(true)]
        [Description("Automatically restock scarabs from stash via Faustus exchange")]
        public bool AutoRestockScarabs { get; set; } = true;

        [DefaultValue(true)]
        [Description("Use Faustus currency exchange for scarab procurement")]
        public bool UseFaustusExchange { get; set; } = true;

        [DefaultValue("Chaos Orb")]
        [Description("Currency to use for Faustus exchange (default: Chaos Orb)")]
        public string FaustusCurrency { get; set; } = "Chaos Orb";

        [DefaultValue(20)]
        [Description("Minimum number of scarabs to buy per Faustus exchange session")]
        public int MinScarabsPurchasePerSession { get; set; } = 20;

        [DefaultValue(40)]
        [Description("Maximum number of scarabs to buy per Faustus exchange session")]
        public int MaxScarabsPurchasePerSession { get; set; } = 40;

        [DefaultValue(true)]
        [Description("Validate all requirements before activating map device")]
        public bool ValidateRequirements { get; set; } = true;

        [DefaultValue(5)]
        [Description("Maximum attempts to restock before giving up")]
        public int MaxRestockAttempts { get; set; } = 5;

        [DefaultValue(true)]
        [Description("Automatically withdraw maps from stash when inventory is empty")]
        public bool AutoWithdrawMaps { get; set; } = true;

        [DefaultValue(false)]
        [Description("Enable map rotation (farm multiple different map types in sequence)")]
        public bool EnableMapRotation { get; set; } = false;

        [DefaultValue("")]
        [Description("Comma-separated list of maps to rotate (e.g., 'Strand,Tropical Island,Burial Chambers'). Leave empty to use any available.")]
        public string MapRotationList { get; set; } = "";

        [DefaultValue(16)]
        [Description("Minimum map tier to use (0 = any tier, 16 = T16 only)")]
        public int MinMapTier { get; set; } = 16;

        [DefaultValue(16)]
        [Description("Maximum map tier to use (16 = highest tier)")]
        public int MaxMapTier { get; set; } = 16;
    }

    public class StashingSettings : JsonSettings
    {
        public StashingSettings() : base(GetSettingsFilePath("DreamPoeBot", "Stashing.json"))
        {
        }

        [DefaultValue(70)]
        [Description("Minimum inventory fullness % to trigger stashing")]
        public int MinInventoryFullnessPercent { get; set; } = 70;

        [DefaultValue(95)]
        [Description("Maximum inventory fullness % (emergency stash)")]
        public int MaxInventoryFullnessPercent { get; set; } = 95;

        [DefaultValue(true)]
        [Description("Deposit blueprints to Heist locker instead of stash")]
        public bool UsHeistLockerForBlueprints { get; set; } = true;

        [DefaultValue(true)]
        [Description("Automatically restock maps, scarabs, and consumables")]
        public bool AutoRestock { get; set; } = true;

        [DefaultValue(1)]
        [Description("Minimum portal scrolls to maintain (will trigger restocking)")]
        public int MinPortalScrolls { get; set; } = 1;

        [DefaultValue(40)]
        [Description("Target portal scrolls to restock to (will withdraw this many from stash)")]
        public int TargetPortalScrolls { get; set; } = 40;

        [DefaultValue(true)]
        [Description("Automatically restock portal scrolls from stash")]
        public bool AutoRestockPortalScrolls { get; set; } = true;
    }
}
