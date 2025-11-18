using DreamPoeBot.Common;
using System.ComponentModel;

namespace SimpleMapBot.Configuration
{
    public class SimpleMapBotSettings : JsonSettings
    {
        private static SimpleMapBotSettings _instance;
        public static SimpleMapBotSettings Instance => _instance ?? (_instance = new SimpleMapBotSettings());

        public SimpleMapBotSettings() : base(GetSettingsFilePath(Configuration.Instance.Name, "SimpleMapBot.json"))
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

        #region Debug Settings
        [DefaultValue(false)]
        [Description("Enable debug logging")]
        public bool DebugLogging { get; set; } = false;

        [DefaultValue(false)]
        [Description("Dry run mode - don't actually enter maps, just test logic")]
        public bool DryRunMode { get; set; } = false;
        #endregion
    }
}
