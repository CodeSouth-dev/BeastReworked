using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Core;
using log4net;

namespace Beasts.Services
{
    /// <summary>
    /// Service for map device operations with device storage caching
    /// Map devices can hold multiple maps and scarabs in storage slots
    /// </summary>
    public static class MapDeviceService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MapDeviceService));


        /// <summary>
        /// Opens the map device UI
        /// </summary>
        /// <returns>True if map device UI is open</returns>
        public static async Task<bool> OpenMapDevice()
        {
            // Check if map device UI is already open (including witness selection UI)
            bool mapDeviceOpen = LokiPoe.InGameState.MapDeviceUi.IsOpened;
            bool masterDeviceOpen = LokiPoe.InGameState.MasterDeviceUi.IsOpened;

            if (mapDeviceOpen || masterDeviceOpen)
            {
                Log.InfoFormat("[MapDeviceService] Map device is already open (MapDeviceUi: {0}, MasterDeviceUi: {1})",
                    mapDeviceOpen, masterDeviceOpen);
                return true;
            }

            Log.Debug("[MapDeviceService] Map device UI not open - attempting to open it");

            // Close any blocking windows first (stash, heist locker, etc.)
            if (LokiPoe.InGameState.StashUi.IsOpened ||
                LokiPoe.InGameState.HeistLockerUi.IsOpened)
            {
                Log.Info("[MapDeviceService] Closing open UIs before opening map device");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(200);
            }

            var device = LokiPoe.ObjectManager.MapDevice;
            if (device == null)
            {
                Log.Warn("[MapDeviceService] Map device not found");
                return false;
            }

            Log.DebugFormat("[MapDeviceService] Found map device at distance {0:F1}", device.Distance);

            // Move closer if needed
            if (device.Distance > 30)
            {
                Log.Debug("[MapDeviceService] Moving closer to map device...");
                PlayerMoverManager.Current.MoveTowards(device.Position);
                await Coroutine.Sleep(100);
                return false; // Still moving
            }

            // Interact with device
            Log.Debug("[MapDeviceService] Attempting to interact with map device...");
            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            if (!await Coroutines.InteractWith(device))
            {
                Log.Warn("[MapDeviceService] Failed to interact with map device");
                return false;
            }

            // Wait for UI to open (up to 3 seconds)
            Log.Debug("[MapDeviceService] Waiting for map device UI to open...");
            bool uiOpened = false;
            for (int i = 0; i < 30; i++) // 30 * 100ms = 3000ms
            {
                if (LokiPoe.InGameState.MapDeviceUi.IsOpened)
                {
                    uiOpened = true;
                    break;
                }
                await Coroutine.Sleep(100);
            }

            if (!uiOpened)
            {
                Log.Warn("[MapDeviceService] Map device UI did not open after interaction");
                return false;
            }

            Log.Info("[MapDeviceService] Map device opened successfully");
            await Coroutine.Sleep(200);
            return true;
        }

        /// <summary>
        /// Places an item (map/fragment/scarab) into the map device
        /// </summary>
        /// <param name="item">Item to place</param>
        /// <returns>True if item was placed successfully</returns>
        public static async Task<bool> PlaceItemInDevice(Item item)
        {
            if (item == null || !item.IsValid)
            {
                Log.Error("[MapDeviceService] Invalid item");
                return false;
            }

            if (!LokiPoe.InGameState.MapDeviceUi.IsOpened)
            {
                Log.Error("[MapDeviceService] Map device is not open");
                return false;
            }

            var deviceControl = LokiPoe.InGameState.MapDeviceUi.InventoryControl;
            if (deviceControl == null)
            {
                Log.Error("[MapDeviceService] Cannot access map device inventory");
                return false;
            }

            var oldCount = deviceControl.Inventory?.Items?.Count ?? 0;

            Log.InfoFormat("[MapDeviceService] Placing {0} into device", item.FullName ?? item.Name);

            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            // Use FastMove to place item into device
            var result = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.FastMove(item.LocalId);

            if (result != FastMoveResult.None)
            {
                Log.ErrorFormat("[MapDeviceService] Failed to place item: {0}", result);
                return false;
            }

            // Wait for item to appear in device (up to 3 seconds)
            bool itemPlaced = false;
            for (int i = 0; i < 30; i++) // 30 * 100ms = 3000ms
            {
                var newCount = deviceControl.Inventory?.Items?.Count ?? 0;
                if (newCount == oldCount + 1)
                {
                    itemPlaced = true;
                    break;
                }
                await Coroutine.Sleep(100);
            }

            if (!itemPlaced)
            {
                Log.Error("[MapDeviceService] Item did not appear in device");
                return false;
            }

            Log.InfoFormat("[MapDeviceService] {0} placed successfully", item.FullName ?? item.Name);

            await Coroutine.Sleep(100);
            return true;
        }

        /// <summary>
        /// Clears all items from the map device
        /// </summary>
        /// <returns>True if device was cleared successfully</returns>
        public static async Task<bool> ClearMapDevice()
        {
            if (!LokiPoe.InGameState.MapDeviceUi.IsOpened)
            {
                Log.Error("[MapDeviceService] Map device is not open");
                return false;
            }

            var deviceControl = LokiPoe.InGameState.MapDeviceUi.InventoryControl;
            if (deviceControl == null)
            {
                Log.Error("[MapDeviceService] Cannot access map device inventory");
                return false;
            }

            var items = deviceControl.Inventory?.Items;
            if (items == null || items.Count == 0)
            {
                Log.Debug("[MapDeviceService] Map device is already empty");
                return true;
            }

            Log.InfoFormat("[MapDeviceService] Clearing {0} items from device", items.Count);

            // Move each item back to inventory
            foreach (var item in items.ToList())
            {
                if (item == null || !item.IsValid)
                    continue;

                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                var result = deviceControl.FastMove(item.LocalId);
                if (result != FastMoveResult.None)
                {
                    Log.WarnFormat("[MapDeviceService] Failed to remove {0}: {1}", item.Name, result);
                    continue;
                }

                await Coroutine.Sleep(50);
            }

            Log.Info("[MapDeviceService] Map device cleared");

            return true;
        }

        /// <summary>
        /// Activates the map device
        /// </summary>
        /// <returns>True if device was activated successfully</returns>
        public static async Task<bool> ActivateMapDevice()
        {
            if (!LokiPoe.InGameState.MapDeviceUi.IsOpened)
            {
                Log.Error("[MapDeviceService] Map device is not open");
                return false;
            }

            Log.Info("[MapDeviceService] Activating map device");

            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            // Delay to ensure UI is ready (especially important when all items are in one section)
            await Coroutine.Sleep(1000);

            // Verify map device UI is still open before activation
            if (!LokiPoe.InGameState.MapDeviceUi.IsOpened)
            {
                Log.Error("[MapDeviceService] Map device UI closed unexpectedly before activation");
                return false;
            }

            // Activate the map device
            Log.Debug("[MapDeviceService] Calling MapDeviceUi.Activate()");
            var activated = LokiPoe.InGameState.MapDeviceUi.Activate();

            if (activated != LokiPoe.InGameState.ActivateResult.None)
            {
                Log.ErrorFormat("[MapDeviceService] Failed to activate device: {0}", activated);
                return false;
            }

            // Wait for device to close (indicates activation) - up to 5 seconds
            bool deviceClosed = false;
            for (int i = 0; i < 50; i++) // 50 * 100ms = 5000ms
            {
                if (!LokiPoe.InGameState.MapDeviceUi.IsOpened)
                {
                    deviceClosed = true;
                    break;
                }
                await Coroutine.Sleep(100);
            }

            if (!deviceClosed)
            {
                Log.Warn("[MapDeviceService] Map device UI did not close after activation");
                // Not necessarily an error - device might stay open in some cases
            }

            Log.Info("[MapDeviceService] Map device activated successfully");

            await Coroutine.Sleep(500);
            return true;
        }

        /// <summary>
        /// Places a map and optional fragments into device, then activates it
        /// </summary>
        /// <param name="mapItem">Map to run</param>
        /// <param name="fragments">Optional fragments/scarabs</param>
        /// <returns>True if map was opened successfully</returns>
        public static async Task<bool> OpenMap(Item mapItem, Item[] fragments = null)
        {
            if (mapItem == null)
            {
                Log.Error("[MapDeviceService] No map item provided");
                return false;
            }

            // Open device
            if (!await OpenMapDevice())
                return false;

            // Clear any existing items
            if (!await ClearMapDevice())
                return false;

            // Place map
            if (!await PlaceItemInDevice(mapItem))
                return false;

            // Place fragments/scarabs if provided
            if (fragments != null && fragments.Length > 0)
            {
                foreach (var fragment in fragments)
                {
                    if (fragment == null || !fragment.IsValid)
                        continue;

                    if (!await PlaceItemInDevice(fragment))
                    {
                        Log.WarnFormat("[MapDeviceService] Failed to place fragment: {0}", fragment.Name);
                        // Continue anyway - not critical
                    }
                }
            }

            // Activate device
            if (!await ActivateMapDevice())
                return false;

            Log.Info("[MapDeviceService] Map opened successfully");
            return true;
        }

        /// <summary>
        /// Finds the map portal after device activation
        /// </summary>
        /// <returns>Portal leading to the map or null</returns>
        public static Portal FindMapPortal()
        {
            try
            {
                return LokiPoe.ObjectManager.Objects
                    .OfType<Portal>()
                    .Where(p => p != null &&
                               p.IsValid &&
                               p.IsTargetable)
                    .OrderBy(p => p.Distance)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log.Error("[MapDeviceService] Error finding map portal", ex);
                return null;
            }
        }

    }
}
