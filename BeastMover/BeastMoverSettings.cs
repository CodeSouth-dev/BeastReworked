using System.ComponentModel;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;

namespace BeastMover
{
    public class BeastMoverSettings : JsonSettings
    {
        private static BeastMoverSettings _instance;
        
        /// <summary>The current instance for this class.</summary>
        public static BeastMoverSettings Instance
        {
            get { return _instance ?? (_instance = new BeastMoverSettings()); }
        }

        /// <summary>The default ctor. Will use the settings path "BeastMover".</summary>
        public BeastMoverSettings()
            : base(GetSettingsFilePath(Configuration.Instance.Name, "BeastMover.json"))
        {
        }

        private int _pathRefreshRate = 1000;
        private bool _debugLogging = false; // Changed to false - less spam
        private int _stuckThreshold = 3;
        private float _stuckDistance = 5f;
        private int _singleUseDistance = 30;

        [DefaultValue(1000)]
        [Description("How often to refresh paths (in milliseconds). Lower = more responsive but more CPU.")]
        public int PathRefreshRate
        {
            get { return _pathRefreshRate; }
            set
            {
                if (value != _pathRefreshRate)
                {
                    _pathRefreshRate = value;
                    NotifyPropertyChanged(() => PathRefreshRate);
                }
            }
        }

        [DefaultValue(true)]
        [Description("Enable detailed logging for debugging movement issues.")]
        public bool DebugLogging
        {
            get { return _debugLogging; }
            set
            {
                if (value != _debugLogging)
                {
                    _debugLogging = value;
                    NotifyPropertyChanged(() => DebugLogging);
                }
            }
        }

        [DefaultValue(3)]
        [Description("Number of stuck checks before regenerating path. Lower = more aggressive recovery.")]
        public int StuckThreshold
        {
            get { return _stuckThreshold; }
            set
            {
                if (value != _stuckThreshold)
                {
                    _stuckThreshold = value;
                    NotifyPropertyChanged(() => StuckThreshold);
                }
            }
        }

        [DefaultValue(5f)]
        [Description("Distance (in meters) to consider as 'not moving' for stuck detection.")]
        public float StuckDistance
        {
            get { return _stuckDistance; }
            set
            {
                if (value != _stuckDistance)
                {
                    _stuckDistance = value;
                    NotifyPropertyChanged(() => StuckDistance);
                }
            }
        }

                    [DefaultValue(30)]
                    [Description("Distance threshold for single click vs hold movement.")]
                    public int SingleUseDistance
                    {
                        get { return _singleUseDistance; }
                        set
                        {
                            if (value != _singleUseDistance)
                            {
                                _singleUseDistance = value;
                                NotifyPropertyChanged(() => SingleUseDistance);
                            }
                        }
                    }

                    private bool _allowPortalMovement = false;
                    [DefaultValue(false)]
                    [Description("Allow movement toward portals. Set to true only when you want the bot to exit maps.")]
                    public bool AllowPortalMovement
                    {
                        get { return _allowPortalMovement; }
                        set
                        {
                            if (value != _allowPortalMovement)
                            {
                                _allowPortalMovement = value;
                                NotifyPropertyChanged(() => AllowPortalMovement);
                            }
                        }
                    }

                    private int _moveRange = 33;
                    [DefaultValue(33)]
                    [Description("Maximum range for basic movement clicks.")]
                    public int MoveRange
                    {
                        get { return _moveRange; }
                        set
                        {
                            if (value != _moveRange)
                            {
                                _moveRange = value;
                                NotifyPropertyChanged(() => MoveRange);
                            }
                        }
                    }

                    private int _moveMinMana = 20;
                    [DefaultValue(20)]
                    [Description("Minimum mana required to use movement skills.")]
                    public int MoveMinMana
                    {
                        get { return _moveMinMana; }
                        set
                        {
                            if (value != _moveMinMana)
                            {
                                _moveMinMana = value;
                                NotifyPropertyChanged(() => MoveMinMana);
                            }
                        }
                    }

                    private bool _ignoreMobs = false;
                    [DefaultValue(false)]
                    [Description("Ignore mobs when calculating movement path.")]
                    public bool IgnoreMobs
                    {
                        get { return _ignoreMobs; }
                        set
                        {
                            if (value != _ignoreMobs)
                            {
                                _ignoreMobs = value;
                                NotifyPropertyChanged(() => IgnoreMobs);
                            }
                        }
                    }

                    private bool _useBloodMagic = false;
                    [DefaultValue(false)]
                    [Description("Use blood magic for movement skills (life instead of mana).")]
                    public bool UseBloodMagic
                    {
                        get { return _useBloodMagic; }
                        set
                        {
                            if (value != _useBloodMagic)
                            {
                                _useBloodMagic = value;
                                NotifyPropertyChanged(() => UseBloodMagic);
                            }
                        }
                    }

                    // Individual skill settings
                    private bool _enableWhirlingBlades = false;
                    [DefaultValue(false)]
                    public bool EnableWhirlingBlades
                    {
                        get { return _enableWhirlingBlades; }
                        set
                        {
                            if (value != _enableWhirlingBlades)
                            {
                                _enableWhirlingBlades = value;
                                NotifyPropertyChanged(() => EnableWhirlingBlades);
                            }
                        }
                    }

                    private int _whirlingBladesMinDist = 25;
                    [DefaultValue(25)]
                    public int WhirlingBladesMinDist
                    {
                        get { return _whirlingBladesMinDist; }
                        set
                        {
                            if (value != _whirlingBladesMinDist)
                            {
                                _whirlingBladesMinDist = value;
                                NotifyPropertyChanged(() => WhirlingBladesMinDist);
                            }
                        }
                    }

                    private int _whirlingBladesMaxDist = 50;
                    [DefaultValue(50)]
                    public int WhirlingBladesMaxDist
                    {
                        get { return _whirlingBladesMaxDist; }
                        set
                        {
                            if (value != _whirlingBladesMaxDist)
                            {
                                _whirlingBladesMaxDist = value;
                                NotifyPropertyChanged(() => WhirlingBladesMaxDist);
                            }
                        }
                    }

                    private bool _enableFlameDash = false;
                    [DefaultValue(false)]
                    public bool EnableFlameDash
                    {
                        get { return _enableFlameDash; }
                        set
                        {
                            if (value != _enableFlameDash)
                            {
                                _enableFlameDash = value;
                                NotifyPropertyChanged(() => EnableFlameDash);
                            }
                        }
                    }

                    private int _flameDashMinDist = 27;
                    [DefaultValue(27)]
                    public int FlameDashMinDist
                    {
                        get { return _flameDashMinDist; }
                        set
                        {
                            if (value != _flameDashMinDist)
                            {
                                _flameDashMinDist = value;
                                NotifyPropertyChanged(() => FlameDashMinDist);
                            }
                        }
                    }

                    private int _flameDashMaxDist = 45;
                    [DefaultValue(45)]
                    public int FlameDashMaxDist
                    {
                        get { return _flameDashMaxDist; }
                        set
                        {
                            if (value != _flameDashMaxDist)
                            {
                                _flameDashMaxDist = value;
                                NotifyPropertyChanged(() => FlameDashMaxDist);
                            }
                        }
                    }

                    private bool _enableShieldCharge = false;
                    [DefaultValue(false)]
                    public bool EnableShieldCharge
                    {
                        get { return _enableShieldCharge; }
                        set
                        {
                            if (value != _enableShieldCharge)
                            {
                                _enableShieldCharge = value;
                                NotifyPropertyChanged(() => EnableShieldCharge);
                            }
                        }
                    }

                    private int _shieldChargeMinDist = 27;
                    [DefaultValue(27)]
                    public int ShieldChargeMinDist
                    {
                        get { return _shieldChargeMinDist; }
                        set
                        {
                            if (value != _shieldChargeMinDist)
                            {
                                _shieldChargeMinDist = value;
                                NotifyPropertyChanged(() => ShieldChargeMinDist);
                            }
                        }
                    }

                    private int _shieldChargeMaxDist = 50;
                    [DefaultValue(50)]
                    public int ShieldChargeMaxDist
                    {
                        get { return _shieldChargeMaxDist; }
                        set
                        {
                            if (value != _shieldChargeMaxDist)
                            {
                                _shieldChargeMaxDist = value;
                                NotifyPropertyChanged(() => ShieldChargeMaxDist);
                            }
                        }
                    }

                    private bool _enableLeapSlam = false;
                    [DefaultValue(false)]
                    public bool EnableLeapSlam
                    {
                        get { return _enableLeapSlam; }
                        set
                        {
                            if (value != _enableLeapSlam)
                            {
                                _enableLeapSlam = value;
                                NotifyPropertyChanged(() => EnableLeapSlam);
                            }
                        }
                    }

                    private int _leapSlamMinDist = 25;
                    [DefaultValue(25)]
                    public int LeapSlamMinDist
                    {
                        get { return _leapSlamMinDist; }
                        set
                        {
                            if (value != _leapSlamMinDist)
                            {
                                _leapSlamMinDist = value;
                                NotifyPropertyChanged(() => LeapSlamMinDist);
                            }
                        }
                    }

                    private int _leapSlamMaxDist = 55;
                    [DefaultValue(55)]
                    public int LeapSlamMaxDist
                    {
                        get { return _leapSlamMaxDist; }
                        set
                        {
                            if (value != _leapSlamMaxDist)
                            {
                                _leapSlamMaxDist = value;
                                NotifyPropertyChanged(() => LeapSlamMaxDist);
                            }
                        }
                    }

                    private bool _enableDash = false;
                    [DefaultValue(false)]
                    public bool EnableDash
                    {
                        get { return _enableDash; }
                        set
                        {
                            if (value != _enableDash)
                            {
                                _enableDash = value;
                                NotifyPropertyChanged(() => EnableDash);
                            }
                        }
                    }

                    private int _dashMinDist = 20;
                    [DefaultValue(20)]
                    public int DashMinDist
                    {
                        get { return _dashMinDist; }
                        set
                        {
                            if (value != _dashMinDist)
                            {
                                _dashMinDist = value;
                                NotifyPropertyChanged(() => DashMinDist);
                            }
                        }
                    }

                    private int _dashMaxDist = 30;
                    [DefaultValue(30)]
                    public int DashMaxDist
                    {
                        get { return _dashMaxDist; }
                        set
                        {
                            if (value != _dashMaxDist)
                            {
                                _dashMaxDist = value;
                                NotifyPropertyChanged(() => DashMaxDist);
                            }
                        }
                    }

                    private bool _enableFrostblink = false;
                    [DefaultValue(false)]
                    public bool EnableFrostblink
                    {
                        get { return _enableFrostblink; }
                        set
                        {
                            if (value != _enableFrostblink)
                            {
                                _enableFrostblink = value;
                                NotifyPropertyChanged(() => EnableFrostblink);
                            }
                        }
                    }

                    private int _frostblinkMinDist = 20;
                    [DefaultValue(20)]
                    public int FrostblinkMinDist
                    {
                        get { return _frostblinkMinDist; }
                        set
                        {
                            if (value != _frostblinkMinDist)
                            {
                                _frostblinkMinDist = value;
                                NotifyPropertyChanged(() => FrostblinkMinDist);
                            }
                        }
                    }

                    private int _frostblinkMaxDist = 30;
                    [DefaultValue(30)]
                    public int FrostblinkMaxDist
                    {
                        get { return _frostblinkMaxDist; }
                        set
                        {
                            if (value != _frostblinkMaxDist)
                            {
                                _frostblinkMaxDist = value;
                                NotifyPropertyChanged(() => FrostblinkMaxDist);
                            }
                        }
                    }

                    private bool _enableLightningWarp = false;
                    [DefaultValue(false)]
                    public bool EnableLightningWarp
                    {
                        get { return _enableLightningWarp; }
                        set
                        {
                            if (value != _enableLightningWarp)
                            {
                                _enableLightningWarp = value;
                                NotifyPropertyChanged(() => EnableLightningWarp);
                            }
                        }
                    }

                    private int _lightningWarpMinDist = 20;
                    [DefaultValue(20)]
                    public int LightningWarpMinDist
                    {
                        get { return _lightningWarpMinDist; }
                        set
                        {
                            if (value != _lightningWarpMinDist)
                            {
                                _lightningWarpMinDist = value;
                                NotifyPropertyChanged(() => LightningWarpMinDist);
                            }
                        }
                    }

                    private int _lightningWarpMaxDist = 60;
                    [DefaultValue(60)]
                    public int LightningWarpMaxDist
                    {
                        get { return _lightningWarpMaxDist; }
                        set
                        {
                            if (value != _lightningWarpMaxDist)
                            {
                                _lightningWarpMaxDist = value;
                                NotifyPropertyChanged(() => LightningWarpMaxDist);
                            }
                        }
                    }

                    private bool _enableBlinkArrow = false;
                    [DefaultValue(false)]
                    public bool EnableBlinkArrow
                    {
                        get { return _enableBlinkArrow; }
                        set
                        {
                            if (value != _enableBlinkArrow)
                            {
                                _enableBlinkArrow = value;
                                NotifyPropertyChanged(() => EnableBlinkArrow);
                            }
                        }
                    }

                    private int _blinkArrowMinDist = 20;
                    [DefaultValue(20)]
                    public int BlinkArrowMinDist
                    {
                        get { return _blinkArrowMinDist; }
                        set
                        {
                            if (value != _blinkArrowMinDist)
                            {
                                _blinkArrowMinDist = value;
                                NotifyPropertyChanged(() => BlinkArrowMinDist);
                            }
                        }
                    }

                    private int _blinkArrowMaxDist = 65;
                    [DefaultValue(65)]
                    public int BlinkArrowMaxDist
                    {
                        get { return _blinkArrowMaxDist; }
                        set
                        {
                            if (value != _blinkArrowMaxDist)
                            {
                                _blinkArrowMaxDist = value;
                                NotifyPropertyChanged(() => BlinkArrowMaxDist);
                            }
                        }
                    }
    }
}

