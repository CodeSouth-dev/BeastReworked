using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using SimpleMapBot.Configuration;
using SimpleMapBot.GUI;
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
        private SimpleMapBotGui _gui;

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
        public JsonSettings Settings => SimpleMapBotSettings.Instance;
        public UserControl Control => _gui ?? (_gui = new SimpleMapBotGui());
        #endregion

        #region ILogicProvider
        public async Task<LogicResult> Logic(Logic logic)
        {
            await Task.CompletedTask;
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
            Log.Info("=====================================================");
            Log.Info("[SimpleMapBot] Starting SimpleMapBot v1.0.0");
            Log.Info("=====================================================");

            // Check required components
            var currentMover = PlayerMoverManager.Current;
            var currentRoutine = RoutineManager.Current;

            Log.InfoFormat("[SimpleMapBot] PlayerMover: {0}", currentMover?.Name ?? "NONE - WARNING!");
            Log.InfoFormat("[SimpleMapBot] Routine: {0}", currentRoutine?.Name ?? "NONE - WARNING!");

            if (currentMover == null)
            {
                Log.Error("[SimpleMapBot] ===== NO PLAYERMOVER SELECTED =====");
                Log.Error("[SimpleMapBot] Please select BeastMover in the Bot Settings!");
            }
            else if (currentMover.Name != "BeastMover")
            {
                Log.WarnFormat("[SimpleMapBot] PlayerMover is '{0}' - BeastMover is recommended", currentMover.Name);
            }

            if (currentRoutine == null)
            {
                Log.Error("[SimpleMapBot] ===== NO ROUTINE SELECTED =====");
                Log.Error("[SimpleMapBot] Please select BeastCombatRoutine in the Bot Settings!");
            }
            else if (currentRoutine.Name != "BeastCombatRoutine")
            {
                Log.WarnFormat("[SimpleMapBot] Routine is '{0}' - BeastCombatRoutine is recommended", currentRoutine.Name);
            }

            // Cache all bound keys
            LokiPoe.Input.Binding.Update();
            Log.InfoFormat("[SimpleMapBot] KeyPickup: {0}", LokiPoe.ConfigManager.KeyPickup);

            // Log enabled maps
            var enabledMaps = SimpleMapBotSettings.Instance.GetEnabledMaps();
            if (enabledMaps.Count > 0)
            {
                Log.InfoFormat("[SimpleMapBot] Enabled maps ({0}): {1}", enabledMaps.Count, string.Join(", ", enabledMaps));
            }
            else
            {
                Log.Error("[SimpleMapBot] ===== NO MAPS ENABLED =====");
                Log.Error("[SimpleMapBot] Please configure map selection in the GUI!");
            }

            // Log selected scarabs
            var selectedScarabs = SimpleMapBotSettings.Instance.GetSelectedScarabs();
            if (selectedScarabs.Count > 0)
            {
                Log.InfoFormat("[SimpleMapBot] Selected scarabs ({0}): {1}", selectedScarabs.Count, string.Join(", ", selectedScarabs));
            }
            else
            {
                Log.Info("[SimpleMapBot] Running with 0 scarabs (none configured)");
            }

            Log.Info("=====================================================");
            Log.Info("[SimpleMapBot] Startup complete - ready to run maps!");
            Log.Info("=====================================================");
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
                if (_tickCount % 200 == 0)
                {
                    Log.Info("[SimpleMapBot] Waiting for game... (not in game yet)");
                }
                _tickCount++;
                return;
            }

            var cwa = LokiPoe.CurrentWorldArea;
            _tickCount++;

            // Log status every 5 seconds (roughly 150 ticks at 30ms per tick)
            if (_tickCount % 150 == 0)
            {
                var pos = LokiPoe.MyPosition;
                var areaName = cwa?.Name ?? "Unknown";
                var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
                var itemCount = inventory?.Items?.Count ?? 0;

                Log.InfoFormat("[SimpleMapBot] Status - Area: {0}, Items in inventory: {1}, Pos: {2}",
                    areaName, itemCount, pos);

                if (cwa != null && cwa.IsHideoutArea)
                {
                    Log.Info("[SimpleMapBot] In hideout - looking for map device or stash");
                }
                else if (cwa != null)
                {
                    Log.Info("[SimpleMapBot] In map - looting and clearing");
                }
            }

            // In hideout - handle stashing or map device
            if (cwa != null && cwa.IsHideoutArea)
            {
                // If we need to return to map, find and enter the portal
                if (_needsToReturnToMap)
                {
                    if (_tickCount % 50 == 0)
                    {
                        Log.Info("[SimpleMapBot] Need to return to map after stashing");
                    }
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
                var portals = LokiPoe.ObjectManager.GetObjectsByType<Portal>();
                var portal = portals.FirstOrDefault(p => p.Distance < 100);

                if (portal != null)
                {
                    Log.InfoFormat("[SimpleMapBot] Found portal at {0:F1}m, entering map", portal.Distance);
                    await EnterPortal(portal);
                    return;
                }

                // Log when searching for map device
                if (_tickCount % 100 == 0)
                {
                    Log.Debug("[SimpleMapBot] No portal found, checking for map in inventory");
                }

                // Check if we have a map in inventory
                var map = GetMapFromInventory();
                if (map == null)
                {
                    if (_tickCount % 200 == 0)
                    {
                        Log.Warn("[SimpleMapBot] No enabled map in inventory - waiting for maps");
                        Log.Warn("[SimpleMapBot] Make sure you have maps that match your enabled map list!");
                    }
                    return;
                }

                // Open map device and place map
                Log.InfoFormat("[SimpleMapBot] Found map to run: {0}", map.Name);
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
            {
                Log.Debug("[SimpleMapBot] Inventory is null");
                return null;
            }

            var settings = SimpleMapBotSettings.Instance;

            // Count maps in inventory for logging
            var allMaps = inventory.Items.Where(i => i != null && i.Class == "Maps").ToList();
            if (allMaps.Count > 0 && _tickCount % 200 == 0)
            {
                Log.DebugFormat("[SimpleMapBot] Found {0} total maps in inventory", allMaps.Count);
                foreach (var m in allMaps)
                {
                    var enabled = settings.IsMapEnabled(m.Name) ? "ENABLED" : "disabled";
                    Log.DebugFormat("[SimpleMapBot]   - {0} ({1})", m.Name, enabled);
                }
            }

            // Find first map that matches our enabled maps filter
            return inventory.Items.FirstOrDefault(item =>
            {
                if (item == null || item.Class != "Maps")
                    return false;

                // Check if this map is enabled in settings
                if (!settings.IsMapEnabled(item.Name))
                {
                    return false;
                }

                return true;
            });
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
            var deviceControl = LokiPoe.InGameState.MapDeviceUi.InventoryControl;
            var placeResult = deviceControl.FastMove(map.LocalId);

            if (placeResult != FastMoveResult.None)
            {
                Log.WarnFormat("[SimpleMapBot] Failed to place map: {0}", placeResult);
                return;
            }

            await Coroutine.Sleep(300);

            // Place scarabs if configured
            var settings = SimpleMapBotSettings.Instance;
            var selectedScarabs = settings.GetSelectedScarabs();

            if (selectedScarabs.Count > 0)
            {
                Log.InfoFormat("[SimpleMapBot] Placing {0} scarabs in device", selectedScarabs.Count);

                var scarabsPlaced = await PlaceScarabsInDevice(selectedScarabs);
                if (scarabsPlaced > 0)
                {
                    Log.InfoFormat("[SimpleMapBot] Successfully placed {0}/{1} scarabs", scarabsPlaced, selectedScarabs.Count);
                }
                else if (selectedScarabs.Count > 0)
                {
                    Log.Warn("[SimpleMapBot] Failed to place any scarabs - continuing without them");
                }

                await Coroutine.Sleep(300);
            }
            else
            {
                Log.Debug("[SimpleMapBot] No scarabs configured, skipping scarab placement");
            }

            // Activate device
            Log.Info("[SimpleMapBot] Activating map device");
            var activateResult = LokiPoe.InGameState.MapDeviceUi.Activate();

            if (activateResult != LokiPoe.InGameState.ActivateResult.None)
            {
                Log.WarnFormat("[SimpleMapBot] Failed to activate map device: {0}", activateResult);
                return;
            }

            await Coroutine.Sleep(500);

            // Close UI
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(200);

            Log.Info("[SimpleMapBot] Map device activated, waiting for portal");
        }

        /// <summary>
        /// Find scarabs in inventory matching the configured names
        /// </summary>
        private System.Collections.Generic.List<Item> FindConfiguredScarabsInInventory(System.Collections.Generic.List<string> scarabNames)
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
                return new System.Collections.Generic.List<Item>();

            var foundScarabs = new System.Collections.Generic.List<Item>();

            // For each configured scarab, find a matching item in inventory
            foreach (var scarabName in scarabNames)
            {
                var scarab = inventory.Items.FirstOrDefault(item =>
                    item != null &&
                    item.IsValid &&
                    item.Name != null &&
                    item.Name.Equals(scarabName, System.StringComparison.OrdinalIgnoreCase));

                if (scarab != null)
                {
                    foundScarabs.Add(scarab);
                    Log.DebugFormat("[SimpleMapBot] Found scarab in inventory: {0}", scarab.Name);
                }
                else
                {
                    Log.WarnFormat("[SimpleMapBot] Configured scarab not found in inventory: {0}", scarabName);
                }
            }

            return foundScarabs;
        }

        /// <summary>
        /// Place scarabs in the map device
        /// Returns the number of scarabs successfully placed
        /// </summary>
        private async Task<int> PlaceScarabsInDevice(System.Collections.Generic.List<string> scarabNames)
        {
            if (!LokiPoe.InGameState.MapDeviceUi.IsOpened)
            {
                Log.Warn("[SimpleMapBot] Cannot place scarabs - map device UI is not open");
                return 0;
            }

            // Find configured scarabs in inventory
            var scarabs = FindConfiguredScarabsInInventory(scarabNames);
            if (scarabs.Count == 0)
            {
                Log.Warn("[SimpleMapBot] No configured scarabs found in inventory");
                return 0;
            }

            // Place each scarab in the device
            int placed = 0;
            var deviceControl = LokiPoe.InGameState.MapDeviceUi.InventoryControl;

            foreach (var scarab in scarabs)
            {
                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                Log.InfoFormat("[SimpleMapBot] Placing scarab: {0}", scarab.Name);
                var result = deviceControl.FastMove(scarab.LocalId);

                if (result == FastMoveResult.None)
                {
                    placed++;
                    Log.DebugFormat("[SimpleMapBot] Placed scarab: {0}", scarab.Name);
                    await Coroutine.Sleep(150); // Small delay between placements
                }
                else
                {
                    Log.WarnFormat("[SimpleMapBot] Failed to place scarab {0}: {1}", scarab.Name, result);
                }
            }

            return placed;
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

            // Standard PoE inventory: 12 columns x 5 rows = 60 slots
            const int INVENTORY_COLS = 12;
            const int INVENTORY_ROWS = 5;
            bool[,] occupiedSlots = new bool[INVENTORY_COLS, INVENTORY_ROWS];

            // Mark occupied slots
            foreach (var item in inventory.Items)
            {
                if (item == null || !item.IsValid)
                    continue;

                int x = (int)item.LocationTopLeft.X;
                int y = (int)item.LocationTopLeft.Y;
                int width = (int)item.Size.X;
                int height = (int)item.Size.Y;

                // Mark all slots this item occupies
                for (int dx = 0; dx < width; dx++)
                {
                    for (int dy = 0; dy < height; dy++)
                    {
                        int slotX = x + dx;
                        int slotY = y + dy;

                        if (slotX < INVENTORY_COLS && slotY < INVENTORY_ROWS)
                        {
                            occupiedSlots[slotX, slotY] = true;
                        }
                    }
                }
            }

            // Count free slots
            int freeSlots = 0;
            for (int x = 0; x < INVENTORY_COLS; x++)
            {
                for (int y = 0; y < INVENTORY_ROWS; y++)
                {
                    if (!occupiedSlots[x, y])
                        freeSlots++;
                }
            }

            // Return if 5 or fewer slots available
            return freeSlots <= 5;
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
            if (inventory == null || inventory.Items.Count == 0)
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
                var itemsToStash = inventory.Items.ToList();

                foreach (var item in itemsToStash)
                {
                    if (item == null || item.Class == "Maps") // Don't stash maps
                        continue;

                    LokiPoe.ProcessHookManager.ClearAllKeyStates();

                    var err = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.FastMove(item.LocalId);

                    if (err != FastMoveResult.None)
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
