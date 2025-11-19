using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
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

            // Try to loot nearby items
            TryLootNearbyItems();
        }

        private async void TryLootNearbyItems()
        {
            // Find nearby valuable loot (currency, maps, divination cards)
            var loot = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
                .Where(wi => wi != null && wi.IsValid &&
                           wi.Distance < 50 &&
                           IsItemValuable(wi.Item))
                .OrderBy(wi => wi.Distance)
                .FirstOrDefault();

            if (loot == null)
                return;

            Log.InfoFormat("[MinimalMapBot] Found loot: {0} at {1:F1}m", loot.Item?.Name ?? "Unknown", loot.Distance);

            // Move to loot if too far (BeastMover handles the actual movement)
            if (loot.Distance > 10)
            {
                PlayerMoverManager.Current.MoveTowards(loot.Position);
                await Coroutine.Sleep(50);
                return;
            }

            // Pick up the item
            if (await Coroutines.InteractWith(loot))
            {
                Log.InfoFormat("[MinimalMapBot] Looted: {0}", loot.Item?.Name ?? "Unknown");
                await Coroutine.Sleep(100);
            }
        }

        private bool IsItemValuable(Item item)
        {
            if (item == null)
                return false;

            // Pick up currency, maps, and divination cards
            return item.Class == "Currency" ||
                   item.Class == "Maps" ||
                   item.Class == "Divination Card";
        }
        #endregion
    }
}
