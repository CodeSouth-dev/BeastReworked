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

namespace SimpleMapBot.Core
{
    /// <summary>
    /// Simple map running bot - handles looting, map device, and stashing
    /// </summary>
    public class SimpleMapBot : IBot
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private int _tickCount = 0;
        private bool _isProcessingMapDevice = false;
        private bool _isProcessingStash = false;
        private bool _needsToReturnToMap = false;

        #region IAuthored
        public string Name => "SimpleMapBot";
        public string Description => "Simple map running bot - looting, map device, stashing";
        public string Author => "BeastReworked";
        public string Version => "1.0.0";
        #endregion

        #region IBase
        public void Initialize()
        {
            Log.Info("[SimpleMapBot] Initialize called");
        }

        public void Deinitialize()
        {
            Log.Info("[SimpleMapBot] Deinitialize called");
        }

        public void Enable()
        {
            Log.Info("[SimpleMapBot] Enable called");
        }

        public void Disable()
        {
            Log.Info("[SimpleMapBot] Disable called");
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
            Log.Info("[SimpleMapBot] Bot started!");
        }

        public void Stop()
        {
            Log.Info("[SimpleMapBot] Bot stopped!");
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
                Log.InfoFormat("[SimpleMapBot] Tick {0} - Area: {1}, Pos: {2}",
                    _tickCount, cwa?.Name ?? "Unknown", pos);
            }

            // In hideout - handle stashing or map device
            if (cwa != null && cwa.IsHideoutArea)
            {
                // If we need to return to map, find and enter the portal
                if (_needsToReturnToMap)
                {
                    TryReturnToMap();
                    return;
                }

                // Otherwise handle stash or map device
                TryHandleStash();
                TryHandleMapDevice();
                return;
            }

            // In map - check inventory first, then loot
            if (IsInventoryFull())
            {
                Log.Info("[SimpleMapBot] Inventory full, returning to hideout to stash");
                TryReturnToHideout();
                return;
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

            Log.InfoFormat("[SimpleMapBot] Found loot: {0} at {1:F1}m", loot.Item?.Name ?? "Unknown", loot.Distance);

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
                Log.InfoFormat("[SimpleMapBot] Looted: {0}", loot.Item?.Name ?? "Unknown");
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
                    Log.InfoFormat("[SimpleMapBot] Found portal, entering map");
                    await EnterPortal(portal);
                    return;
                }

                // Check if we have a map in inventory
                var map = GetMapFromInventory();
                if (map == null)
                {
                    Log.Warn("[SimpleMapBot] No map in inventory");
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
                Log.Warn("[SimpleMapBot] Map device not found");
                return;
            }

            // Move closer if needed
            if (device.Distance > 30)
            {
                Log.InfoFormat("[SimpleMapBot] Moving to map device (distance: {0:F1})", device.Distance);
                PlayerMoverManager.Current.MoveTowards(device.Position);
                await Coroutine.Sleep(100);
                return;
            }

            // Open map device UI
            if (!LokiPoe.InGameState.MapDeviceUi.IsOpened && !LokiPoe.InGameState.MasterDeviceUi.IsOpened)
            {
                Log.Info("[SimpleMapBot] Opening map device");
                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                if (!await Coroutines.InteractWith(device))
                {
                    Log.Warn("[SimpleMapBot] Failed to interact with map device");
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
                    Log.Warn("[SimpleMapBot] Map device UI did not open");
                    return;
                }
            }

            // Place map in device
            Log.InfoFormat("[SimpleMapBot] Placing map: {0}", map.Name);
            var inventory = LokiPoe.InGameState.InventoryUi.InventoryControl_Main;
            if (!inventory.UseItem(map.LocalId))
            {
                Log.Warn("[SimpleMapBot] Failed to use map item");
                return;
            }

            await Coroutine.Sleep(300);

            // Activate device
            Log.Info("[SimpleMapBot] Activating map device");
            var mapDeviceUi = LokiPoe.InGameState.MapDeviceUi;
            if (!mapDeviceUi.Activate())
            {
                Log.Warn("[SimpleMapBot] Failed to activate map device");
                return;
            }

            await Coroutine.Sleep(500);

            // Close UI
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(200);

            Log.Info("[SimpleMapBot] Map device activated, waiting for portal");
        }

        private async Task EnterPortal(Portal portal)
        {
            // Move to portal if needed
            if (portal.Distance > 20)
            {
                Log.InfoFormat("[SimpleMapBot] Moving to portal (distance: {0:F1})", portal.Distance);
                PlayerMoverManager.Current.MoveTowards(portal.Position);
                await Coroutine.Sleep(100);
                return;
            }

            // Enter portal
            Log.Info("[SimpleMapBot] Entering map portal");
            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            if (!await Coroutines.InteractWith(portal))
            {
                Log.Warn("[SimpleMapBot] Failed to interact with portal");
                return;
            }

            // Wait for zone transition
            for (int i = 0; i < 50; i++)
            {
                await Coroutine.Sleep(100);

                var newArea = LokiPoe.CurrentWorldArea;
                if (newArea != null && !newArea.IsHideoutArea && !newArea.IsTown)
                {
                    Log.InfoFormat("[SimpleMapBot] Entered map: {0}", newArea.Name);
                    return;
                }
            }

            Log.Warn("[SimpleMapBot] Failed to enter map (timeout)");
        }

        private bool IsInventoryFull()
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
                return false;

            // Count free slots
            int totalSlots = inventory.Width * inventory.Height;
            int usedSlots = inventory.AvailableItems.Count;
            int freeSlots = totalSlots - usedSlots;

            return freeSlots <= 5; // Return if 5 or fewer slots available
        }

        private async void TryReturnToHideout()
        {
            // Use portal skill or scroll to return
            var portalSkill = LokiPoe.Me.AvailableSkills.FirstOrDefault(s => s.Name == "Portal");
            if (portalSkill != null && portalSkill.CanUse())
            {
                Log.Info("[SimpleMapBot] Using portal skill to return to hideout");
                var slot = portalSkill.Slot;
                LokiPoe.InGameState.SkillBarHud.Use(slot, false, false);
                await Coroutine.Sleep(500);

                // Wait for portal to appear and enter it
                for (int i = 0; i < 30; i++)
                {
                    var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                        .FirstOrDefault(p => p.Distance < 20);

                    if (portal != null)
                    {
                        LokiPoe.ProcessHookManager.ClearAllKeyStates();
                        if (await Coroutines.InteractWith(portal))
                        {
                            _needsToReturnToMap = true;
                            Log.Info("[SimpleMapBot] Returning to hideout to stash items");
                            await Coroutine.Sleep(2000); // Wait for zone transition
                            return;
                        }
                    }
                    await Coroutine.Sleep(100);
                }
            }

            Log.Warn("[SimpleMapBot] No portal skill available");
        }

        private async void TryReturnToMap()
        {
            // Find the map portal (not map device portal)
            var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                .FirstOrDefault(p => p.Distance < 100 && !p.Metadata.Contains("MapDevice"));

            if (portal == null)
            {
                Log.Warn("[SimpleMapBot] No return portal found, starting new map");
                _needsToReturnToMap = false;
                return;
            }

            // Move to portal if needed
            if (portal.Distance > 20)
            {
                Log.InfoFormat("[SimpleMapBot] Moving to return portal (distance: {0:F1})", portal.Distance);
                PlayerMoverManager.Current.MoveTowards(portal.Position);
                await Coroutine.Sleep(100);
                return;
            }

            // Enter portal
            Log.Info("[SimpleMapBot] Re-entering map");
            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            if (await Coroutines.InteractWith(portal))
            {
                await Coroutine.Sleep(2000); // Wait for zone transition
                _needsToReturnToMap = false;
                Log.Info("[SimpleMapBot] Returned to map");
            }
        }

        private async void TryHandleStash()
        {
            // Prevent re-entrant calls
            if (_isProcessingStash)
                return;

            // Check if we have items to stash
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null || inventory.AvailableItems.Count == 0)
                return;

            _isProcessingStash = true;

            try
            {
                // Find stash
                var stash = LokiPoe.ObjectManager.Stash;
                if (stash == null)
                {
                    Log.Warn("[SimpleMapBot] Stash not found");
                    return;
                }

                // Move to stash if needed
                if (stash.Distance > 30)
                {
                    Log.InfoFormat("[SimpleMapBot] Moving to stash (distance: {0:F1})", stash.Distance);
                    PlayerMoverManager.Current.MoveTowards(stash.Position);
                    await Coroutine.Sleep(100);
                    return;
                }

                // Open stash if not open
                if (!LokiPoe.InGameState.StashUi.IsOpened)
                {
                    Log.Info("[SimpleMapBot] Opening stash");
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();

                    if (!await Coroutines.InteractWith(stash))
                    {
                        Log.Warn("[SimpleMapBot] Failed to interact with stash");
                        return;
                    }

                    // Wait for UI to open
                    for (int i = 0; i < 30; i++)
                    {
                        if (LokiPoe.InGameState.StashUi.IsOpened && LokiPoe.InGameState.StashUi.StashTabInfo != null)
                            break;
                        await Coroutine.Sleep(100);
                    }

                    if (!LokiPoe.InGameState.StashUi.IsOpened)
                    {
                        Log.Warn("[SimpleMapBot] Stash UI did not open");
                        return;
                    }
                }

                // Deposit all items
                Log.Info("[SimpleMapBot] Depositing items to stash");
                var itemsToStash = inventory.AvailableItems.ToList();

                foreach (var item in itemsToStash)
                {
                    if (item == null || item.Class == "Maps") // Don't stash maps
                        continue;

                    var stashUi = LokiPoe.InGameState.StashUi;
                    var err = stashUi.InventoryControl.FastMove(item.LocalId);

                    if (err != LokiPoe.InGameState.FastMoveResult.None)
                    {
                        Log.WarnFormat("[SimpleMapBot] Failed to stash item: {0} (error: {1})", item.Name, err);
                    }
                    else
                    {
                        Log.InfoFormat("[SimpleMapBot] Stashed: {0}", item.Name);
                    }

                    await Coroutine.Sleep(50);
                }

                // Close stash
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(200);

                Log.Info("[SimpleMapBot] Finished stashing items");
            }
            finally
            {
                _isProcessingStash = false;
            }
        }
        #endregion
    }
}
