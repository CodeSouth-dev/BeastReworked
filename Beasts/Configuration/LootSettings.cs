namespace Beasts.Configuration
{
    /// <summary>
    /// Settings for loot filtering and pickup
    /// </summary>
    public class LootSettings
    {
        // Loot range
        public float MaxLootRange { get; set; } = 40f;

        // === POE.NINJA INTEGRATION ===
        
        /// <summary>
        /// Enable poe.ninja price checking for loot filtering
        /// When enabled, items must meet the MinimumChaosValue threshold to be looted
        /// </summary>
        public bool UsePoeNinjaFiltering { get; set; } = true;

        /// <summary>
        /// Minimum chaos value for an item to be picked up (when UsePoeNinjaFiltering is enabled)
        /// Default: 5 chaos (only loot items worth 5c or more)
        /// </summary>
        public double MinimumChaosValue { get; set; } = 5.0;

        /// <summary>
        /// League to use for poe.ninja price data
        /// Common values: "Standard", "Hardcore", "Settlers", "Hardcore Settlers"
        /// Leave empty to auto-detect current league (not yet implemented)
        /// </summary>
        public string PoeNinjaLeague { get; set; } = "Standard";

        // === MANDATORY LOOT (ALWAYS PICKED UP) ===
        
        /// <summary>
        /// Heist blueprints are ALWAYS looted regardless of value
        /// Cannot be disabled - this is the core purpose of the bot
        /// </summary>
        public bool PickupBlueprints { get; set; } = true; // Always true, cannot disable

        /// <summary>
        /// Heist contracts are ALWAYS looted regardless of value
        /// </summary>
        public bool PickupContracts { get; set; } = true;

        /// <summary>
        /// Rogue's Markers (Heist currency)
        /// </summary>
        public bool PickupMarkers { get; set; } = true;

        // === VALUE-BASED LOOT (FILTERED BY POE.NINJA) ===
        
        /// <summary>
        /// Pickup currency (filtered by poe.ninja value if enabled)
        /// Examples: Chaos Orbs, Divine Orbs, Exalted Orbs, etc.
        /// </summary>
        public bool PickupCurrency { get; set; } = true;

        /// <summary>
        /// Pickup unique items (filtered by poe.ninja value if enabled)
        /// Many uniques are worthless, poe.ninja filtering recommended
        /// </summary>
        public bool PickupUniques { get; set; } = true;

        /// <summary>
        /// Pickup maps (filtered by poe.ninja value if enabled)
        /// </summary>
        public bool PickupMaps { get; set; } = true;

        /// <summary>
        /// Pickup divination cards (filtered by poe.ninja value if enabled)
        /// Examples: The Doctor, House of Mirrors, Apothecary
        /// </summary>
        public bool PickupDivinationCards { get; set; } = true;

        /// <summary>
        /// Pickup rare items (NOT recommended - most rares are trash)
        /// Even with poe.ninja filtering, rares won't have accurate price data
        /// </summary>
        public bool PickupRares { get; set; } = false;

        // === FALLBACK FILTERS (WHEN POE.NINJA UNAVAILABLE) ===
        
        /// <summary>
        /// Minimum item level for rare items when PickupRares is enabled
        /// Only used as fallback when poe.ninja is unavailable
        /// </summary>
        public int MinRareItemLevel { get; set; } = 75;

        /// <summary>
        /// Always pickup currency even if poe.ninja value is unknown
        /// Recommended: true (currency is usually valuable)
        /// </summary>
        public bool AlwaysPickupUnknownCurrency { get; set; } = true;
    }
}
