using System.ComponentModel;

namespace Beasts.Configuration
{
    /// <summary>
    /// Settings for combat behavior
    /// NOTE: Skill configuration has been moved to BeastCombatRoutine and SkillPlayerMover
    /// This class now only contains flask and defensive settings used by BeastRoutine phases
    /// </summary>
    public class CombatSettings
    {
        // Combat range (used by phases to determine engagement distance)
        [DefaultValue(60)]
        public int CombatRange { get; set; } = 60;

        // Skill ranges (used by phases for positioning)
        [DefaultValue(30)]
        public int MaxMeleeRange { get; set; } = 30;

        [DefaultValue(60)]
        public int MaxRangeRange { get; set; } = 60;

        // Skill slots
        [DefaultValue(0)]
        public int PrimarySkillSlot { get; set; } = 0;

        [DefaultValue(1)]
        public int MovementSkillSlot { get; set; } = 1;

        [DefaultValue(0)]
        public int SecondaryMovementSkillSlot { get; set; } = 0;

        // Attack behavior
        [DefaultValue(false)]
        public bool AttackWhileMoving { get; set; } = false;

        [DefaultValue(5)]
        public int MaxAttacksBeforeMove { get; set; } = 5;

        [DefaultValue(false)]
        public bool AlwaysAttackInPlace { get; set; } = false;

        [DefaultValue(false)]
        public bool HoldAttackButton { get; set; } = false;

        // Movement skill settings
        [DefaultValue(true)]
        public bool UseMovementSkill { get; set; } = true;

        [DefaultValue(40)]
        public int MinMovementSkillDistance { get; set; } = 40;

        [DefaultValue(100)]
        public int MinMovementSkillDelayMs { get; set; } = 100;

        // Fast combat mode
        [DefaultValue(true)]
        public bool EnableFastCombatMode { get; set; } = true;

        [DefaultValue(50)]
        public int MinAttackDelayMs { get; set; } = 50;

        // Defensive settings
        [DefaultValue(30)]
        public int KiteHealthThreshold { get; set; } = 30;

        [DefaultValue(false)]
        public bool EnableKiting { get; set; } = false;

        // Flask usage
        [DefaultValue(true)]
        public bool UseFlasksInCombat { get; set; } = true;

        [DefaultValue(50)]
        public int UseLifeFlaskPercent { get; set; } = 50;

        [DefaultValue(30)]
        public int UseManaFlaskPercent { get; set; } = 30;

        [DefaultValue(true)]
        [Description("Use utility flasks during combat (Quicksilver, Granite, Jade, etc.)")]
        public bool UseUtilityFlasks { get; set; } = true;

        [DefaultValue(true)]
        [Description("Use Quicksilver Flask for movement speed")]
        public bool UseQuicksilverFlask { get; set; } = true;

        [DefaultValue(true)]
        [Description("Use defensive flasks (Granite, Jade, Basalt, etc.)")]
        public bool UseDefensiveFlasks { get; set; } = true;

        [DefaultValue(60)]
        [Description("Use defensive flasks when health below this percent")]
        public int UseDefensiveFlaskPercent { get; set; } = 60;

        // Offensive flask usage (for beast/boss fights)
        [DefaultValue(true)]
        [Description("Use offensive flasks when fighting beasts/bosses (Diamond, Silver, Sulphur, etc.)")]
        public bool UseOffensiveFlasks { get; set; } = true;

        [DefaultValue(true)]
        [Description("Use offensive flasks on rare+ monsters")]
        public bool UseOffensiveFlasksOnRares { get; set; } = true;

        [DefaultValue(true)]
        [Description("Use offensive flasks on unique/boss monsters")]
        public bool UseOffensiveFlasksOnBosses { get; set; } = true;

        // Tincture usage (Rosethorn Tincture, etc.)
        [DefaultValue(true)]
        [Description("Use tincture during combat (Rosethorn Tincture, etc.)")]
        public bool UseTincture { get; set; } = true;

        [DefaultValue("5")]
        [Description("Tincture flask keybind (1-5, or custom key)")]
        public string TinctureKeybind { get; set; } = "5";

        [DefaultValue(true)]
        [Description("Use tincture on rare+ monsters")]
        public bool UseTinctureOnRares { get; set; } = true;

        [DefaultValue(true)]
        [Description("Use tincture on unique/boss monsters")]
        public bool UseTinctureOnBosses { get; set; } = true;
    }
}
