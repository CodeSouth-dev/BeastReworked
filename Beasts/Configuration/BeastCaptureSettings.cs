using System.Collections.Generic;

namespace Beasts.Configuration
{
    /// <summary>
    /// Settings for beast detection and capture
    /// </summary>
    public class BeastCaptureSettings
    {
        // Enable/disable beast farming
        public bool Enabled { get; set; } = true;

        // Rarity filters
        // NOTE: Red beasts = Rare, Yellow beasts = Magic
        // By default, only capture red beasts (Rare) for best value
        public bool CaptureUnique { get; set; } = true;
        public bool CaptureRare { get; set; } = true;  // Red beasts - VALUABLE
        public bool CaptureMagic { get; set; } = false; // Yellow beasts - usually not worth
        public bool CaptureNormal { get; set; } = false;

        // Detection range
        public float MaxDetectionRange { get; set; } = 80f;

        // Capture threshold (HP% required before throwing net)
        public float CaptureHealthPercent { get; set; } = 10f;

        // Beast filtering options
        // Default to Whitelist mode to only capture valuable beasts from the list
        public BeastFilterMode FilterMode { get; set; } = BeastFilterMode.Whitelist;

        // Specific beast filtering
        public BeastFilterSettings Filter { get; set; } = new BeastFilterSettings();

        // Capture priority settings
        public BeastPrioritySettings Priority { get; set; } = new BeastPrioritySettings();
    }

    /// <summary>
    /// Beast filtering mode
    /// </summary>
    public enum BeastFilterMode
    {
        CaptureAll,    // Capture all beasts matching rarity settings
        Whitelist,     // Only capture beasts in the whitelist
        Blacklist      // Capture all except beasts in the blacklist
    }

    /// <summary>
    /// Beast filtering settings
    /// </summary>
    public class BeastFilterSettings
    {
        // Beast families/genus to always capture (whitelist) or never capture (blacklist)
        public HashSet<string> BeastFamilies { get; set; } = new HashSet<string>();

        // Specific beast names to filter
        public HashSet<string> BeastNames { get; set; } = new HashSet<string>();

        // Beast tags/modifiers to filter
        public HashSet<string> BeastTags { get; set; } = new HashSet<string>();

        // High-value named beasts to capture (metadata paths for accurate detection)
        public static HashSet<string> HighValueBeastPaths => new HashSet<string>()
        {
            // Harvest Beasts - Top Tier
            "Metadata/Monsters/LeagueHarvest/Green/HarvestSquidT3MemoryLine_",  // Vivid Watcher
            "Metadata/Monsters/LeagueHarvest/Green/HarvestSquidT3_",  // Vivid Watcher (variant)
            "Metadata/Monsters/LeagueHarvest/Green/HarvestVultureParasiteT3MemoryLine",  // Vivid Vulture
            "Metadata/Monsters/LeagueHarvest/Green/HarvestVultureParasiteT3",  // Vivid Vulture (variant)
            "Metadata/Monsters/LeagueHarvest/Red/HarvestBeastT3MemoryLine_",  // Wild Bristle Matron
            "Metadata/Monsters/LeagueHarvest/Red/HarvestBeastT3",  // Wild Bristle Matron (variant)
            "Metadata/Monsters/LeagueHarvest/Red/HarvestHellionT3MemoryLine",  // Wild Hellion Alpha
            "Metadata/Monsters/LeagueHarvest/Red/HarvestHellionT3",  // Wild Hellion Alpha (variant)
            "Metadata/Monsters/LeagueHarvest/Red/HarvestBrambleHulkT3MemoryLine",  // Wild Brambleback
            "Metadata/Monsters/LeagueHarvest/Red/HarvestBrambleHulkT3",  // Wild Brambleback (variant)
            
            // Spirit Beasts - Very High Value
            "Metadata/Monsters/LeagueBestiary/TigerBestiarySpiritBoss",  // Farrul, First of the Plains
            "Metadata/Monsters/LeagueBestiary/SpiderPlatedBestiarySpiritBoss",  // Fenumus, First of the Night
            "Metadata/Monsters/LeagueBestiary/NessaCrabBestiarySpiritBoss",  // Craiceann, First of the Deep
            
            // Ultra Rare
            "Metadata/Monsters/LeagueAzmeri/GullGoliathBestiary_",  // Black Mórrigan
            
            // High Value Crafting Beasts
            "Metadata/Monsters/LeagueBestiary/GemFrogBestiary",  // Craicic Chimeral (Imprint)
            "Metadata/Monsters/LeagueBestiary/SpiderPlagueBestiary",  // Fenumal Plagued Arachnid (Split)
        };

        // Default valuable beast families (for family-based filtering)
        public static HashSet<string> DefaultValuableFamilies => new HashSet<string>()
        {
            "Rhoas",           // Popular for crafting
            "Crabs",           // Popular for crafting
            "Spiders",         // Aspect of the Spider
            "Cats",            // Aspect of the Cat
            "Birds",           // Various useful crafts
            "Apes",            // Various useful crafts
            "Bears",           // Various useful crafts
            "Goatmen",         // Various useful crafts
            "Amphibians"       // Various useful crafts
        };

        // Default beast families to avoid (for blacklist mode)
        public static HashSet<string> DefaultAvoidFamilies => new HashSet<string>()
        {
            "Skeletons",       // Usually not valuable
            "Zombies",         // Usually not valuable
            "Ghosts"           // Usually not valuable
        };

        /// <summary>
        /// Applies default settings based on filter mode
        /// </summary>
        public void ApplyDefaults(BeastFilterMode mode)
        {
            switch (mode)
            {
                case BeastFilterMode.Whitelist:
                    BeastFamilies = new HashSet<string>(DefaultValuableFamilies);
                    BeastNames.Clear(); // Names are now in HighValueBeastPaths as metadata paths
                    break;
                case BeastFilterMode.Blacklist:
                    BeastFamilies = new HashSet<string>(DefaultAvoidFamilies);
                    BeastNames.Clear(); // Don't blacklist specific names by default
                    break;
                case BeastFilterMode.CaptureAll:
                    BeastFamilies.Clear();
                    BeastNames.Clear();
                    break;
            }
        }

        /// <summary>
        /// Applies high-value beasts only configuration (your specific list)
        /// Note: High-value beasts are now defined in HighValueBeastPaths using metadata paths
        /// </summary>
        public void ApplyHighValueBeastsOnly()
        {
            BeastFamilies.Clear();
            BeastNames.Clear(); // Names are now in HighValueBeastPaths as metadata paths
            BeastTags.Clear();
        }
    }

    /// <summary>
    /// Beast priority settings for targeting
    /// </summary>
    public class BeastPrioritySettings
    {
        // Prioritize beasts based on crafting value
        public bool PrioritizeValuableCrafts { get; set; } = true;

        // Prioritize red beasts over yellow beasts
        public bool PrioritizeRedBeasts { get; set; } = true;

        // Family-specific priority weights (higher = more priority)
        public Dictionary<string, int> FamilyPriority { get; set; } = new Dictionary<string, int>
        {
            { "Rhoas", 100 },
            { "Crabs", 95 },
            { "Spiders", 90 },
            { "Cats", 85 },
            { "Birds", 80 },
            { "Apes", 75 },
            { "Bears", 70 },
            { "Goatmen", 65 },
            { "Amphibians", 60 }
        };

        // Modifier-specific priority weights
        public Dictionary<string, int> ModifierPriority { get; set; } = new Dictionary<string, int>
        {
            { "AlwaysCrits", 50 },
            { "BleedOnHit", 45 },
            { "PoisonOnHit", 40 },
            { "ElementalDamage", 35 },
            { "LifeRegeneration", 30 }
        };
    }
}
