using System.Collections.Generic;
using System.ComponentModel;
using DreamPoeBot.Loki.Common;

namespace SimpleMapBot.Configuration
{
    public class SimpleMapBotSettings : JsonSettings
    {
        private static SimpleMapBotSettings _instance;
        public static SimpleMapBotSettings Instance => _instance ?? (_instance = new SimpleMapBotSettings());

        public SimpleMapBotSettings() : base(GetSettingsFilePath("DreamPoeBot", "SimpleMapBot.json"))
        {
        }

        #region Map Settings
        [DefaultValue("Maps")]
        [Description("Name of the stash tab containing maps")]
        public string MapStashTab { get; set; } = "Maps";

        [DefaultValue(1)]
        [Description("Minimum map tier to run")]
        public int MinMapTier { get; set; } = 1;

        [DefaultValue(16)]
        [Description("Maximum map tier to run")]
        public int MaxMapTier { get; set; } = 16;

        [DefaultValue(true)]
        [Description("Run white (normal) rarity maps")]
        public bool RunWhiteMaps { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run magic (blue) rarity maps")]
        public bool RunMagicMaps { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run rare (yellow) rarity maps")]
        public bool RunRareMaps { get; set; } = true;

        [DefaultValue(false)]
        [Description("Run unique (orange) rarity maps")]
        public bool RunUniqueMaps { get; set; } = false;
        #endregion

        #region Zone Exploration
        [DefaultValue(60)]
        [Description("Maximum time to spend in a map (seconds) before returning")]
        public int MaxMapTimeSeconds { get; set; } = 60;

        [DefaultValue(30)]
        [Description("Exploration radius from starting point")]
        public int ExplorationRadius { get; set; } = 30;

        [DefaultValue(80)]
        [Description("Target percentage of zone to explore before leaving")]
        public int TargetExplorationPercent { get; set; } = 80;

        [DefaultValue(20)]
        [Description("Map is considered complete when monsters remaining drops to this value or below")]
        public int MonstersRemainingThreshold { get; set; } = 20;
        #endregion

        #region Combat Settings
        [DefaultValue(true)]
        [Description("Enable combat during mapping")]
        public bool EnableCombat { get; set; } = true;

        [DefaultValue(40)]
        [Description("Combat leash range (how far to chase enemies)")]
        public int CombatLeashRange { get; set; } = 40;
        #endregion

        #region Loot Settings
        [DefaultValue(true)]
        [Description("Pick up loot during mapping")]
        public bool EnableLooting { get; set; } = true;

        [DefaultValue(50)]
        [Description("Maximum distance to pick up loot")]
        public int MaxLootRange { get; set; } = 50;

        [DefaultValue(false)]
        [Description("Only pick up items highlighted by in-game loot filter")]
        public bool OnlyPickupHighlightedItems { get; set; } = false;

        [DefaultValue(true)]
        [Description("Pick up currency items")]
        public bool PickupCurrency { get; set; } = true;

        [DefaultValue(true)]
        [Description("Pick up divination cards")]
        public bool PickupDivinationCards { get; set; } = true;

        [DefaultValue(true)]
        [Description("Pick up maps")]
        public bool PickupMaps { get; set; } = true;

        [DefaultValue(5)]
        [Description("Minimum chaos value for items to pick up (if using value filtering)")]
        public double MinItemValueChaos { get; set; } = 5.0;

        [DefaultValue(true)]
        [Description("Use poe.ninja for price checking and loot filtering")]
        public bool UsePoeNinjaFiltering { get; set; } = true;

        [DefaultValue("Standard")]
        [Description("League name for poe.ninja price data (e.g. 'Standard', 'Settlers', 'Hardcore')")]
        public string PoeNinjaLeague { get; set; } = "Standard";
        #endregion

        #region Return Conditions
        [DefaultValue(true)]
        [Description("Return to hideout when inventory is full")]
        public bool ReturnWhenInventoryFull { get; set; } = true;

        [DefaultValue(20)]
        [Description("Return when player health drops below this percentage")]
        public int ReturnHealthPercent { get; set; } = 20;

        [DefaultValue(true)]
        [Description("Return to hideout if stuck for too long")]
        public bool ReturnIfStuck { get; set; } = true;
        #endregion

        #region Map Selection
        [DefaultValue(true)]
        [Description("Run Crater maps")]
        public bool EnableCrater { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Underground Sea maps")]
        public bool EnableUndergroundSea { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Port maps")]
        public bool EnablePort { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Tower maps")]
        public bool EnableTower { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Phantasmagoria maps")]
        public bool EnablePhantasmagoria { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Channel maps")]
        public bool EnableChannel { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Waterways maps")]
        public bool EnableWaterways { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Frozen Cabins maps")]
        public bool EnableFrozenCabins { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Silo maps")]
        public bool EnableSilo { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Toxic Sewers maps")]
        public bool EnableToxicSewers { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Atoll maps")]
        public bool EnableAtoll { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Wastepool maps")]
        public bool EnableWastepool { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Beach maps")]
        public bool EnableBeach { get; set; } = true;

        [DefaultValue(true)]
        [Description("Run Dunes maps")]
        public bool EnableDunes { get; set; } = true;
        #endregion

        #region Scarab Selection
        [DefaultValue("None")]
        [Description("Scarab slot 1")]
        public string ScarabSlot1 { get; set; } = "None";

        [DefaultValue("None")]
        [Description("Scarab slot 2")]
        public string ScarabSlot2 { get; set; } = "None";

        [DefaultValue("None")]
        [Description("Scarab slot 3")]
        public string ScarabSlot3 { get; set; } = "None";

        [DefaultValue("None")]
        [Description("Scarab slot 4")]
        public string ScarabSlot4 { get; set; } = "None";

        [DefaultValue("None")]
        [Description("Scarab slot 5")]
        public string ScarabSlot5 { get; set; } = "None";
        #endregion

        #region Debug Settings
        [DefaultValue(false)]
        [Description("Enable debug logging")]
        public bool DebugLogging { get; set; } = false;

        [DefaultValue(false)]
        [Description("Dry run mode - don't actually enter maps, just test logic")]
        public bool DryRunMode { get; set; } = false;
        #endregion

        #region Helper Methods
        /// <summary>
        /// Get list of enabled map names
        /// </summary>
        public List<string> GetEnabledMaps()
        {
            var maps = new List<string>();

            if (EnableCrater) maps.Add("Crater");
            if (EnableUndergroundSea) maps.Add("Underground Sea");
            if (EnablePort) maps.Add("Port");
            if (EnableTower) maps.Add("Tower");
            if (EnablePhantasmagoria) maps.Add("Phantasmagoria");
            if (EnableChannel) maps.Add("Channel");
            if (EnableWaterways) maps.Add("Waterways");
            if (EnableFrozenCabins) maps.Add("Frozen Cabins");
            if (EnableSilo) maps.Add("Silo");
            if (EnableToxicSewers) maps.Add("Toxic Sewers");
            if (EnableAtoll) maps.Add("Atoll");
            if (EnableWastepool) maps.Add("Wastepool");
            if (EnableBeach) maps.Add("Beach");
            if (EnableDunes) maps.Add("Dunes");

            return maps;
        }

        /// <summary>
        /// Get list of selected scarabs (excluding "None")
        /// </summary>
        public List<string> GetSelectedScarabs()
        {
            var scarabs = new List<string>();

            if (ScarabSlot1 != "None") scarabs.Add(ScarabSlot1);
            if (ScarabSlot2 != "None") scarabs.Add(ScarabSlot2);
            if (ScarabSlot3 != "None") scarabs.Add(ScarabSlot3);
            if (ScarabSlot4 != "None") scarabs.Add(ScarabSlot4);
            if (ScarabSlot5 != "None") scarabs.Add(ScarabSlot5);

            return scarabs;
        }

        /// <summary>
        /// Check if a map is enabled
        /// </summary>
        public bool IsMapEnabled(string mapName)
        {
            if (string.IsNullOrEmpty(mapName))
                return false;

            // Normalize name for comparison
            var normalized = mapName.ToLower().Replace(" ", "");

            if (normalized.Contains("crater")) return EnableCrater;
            if (normalized.Contains("undergroundsea")) return EnableUndergroundSea;
            if (normalized.Contains("port")) return EnablePort;
            if (normalized.Contains("tower")) return EnableTower;
            if (normalized.Contains("phantasmagoria")) return EnablePhantasmagoria;
            if (normalized.Contains("channel")) return EnableChannel;
            if (normalized.Contains("waterways")) return EnableWaterways;
            if (normalized.Contains("frozencabins")) return EnableFrozenCabins;
            if (normalized.Contains("silo")) return EnableSilo;
            if (normalized.Contains("toxicsewers")) return EnableToxicSewers;
            if (normalized.Contains("atoll")) return EnableAtoll;
            if (normalized.Contains("wastepool")) return EnableWastepool;
            if (normalized.Contains("beach")) return EnableBeach;
            if (normalized.Contains("dunes")) return EnableDunes;

            return false; // Unknown map, don't run it
        }
        #endregion
    }
}
