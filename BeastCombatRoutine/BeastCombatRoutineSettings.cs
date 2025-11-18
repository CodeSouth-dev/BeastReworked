using System.Collections.Generic;
using System.ComponentModel;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;
using Newtonsoft.Json;

namespace BeastCombatRoutine
{
    public class BeastCombatRoutineSettings : JsonSettings
    {
        private static BeastCombatRoutineSettings _instance;

        public static BeastCombatRoutineSettings Instance
        {
            get { return _instance ?? (_instance = new BeastCombatRoutineSettings()); }
        }

        public BeastCombatRoutineSettings()
            : base(GetSettingsFilePath(Configuration.Instance.Name, string.Format("{0}.json", "BeastCombatRoutine")))
        {
        }

        // Combat Skill Slots
        private int _singleTargetMeleeSlot;
        private int _singleTargetRangedSlot;
        private int _aoeMeleeSlot;
        private int _aoeRangedSlot;
        private int _fallbackSlot;

        // Buff Skill Slots
        private int _buffSlot1;
        private int _buffSlot2;
        private int _buffSlot3;

        // Aura Skill Slots (always-on skills like Righteous Fire, Heralds, Auras)
        private int _auraSlot1;
        private int _auraSlot2;
        private int _auraSlot3;
        private bool _enableAuraActivation;

        // Combat Settings
        private int _combatRange;
        private int _maxMeleeRange;
        private int _maxRangeRange;
        private int _aoePackSize;
        private bool _alwaysAttackInPlace;
        private bool _enableZoomMode;
        private bool _autoDetectSkills;

        /// <summary>
        /// The skill slot to use in melee range against single targets
        /// </summary>
        [DefaultValue(-1)]
        public int SingleTargetMeleeSlot
        {
            get { return _singleTargetMeleeSlot; }
            set
            {
                if (_singleTargetMeleeSlot != value)
                {
                    _singleTargetMeleeSlot = value;
                    NotifyPropertyChanged(() => SingleTargetMeleeSlot);
                }
            }
        }

        /// <summary>
        /// The skill slot to use outside melee range against single targets
        /// </summary>
        [DefaultValue(-1)]
        public int SingleTargetRangedSlot
        {
            get { return _singleTargetRangedSlot; }
            set
            {
                if (_singleTargetRangedSlot != value)
                {
                    _singleTargetRangedSlot = value;
                    NotifyPropertyChanged(() => SingleTargetRangedSlot);
                }
            }
        }

        /// <summary>
        /// The skill slot to use in melee range against packs
        /// </summary>
        [DefaultValue(-1)]
        public int AoeMeleeSlot
        {
            get { return _aoeMeleeSlot; }
            set
            {
                if (_aoeMeleeSlot != value)
                {
                    _aoeMeleeSlot = value;
                    NotifyPropertyChanged(() => AoeMeleeSlot);
                }
            }
        }

        /// <summary>
        /// The skill slot to use outside melee range against packs
        /// </summary>
        [DefaultValue(-1)]
        public int AoeRangedSlot
        {
            get { return _aoeRangedSlot; }
            set
            {
                if (_aoeRangedSlot != value)
                {
                    _aoeRangedSlot = value;
                    NotifyPropertyChanged(() => AoeRangedSlot);
                }
            }
        }

        /// <summary>
        /// The skill slot to use as a fallback if desired skill cannot be cast
        /// </summary>
        [DefaultValue(-1)]
        public int FallbackSlot
        {
            get { return _fallbackSlot; }
            set
            {
                if (_fallbackSlot != value)
                {
                    _fallbackSlot = value;
                    NotifyPropertyChanged(() => FallbackSlot);
                }
            }
        }

        /// <summary>
        /// First buff skill slot (auras, buffs, etc)
        /// </summary>
        [DefaultValue(-1)]
        public int BuffSlot1
        {
            get { return _buffSlot1; }
            set
            {
                if (_buffSlot1 != value)
                {
                    _buffSlot1 = value;
                    NotifyPropertyChanged(() => BuffSlot1);
                }
            }
        }

        /// <summary>
        /// Second buff skill slot
        /// </summary>
        [DefaultValue(-1)]
        public int BuffSlot2
        {
            get { return _buffSlot2; }
            set
            {
                if (_buffSlot2 != value)
                {
                    _buffSlot2 = value;
                    NotifyPropertyChanged(() => BuffSlot2);
                }
            }
        }

        /// <summary>
        /// Third buff skill slot
        /// </summary>
        [DefaultValue(-1)]
        public int BuffSlot3
        {
            get { return _buffSlot3; }
            set
            {
                if (_buffSlot3 != value)
                {
                    _buffSlot3 = value;
                    NotifyPropertyChanged(() => BuffSlot3);
                }
            }
        }

        /// <summary>
        /// First aura skill slot (always-on skills like Righteous Fire, Heralds, Auras)
        /// These are activated once when entering combat areas and stay on
        /// </summary>
        [DefaultValue(-1)]
        public int AuraSlot1
        {
            get { return _auraSlot1; }
            set
            {
                if (_auraSlot1 != value)
                {
                    _auraSlot1 = value;
                    NotifyPropertyChanged(() => AuraSlot1);
                }
            }
        }

        /// <summary>
        /// Second aura skill slot
        /// </summary>
        [DefaultValue(-1)]
        public int AuraSlot2
        {
            get { return _auraSlot2; }
            set
            {
                if (_auraSlot2 != value)
                {
                    _auraSlot2 = value;
                    NotifyPropertyChanged(() => AuraSlot2);
                }
            }
        }

        /// <summary>
        /// Third aura skill slot
        /// </summary>
        [DefaultValue(-1)]
        public int AuraSlot3
        {
            get { return _auraSlot3; }
            set
            {
                if (_auraSlot3 != value)
                {
                    _auraSlot3 = value;
                    NotifyPropertyChanged(() => AuraSlot3);
                }
            }
        }

        /// <summary>
        /// Enable automatic aura activation in combat areas (Righteous Fire, Heralds, etc.)
        /// </summary>
        [DefaultValue(true)]
        public bool EnableAuraActivation
        {
            get { return _enableAuraActivation; }
            set
            {
                if (_enableAuraActivation != value)
                {
                    _enableAuraActivation = value;
                    NotifyPropertyChanged(() => EnableAuraActivation);
                }
            }
        }

        /// <summary>
        /// Only attack mobs within this range
        /// </summary>
        [DefaultValue(70)]
        public int CombatRange
        {
            get { return _combatRange; }
            set
            {
                if (_combatRange != value)
                {
                    _combatRange = value;
                    NotifyPropertyChanged(() => CombatRange);
                }
            }
        }

        /// <summary>
        /// How close does a mob need to be to trigger the Melee skill
        /// </summary>
        [DefaultValue(20)]
        public int MaxMeleeRange
        {
            get { return _maxMeleeRange; }
            set
            {
                if (_maxMeleeRange != value)
                {
                    _maxMeleeRange = value;
                    NotifyPropertyChanged(() => MaxMeleeRange);
                }
            }
        }

        /// <summary>
        /// How close does a mob need to be to trigger the Ranged skill
        /// </summary>
        [DefaultValue(40)]
        public int MaxRangeRange
        {
            get { return _maxRangeRange; }
            set
            {
                if (_maxRangeRange != value)
                {
                    _maxRangeRange = value;
                    NotifyPropertyChanged(() => MaxRangeRange);
                }
            }
        }

        /// <summary>
        /// Number of mobs near the best target to use AOE skill
        /// </summary>
        [DefaultValue(3)]
        public int AoePackSize
        {
            get { return _aoePackSize; }
            set
            {
                if (_aoePackSize != value)
                {
                    _aoePackSize = value;
                    NotifyPropertyChanged(() => AoePackSize);
                }
            }
        }

        /// <summary>
        /// Should the CR always attack in place
        /// </summary>
        [DefaultValue(false)]
        public bool AlwaysAttackInPlace
        {
            get { return _alwaysAttackInPlace; }
            set
            {
                if (_alwaysAttackInPlace != value)
                {
                    _alwaysAttackInPlace = value;
                    NotifyPropertyChanged(() => AlwaysAttackInPlace);
                }
            }
        }

        /// <summary>
        /// Enable zoom mode - only attacks near beasts/caches, ignores random enemies
        /// When disabled, always attacks all enemies
        /// </summary>
        [DefaultValue(true)]
        public bool EnableZoomMode
        {
            get { return _enableZoomMode; }
            set
            {
                if (_enableZoomMode != value)
                {
                    _enableZoomMode = value;
                    NotifyPropertyChanged(() => EnableZoomMode);
                }
            }
        }

        /// <summary>
        /// Automatically detect and assign skills from skillbar on start
        /// </summary>
        [DefaultValue(true)]
        public bool AutoDetectSkills
        {
            get { return _autoDetectSkills; }
            set
            {
                if (_autoDetectSkills != value)
                {
                    _autoDetectSkills = value;
                    NotifyPropertyChanged(() => AutoDetectSkills);
                }
            }
        }

        // For GUI ComboBoxes
        [JsonIgnore]
        private static List<int> _allSkillSlots;

        [JsonIgnore]
        public static List<int> AllSkillSlots => _allSkillSlots ?? (_allSkillSlots = new List<int>
        {
            -1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13
        });

        [JsonIgnore]
        private static Dictionary<string, int> _keybindToSlot;

        /// <summary>
        /// Maps keybind names to slot numbers
        /// </summary>
        [JsonIgnore]
        public static Dictionary<string, int> KeybindToSlot => _keybindToSlot ?? (_keybindToSlot = new Dictionary<string, int>
        {
            { "None", -1 },
            { "Q", 1 },
            { "W", 2 },
            { "E", 3 },
            { "R", 4 },
            { "T", 5 },
            { "Spacebar", 6 },
            { "Y", 7 },
            { "1", 8 },
            { "2", 9 },
            { "3", 10 },
            { "4", 11 },
            { "5", 12 },
            { "Custom", 13 }
        });

        [JsonIgnore]
        private static List<string> _allKeybinds;

        /// <summary>
        /// List of keybind names for GUI dropdowns
        /// </summary>
        [JsonIgnore]
        public static List<string> AllKeybinds => _allKeybinds ?? (_allKeybinds = new List<string>
        {
            "None", "Q", "W", "E", "R", "T", "Spacebar", "Y", "1", "2", "3", "4", "5", "Custom"
        });

        /// <summary>
        /// Converts slot number to keybind name for display
        /// </summary>
        public static string SlotToKeybind(int slot)
        {
            foreach (var kvp in KeybindToSlot)
            {
                if (kvp.Value == slot)
                    return kvp.Key;
            }
            return "None";
        }
    }
}
