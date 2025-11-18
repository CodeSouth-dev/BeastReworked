using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Configuration;
using Beasts.Services;
using log4net;

namespace Beasts.Helpers
{
    /// <summary>
    /// Helper class for map device operations - provides clean, focused methods for map handling
    /// </summary>
    public static class MapDeviceHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MapDeviceHelper));

        #region Map Device Status Checks

        /// <summary>
        /// Check if map device UI is currently open
        /// </summary>
        public static bool IsMapDeviceOpen()
        {
            return LokiPoe.InGameState.MapDeviceUi.IsOpened ||
                   LokiPoe.InGameState.MasterDeviceUi.IsOpened;
        }

        /// <summary>
        /// Check if a map is currently inserted in the map device
        /// Must call with device UI open
        /// </summary>
        public static bool IsMapInserted()
        {
            try
            {
                if (!IsMapDeviceOpen())
                {
                    Log.Warn("[MapDeviceHelper] Cannot check if map inserted - device UI is not open");
                    return false;
                }

                // Get the correct inventory control based on which UI is open
                var deviceControl = LokiPoe.InGameState.MapDeviceUi.IsOpened
                    ? LokiPoe.InGameState.MapDeviceUi.InventoryControl
                    : LokiPoe.InGameState.MasterDeviceUi.InventoryControl;

                if (deviceControl?.Inventory?.Items == null)
                {
                    Log.Debug("[MapDeviceHelper] Device inventory is null - no map inserted");
                    return false;
                }

                // Check if any item in device is a map
                bool hasMap = deviceControl.Inventory.Items.Any(item =>
                    item != null &&
                    item.IsValid &&
                    item.Class != null &&
                    item.Class.ToLower().Contains("map"));

                Log.InfoFormat("[MapDeviceHelper] Map device has map: {0}", hasMap);
                return hasMap;
            }
            catch (Exception ex)
            {
                Log.Error("[MapDeviceHelper] Error checking if map is inserted", ex);
                return false;
            }
        }

        /// <summary>
        /// Check if scarabs are inserted in the map device
        /// </summary>
        public static bool HasScrabsInserted()
        {
            try
            {
                if (!IsMapDeviceOpen())
                {
                    return false;
                }

                var deviceControl = LokiPoe.InGameState.MapDeviceUi.IsOpened
                    ? LokiPoe.InGameState.MapDeviceUi.InventoryControl
                    : LokiPoe.InGameState.MasterDeviceUi.InventoryControl;

                if (deviceControl?.Inventory?.Items == null)
                {
                    return false;
                }

                return deviceControl.Inventory.Items.Any(item =>
                    item != null &&
                    item.IsValid &&
                    item.Class != null &&
                    item.Class.ToLower().Contains("scarab"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get count of items currently in map device
        /// </summary>
        public static int GetDeviceItemCount()
        {
            try
            {
                if (!IsMapDeviceOpen())
                {
                    return 0;
                }

                var deviceControl = LokiPoe.InGameState.MapDeviceUi.IsOpened
                    ? LokiPoe.InGameState.MapDeviceUi.InventoryControl
                    : LokiPoe.InGameState.MasterDeviceUi.InventoryControl;

                return deviceControl?.Inventory?.Items?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Map Finding and Selection

        /// <summary>
        /// Find a runnable map in player inventory based on settings
        /// </summary>
        public static Item FindMapInInventory()
        {
            try
            {
                var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
                if (inventory == null)
                {
                    Log.Warn("[MapDeviceHelper] Could not access inventory");
                    return null;
                }

                var settings = BeastRoutineSettings.Instance.MapDevice;

                // Find all maps in inventory
                var maps = inventory.Items
                    .Where(item => item != null &&
                                  item.IsValid &&
                                  item.Class != null &&
                                  item.Class.ToLower().Contains("map"))
                    .ToList();

                if (!maps.Any())
                {
                    Log.Debug("[MapDeviceHelper] No maps found in inventory");
                    return null;
                }

                // Filter by tier if specified
                if (settings.MinMapTier > 0 || settings.MaxMapTier < 16)
                {
                    maps = maps.Where(m =>
                    {
                        int tier = m.MapTier;
                        if (tier == 0)
                        {
                            Log.WarnFormat("[MapDeviceHelper] Could not determine tier for map: {0}", m.Name);
                            return true; // Include maps with unknown tier to be safe
                        }

                        return tier >= settings.MinMapTier && tier <= settings.MaxMapTier;
                    }).ToList();

                    if (!maps.Any())
                    {
                        Log.InfoFormat("[MapDeviceHelper] No maps in tier range {0}-{1}",
                            settings.MinMapTier, settings.MaxMapTier);
                        return null;
                    }
                }

                // Try preferred map first
                if (!string.IsNullOrEmpty(settings.PreferredMap))
                {
                    var preferredMap = maps.FirstOrDefault(m =>
                        m.Name.ToLower().Contains(settings.PreferredMap.ToLower()));

                    if (preferredMap != null)
                    {
                        Log.InfoFormat("[MapDeviceHelper] Found preferred map: {0}", preferredMap.Name);
                        return preferredMap;
                    }
                }

                // Handle map rotation
                if (settings.EnableMapRotation && !string.IsNullOrEmpty(settings.MapRotationList))
                {
                    var rotationMaps = settings.MapRotationList
                        .Split(',')
                        .Select(m => m.Trim())
                        .Where(m => !string.IsNullOrEmpty(m))
                        .ToList();

                    foreach (var rotationMapName in rotationMaps)
                    {
                        var rotationMap = maps.FirstOrDefault(m =>
                            m.Name.ToLower().Contains(rotationMapName.ToLower()));

                        if (rotationMap != null)
                        {
                            Log.InfoFormat("[MapDeviceHelper] Found rotation map: {0}", rotationMap.Name);
                            return rotationMap;
                        }
                    }
                }

                // Return first available map
                var selectedMap = maps.First();
                Log.InfoFormat("[MapDeviceHelper] Selected map: {0} (Tier {1})",
                    selectedMap.Name, selectedMap.MapTier);
                return selectedMap;
            }
            catch (Exception ex)
            {
                Log.Error("[MapDeviceHelper] Error finding map in inventory", ex);
                return null;
            }
        }

        /// <summary>
        /// Find scarabs in inventory
        /// </summary>
        public static Item[] FindScarabsInInventory()
        {
            try
            {
                var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
                if (inventory == null)
                {
                    return new Item[0];
                }

                var settings = BeastRoutineSettings.Instance.MapDevice;

                var scarabs = inventory.Items
                    .Where(item => item != null &&
                                  item.IsValid &&
                                  item.Class != null &&
                                  item.Class.ToLower().Contains("scarab"))
                    .ToList();

                // If specific scarabs are configured, filter to those
                if (!string.IsNullOrEmpty(settings.PreferredScarabs))
                {
                    var preferredScarabNames = settings.PreferredScarabs
                        .Split(',')
                        .Select(s => s.Trim().ToLower())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();

                    if (preferredScarabNames.Any())
                    {
                        scarabs = scarabs.Where(s =>
                            preferredScarabNames.Any(name => s.Name.ToLower().Contains(name)))
                            .ToList();
                    }
                }

                return scarabs.ToArray();
            }
            catch (Exception ex)
            {
                Log.Error("[MapDeviceHelper] Error finding scarabs", ex);
                return new Item[0];
            }
        }

        #endregion

        #region Map Insertion

        /// <summary>
        /// Insert a map from inventory into the map device
        /// Device UI must be open before calling
        /// </summary>
        public static async Task<bool> InsertMapFromInventory()
        {
            try
            {
                if (!IsMapDeviceOpen())
                {
                    Log.Error("[MapDeviceHelper] Cannot insert map - device UI is not open");
                    return false;
                }

                // Check if map already inserted
                if (IsMapInserted())
                {
                    Log.Info("[MapDeviceHelper] Map already inserted in device");
                    return true;
                }

                // Find map in inventory
                var mapItem = FindMapInInventory();
                if (mapItem == null)
                {
                    Log.Info("[MapDeviceHelper] No suitable map found in inventory");
                    return false;
                }

                // Insert map using MapDeviceService
                Log.InfoFormat("[MapDeviceHelper] Inserting map: {0}", mapItem.Name);
                return await MapDeviceService.PlaceItemInDevice(mapItem);
            }
            catch (Exception ex)
            {
                Log.Error("[MapDeviceHelper] Error inserting map from inventory", ex);
                return false;
            }
        }

        /// <summary>
        /// Insert scarabs from inventory into the map device
        /// Device UI must be open before calling
        /// </summary>
        public static async Task<bool> InsertScarabsFromInventory()
        {
            try
            {
                if (!IsMapDeviceOpen())
                {
                    Log.Error("[MapDeviceHelper] Cannot insert scarabs - device UI is not open");
                    return false;
                }

                var scarabs = FindScarabsInInventory();
                if (scarabs.Length == 0)
                {
                    Log.Debug("[MapDeviceHelper] No scarabs found in inventory");
                    return true; // Not an error - scarabs are optional
                }

                var settings = BeastRoutineSettings.Instance.MapDevice;
                int maxScarabs = Math.Min(scarabs.Length, settings.MaxScarabsToUse);

                Log.InfoFormat("[MapDeviceHelper] Inserting {0} scarabs", maxScarabs);

                int inserted = 0;
                for (int i = 0; i < maxScarabs; i++)
                {
                    if (await MapDeviceService.PlaceItemInDevice(scarabs[i]))
                    {
                        inserted++;
                        await Coroutine.Sleep(100); // Small delay between insertions
                    }
                }

                Log.InfoFormat("[MapDeviceHelper] Inserted {0}/{1} scarabs", inserted, maxScarabs);
                return inserted > 0 || scarabs.Length == 0;
            }
            catch (Exception ex)
            {
                Log.Error("[MapDeviceHelper] Error inserting scarabs", ex);
                return false;
            }
        }

        #endregion

        #region Stash Operations

        /// <summary>
        /// Find stash chest in hideout
        /// </summary>
        public static Chest FindStashChest()
        {
            try
            {
                return LokiPoe.ObjectManager.Objects
                    .OfType<Chest>()
                    .Where(c => c != null &&
                               c.IsValid &&
                               c.IsTargetable &&
                               c.Name != null &&
                               c.Name.ToLower().Contains("stash"))
                    .OrderBy(c => c.Distance)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if there are maps in stash
        /// Stash must be open before calling
        /// </summary>
        public static bool HasMapsInStash()
        {
            try
            {
                if (!LokiPoe.InGameState.StashUi.IsOpened)
                {
                    return false;
                }

                var settings = BeastRoutineSettings.Instance.MapDevice;
                var stashControl = LokiPoe.InGameState.StashUi.StashTabControl;

                if (stashControl == null)
                {
                    return false;
                }

                // Search current tab for maps
                var currentTab = stashControl.CurrentTab;
                if (currentTab?.Inventory?.Items == null)
                {
                    return false;
                }

                return currentTab.Inventory.Items.Any(item =>
                    item != null &&
                    item.IsValid &&
                    item.Class != null &&
                    item.Class.ToLower().Contains("map") &&
                    (settings.MinMapTier == 0 || item.MapTier >= settings.MinMapTier) &&
                    (settings.MaxMapTier >= 16 || item.MapTier <= settings.MaxMapTier));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Withdraw a map from stash to inventory
        /// Stash must be open and positioned on a tab with maps
        /// </summary>
        public static async Task<bool> WithdrawMapFromStash()
        {
            try
            {
                if (!LokiPoe.InGameState.StashUi.IsOpened)
                {
                    Log.Error("[MapDeviceHelper] Cannot withdraw map - stash is not open");
                    return false;
                }

                var settings = BeastRoutineSettings.Instance.MapDevice;
                var stashControl = LokiPoe.InGameState.StashUi.StashTabControl;

                if (stashControl?.CurrentTab?.Inventory?.Items == null)
                {
                    Log.Error("[MapDeviceHelper] Cannot access stash tab inventory");
                    return false;
                }

                // Find a suitable map in current stash tab
                var mapToWithdraw = stashControl.CurrentTab.Inventory.Items
                    .Where(item =>
                        item != null &&
                        item.IsValid &&
                        item.Class != null &&
                        item.Class.ToLower().Contains("map") &&
                        (settings.MinMapTier == 0 || item.MapTier >= settings.MinMapTier) &&
                        (settings.MaxMapTier >= 16 || item.MapTier <= settings.MaxMapTier))
                    .FirstOrDefault();

                if (mapToWithdraw == null)
                {
                    Log.Info("[MapDeviceHelper] No suitable maps found in current stash tab");
                    return false;
                }

                Log.InfoFormat("[MapDeviceHelper] Withdrawing map from stash: {0} (Tier {1})",
                    mapToWithdraw.Name, mapToWithdraw.MapTier);

                // Use FastMove to withdraw map
                var result = stashControl.CurrentTab.InventoryControl.FastMove(mapToWithdraw.LocalId);

                if (result != FastMoveResult.None)
                {
                    Log.ErrorFormat("[MapDeviceHelper] Failed to withdraw map: {0}", result);
                    return false;
                }

                await Coroutine.Sleep(200); // Wait for item to move
                Log.Info("[MapDeviceHelper] Map withdrawn successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[MapDeviceHelper] Error withdrawing map from stash", ex);
                return false;
            }
        }

        #endregion

        #region High-Level Workflow

        /// <summary>
        /// Ensure a map is in the device - handles full workflow:
        /// 1. Check if map already inserted
        /// 2. If not, try to insert from inventory
        /// 3. If no map in inventory, needs to go to stash (caller handles this)
        /// </summary>
        public static async Task<MapDeviceStatus> EnsureMapInDevice()
        {
            try
            {
                // Device must be open
                if (!IsMapDeviceOpen())
                {
                    Log.Error("[MapDeviceHelper] Device UI must be open before ensuring map");
                    return MapDeviceStatus.DeviceNotOpen;
                }

                // Check if map already inserted
                if (IsMapInserted())
                {
                    Log.Info("[MapDeviceHelper] Map already in device");
                    return MapDeviceStatus.MapReady;
                }

                // Try to insert from inventory
                Log.Info("[MapDeviceHelper] No map in device, checking inventory...");

                var mapInInventory = FindMapInInventory();
                if (mapInInventory == null)
                {
                    Log.Info("[MapDeviceHelper] No map in inventory - need to fetch from stash");
                    return MapDeviceStatus.NeedMapFromStash;
                }

                // Insert map from inventory
                if (await InsertMapFromInventory())
                {
                    Log.Info("[MapDeviceHelper] Map inserted successfully");
                    return MapDeviceStatus.MapReady;
                }

                Log.Error("[MapDeviceHelper] Failed to insert map from inventory");
                return MapDeviceStatus.InsertionFailed;
            }
            catch (Exception ex)
            {
                Log.Error("[MapDeviceHelper] Error in EnsureMapInDevice", ex);
                return MapDeviceStatus.Error;
            }
        }

        #endregion
    }

    /// <summary>
    /// Status returned by map device workflow operations
    /// </summary>
    public enum MapDeviceStatus
    {
        MapReady,           // Map is in device and ready to activate
        NeedMapFromStash,   // No map in inventory, need to fetch from stash
        DeviceNotOpen,      // Map device UI is not open
        InsertionFailed,    // Failed to insert map into device
        Error               // Unexpected error occurred
    }
}
