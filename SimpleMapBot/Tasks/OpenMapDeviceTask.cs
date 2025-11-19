using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using SimpleMapBot.Configuration;
using SimpleMapBot.Utilities;
using log4net;

namespace SimpleMapBot.Tasks
{
    public class OpenMapDeviceTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private bool _mapPlaced = false;

        public string Name => "OpenMapDeviceTask";
        public string Description => "Opens map device and places map";
        public string Author => "SimpleMapBot";
        public string Version => "1.0.0";
        public bool IsEnabled => true;

        public async Task<bool> Run()
        {
            var cwa = LokiPoe.CurrentWorldArea;

            // Only run in hideout
            if (!cwa.IsHideoutArea && !cwa.IsTown)
                return false;

            // Check if map device is already activated
            if (IsMapDeviceActivated())
            {
                _mapPlaced = true;
                return false;
            }

            // Check if we have a map in inventory
            var map = GetMapFromInventory();
            if (map == null)
            {
                Log.Warn("[OpenMapDeviceTask] No map in inventory");
                return false;
            }

            // Open map device
            if (!await OpenMapDevice())
            {
                ErrorManager.ReportError();
                return false;
            }

            // Place map
            if (!await PlaceMap(map))
            {
                ErrorManager.ReportError();
                return false;
            }

            // Activate device
            if (await ActivateDevice())
            {
                _mapPlaced = true;
                ErrorManager.Reset(); // Reset on full success
                return true;
            }

            return false;
        }

        private bool IsMapDeviceActivated()
        {
            // Check if portal is available near map device
            var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                .FirstOrDefault(p => p.Distance < 50 && p.Metadata.Contains("MapDevice"));

            return portal != null;
        }

        private Item GetMapFromInventory()
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
                return null;

            return inventory.Items.FirstOrDefault(item =>
                item != null && item.Class == "Maps");
        }

        private async Task<bool> OpenMapDevice()
        {
            // Check if already open
            if (LokiPoe.InGameState.MapDeviceUi.IsOpened || LokiPoe.InGameState.MasterDeviceUi.IsOpened)
                return true;

            // Close any other windows
            if (LokiPoe.InGameState.StashUi.IsOpened)
            {
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(200);
            }

            var device = LokiPoe.ObjectManager.MapDevice;
            if (device == null)
            {
                Log.Warn("[OpenMapDeviceTask] Map device not found");
                return false;
            }

            // Move closer if needed
            if (device.Distance > 30)
            {
                PlayerMoverManager.Current.MoveTowards(device.Position);
                await Coroutine.Sleep(100);
                return false;
            }

            // Interact
            LokiPoe.ProcessHookManager.ClearAllKeyStates();
            if (!await Coroutines.InteractWith(device))
            {
                Log.Warn("[OpenMapDeviceTask] Failed to interact with map device");
                return false;
            }

            // Wait for UI
            for (int i = 0; i < 30; i++)
            {
                if (LokiPoe.InGameState.MapDeviceUi.IsOpened)
                    return true;
                await Coroutine.Sleep(100);
            }

            Log.Warn("[OpenMapDeviceTask] Map device UI did not open");
            return false;
        }

        private async Task<bool> PlaceMap(Item map)
        {
            if (map == null)
                return false;

            Log.InfoFormat("[OpenMapDeviceTask] Placing map: {0} (T{1})", map.Name, map.MapTier);

            // Use the map item (places it in device)
            int invId = map.LocalId;
            var inventory = LokiPoe.InGameState.InventoryUi.InventoryControl_Main;

            if (!inventory.UseItem(invId))
            {
                Log.Warn("[OpenMapDeviceTask] Failed to use map item");
                return false;
            }

            await Coroutine.Sleep(300);
            return true;
        }

        private async Task<bool> ActivateDevice()
        {
            var mapDeviceUi = LokiPoe.InGameState.MapDeviceUi;
            if (!mapDeviceUi.IsOpened)
            {
                Log.Warn("[OpenMapDeviceTask] Map device UI not open");
                ErrorManager.ReportError();
                return false;
            }

            // Find any existing portal near map device (before activation)
            var existingPortal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                .FirstOrDefault(p => p.Distance < 50 && p.Metadata.Contains("MapDevice"));

            bool hadOldPortal = existingPortal != null && existingPortal.IsTargetable;

            // Click the activate button
            Log.Info("[OpenMapDeviceTask] Activating map device");

            if (!mapDeviceUi.Activate())
            {
                Log.Warn("[OpenMapDeviceTask] Failed to activate map device");
                ErrorManager.ReportError();
                return false;
            }

            await Coroutine.Sleep(500);

            // Close UI
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(200);

            // If there was an old portal, wait for it to despawn first
            if (hadOldPortal)
            {
                Log.Debug("[OpenMapDeviceTask] Waiting for old portals to despawn");

                if (!await Wait.For(() =>
                    {
                        var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                            .FirstOrDefault(p => p.Distance < 50);
                        return portal == null || !portal.IsTargetable;
                    },
                    "old map portals despawning", 200, 10000))
                {
                    Log.Warn("[OpenMapDeviceTask] Timeout waiting for old portals to despawn");
                    ErrorManager.ReportError();
                    return false;
                }
            }

            // Wait for new map portal to spawn
            Log.Debug("[OpenMapDeviceTask] Waiting for new map portal to spawn");

            if (!await Wait.For(() =>
                {
                    var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                        .FirstOrDefault(p => p.Distance < 50);

                    if (portal == null)
                        return false;

                    if (!portal.IsTargetable)
                        return false;

                    // Verify it leads to a map
                    var leadsToArea = portal.LeadsTo;
                    return leadsToArea != null && leadsToArea.IsMap;
                },
                "new map portal spawning", 500, 15000))
            {
                Log.Warn("[OpenMapDeviceTask] Portal did not appear after activation");
                ErrorManager.ReportError();
                return false;
            }

            Log.Info("[OpenMapDeviceTask] Map device activated successfully");
            ErrorManager.Reset(); // Reset error counter on success
            return true;
        }

        #region Unused interface members
        public async Task<LogicResult> Logic(Logic logic) => LogicResult.Unprovided;
        public MessageResult Message(Message message) => MessageResult.Unprocessed;
        public void Start() { }
        public void Stop() { }
        public void Tick() { }
        #endregion
    }
}
