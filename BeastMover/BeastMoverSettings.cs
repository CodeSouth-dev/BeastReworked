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
    }
}

