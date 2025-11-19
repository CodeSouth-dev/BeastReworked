using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using log4net;

namespace MinimalMapBot
{
    /// <summary>
    /// Minimal bot to verify API and test basic functionality
    /// </summary>
    public class MinimalMapBot : IBot
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private int _tickCount = 0;

        #region IAuthored
        public string Name => "MinimalMapBot";
        public string Description => "Minimal test bot - just logs and moves";
        public string Author => "Test";
        public string Version => "0.0.1";
        #endregion

        #region IBase
        public void Initialize()
        {
            Log.Info("[MinimalMapBot] Initialize called");
        }

        public void Deinitialize()
        {
            Log.Info("[MinimalMapBot] Deinitialize called");
        }

        public void Enable()
        {
            Log.Info("[MinimalMapBot] Enable called");
        }

        public void Disable()
        {
            Log.Info("[MinimalMapBot] Disable called");
        }
        #endregion

        #region IConfigurable
        public JsonSettings Settings => null;
        public UserControl Control => null;
        #endregion

        #region ILogicProvider
        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }
        #endregion

        #region IMessageHandler
        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
        #endregion

        #region IStartStopEvents
        public void Start()
        {
            Log.Info("[MinimalMapBot] Bot started!");
        }

        public void Stop()
        {
            Log.Info("[MinimalMapBot] Bot stopped!");
        }
        #endregion

        #region ITickEvents
        public void Tick()
        {
            // Called every tick - keep minimal
        }
        #endregion

        #region IBot
        public void Execute()
        {
            // Main bot logic
            if (!LokiPoe.IsInGame)
            {
                return;
            }

            // Just log every 100 ticks
            _tickCount++;

            if (_tickCount % 100 == 0)
            {
                var area = LokiPoe.CurrentWorldArea;
                var pos = LokiPoe.MyPosition;
                Log.InfoFormat("[MinimalMapBot] Tick {0} - Area: {1}, Pos: {2}",
                    _tickCount, area?.Name ?? "Unknown", pos);
            }
        }
        #endregion
    }
}
