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
        private bool _isProcessingMapDevice = false;

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

            var cwa = LokiPoe.CurrentWorldArea;

            // Just log every 100 ticks
            _tickCount++;

            if (_tickCount % 100 == 0)
            {
                var pos = LokiPoe.MyPosition;
                Log.InfoFormat("[MinimalMapBot] Tick {0} - Area: {1}, Pos: {2}",
                    _tickCount, cwa?.Name ?? "Unknown", pos);
            }

            // In hideout - handle map device
            if (cwa != null && cwa.IsHideoutArea)
            {
                TryHandleMapDevice();
                return;
            }

            // In map - try to loot nearby items
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

        private async void TryHandleMapDevice()
        {
            // Prevent re-entrant calls
            if (_isProcessingMapDevice)
                return;

            _isProcessingMapDevice = true;

            try
            {
                // Check if portal already exists (map device activated)
                var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                    .FirstOrDefault(p => p.Distance < 100);

                if (portal != null)
                {
                    Log.InfoFormat("[MinimalMapBot] Found portal, entering map");
                    await EnterPortal(portal);
                    return;
                }

                // Check if we have a map in inventory
                var map = GetMapFromInventory();
                if (map == null)
                {
                    Log.Warn("[MinimalMapBot] No map in inventory");
                    return;
                }

                // Open map device and place map
                await OpenAndActivateMapDevice(map);
            }
            finally
            {
                _isProcessingMapDevice = false;
            }
        }

        private Item GetMapFromInventory()
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
                return null;

            return inventory.Items.FirstOrDefault(item =>
                item != null && item.Class == "Maps");
        }

        private async Task OpenAndActivateMapDevice(Item map)
        {
            var device = LokiPoe.ObjectManager.MapDevice;
            if (device == null)
            {
                Log.Warn("[MinimalMapBot] Map device not found");
                return;
            }

            // Move closer if needed
            if (device.Distance > 30)
            {
                Log.InfoFormat("[MinimalMapBot] Moving to map device (distance: {0:F1})", device.Distance);
                PlayerMoverManager.Current.MoveTowards(device.Position);
                await Coroutine.Sleep(100);
                return;
            }

            // Open map device UI
            if (!LokiPoe.InGameState.MapDeviceUi.IsOpened && !LokiPoe.InGameState.MasterDeviceUi.IsOpened)
            {
                Log.Info("[MinimalMapBot] Opening map device");
                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                if (!await Coroutines.InteractWith(device))
                {
                    Log.Warn("[MinimalMapBot] Failed to interact with map device");
                    return;
                }

                // Wait for UI to open
                for (int i = 0; i < 30; i++)
                {
                    if (LokiPoe.InGameState.MapDeviceUi.IsOpened)
                        break;
                    await Coroutine.Sleep(100);
                }

                if (!LokiPoe.InGameState.MapDeviceUi.IsOpened)
                {
                    Log.Warn("[MinimalMapBot] Map device UI did not open");
                    return;
                }
            }

            // Place map in device
            Log.InfoFormat("[MinimalMapBot] Placing map: {0}", map.Name);
            var inventory = LokiPoe.InGameState.InventoryUi.InventoryControl_Main;
            if (!inventory.UseItem(map.LocalId))
            {
                Log.Warn("[MinimalMapBot] Failed to use map item");
                return;
            }

            await Coroutine.Sleep(300);

            // Activate device
            Log.Info("[MinimalMapBot] Activating map device");
            var mapDeviceUi = LokiPoe.InGameState.MapDeviceUi;
            if (!mapDeviceUi.Activate())
            {
                Log.Warn("[MinimalMapBot] Failed to activate map device");
                return;
            }

            await Coroutine.Sleep(500);

            // Close UI
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(200);

            Log.Info("[MinimalMapBot] Map device activated, waiting for portal");
        }

        private async Task EnterPortal(Portal portal)
        {
            // Move to portal if needed
            if (portal.Distance > 20)
            {
                Log.InfoFormat("[MinimalMapBot] Moving to portal (distance: {0:F1})", portal.Distance);
                PlayerMoverManager.Current.MoveTowards(portal.Position);
                await Coroutine.Sleep(100);
                return;
            }

            // Enter portal
            Log.Info("[MinimalMapBot] Entering map portal");
            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            if (!await Coroutines.InteractWith(portal))
            {
                Log.Warn("[MinimalMapBot] Failed to interact with portal");
                return;
            }

            // Wait for zone transition
            for (int i = 0; i < 50; i++)
            {
                await Coroutine.Sleep(100);

                var newArea = LokiPoe.CurrentWorldArea;
                if (newArea != null && !newArea.IsHideoutArea && !newArea.IsTown)
                {
                    Log.InfoFormat("[MinimalMapBot] Entered map: {0}", newArea.Name);
                    return;
                }
            }

            Log.Warn("[MinimalMapBot] Failed to enter map (timeout)");
        }
        #endregion
    }
}
