using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using Beasts.Core;
using Beasts.Configuration;
using Beasts.Services;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Open Map Phase: Handles the complete map opening workflow in hideout
    ///
    /// WORKFLOW:
    /// 1. Validate Requirements (map, scarabs, receptacle)
    /// 2. Restock if needed (from stash or Faustus)
    /// 3. Move to Map Device
    /// 4. Select Receptacle Type (Heist)
    /// 5. Insert Map
    /// 6. Insert Scarabs (1x Duplicating, 2x Herd)
    /// 7. Activate Map
    /// 8. Wait for portal/loading
    /// </summary>
    public class OpenMapPhase : IPhase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OpenMapPhase));

        public string Name => "OpenMap";

        private MapOpeningStep _currentStep = MapOpeningStep.ValidateRequirements;
        private int _stepAttempts = 0;

        // Material availability cache - REMOVED (was too strict)

        public bool CanExecute(GameContext context)
        {
            Log.Info($"[OpenMap] CanExecute check - IsInHideout: {context.Player.IsInHideout}, MapDeviceActive: {context.MapDevice.IsActive}");

            // Must be in hideout
            if (!context.Player.IsInHideout)
                return false;

            // Don't execute if map device is already active
            if (context.MapDevice.IsActive)
            {
                Log.Debug("[OpenMap] Map device is active, skipping");
                return false;
            }

            // Only requirement: Must have at least 1 portal scroll
            // Maps and scarabs can be in inventory, device, or stash - we'll check during execution
            bool hasPortalScrolls = context.Player.HasPortalScrolls;
            
            if (!hasPortalScrolls)
            {
                Log.Info("[OpenMap] Missing portal scrolls - PreparationPhase should restock");
                return false;
            }

            // Have portal scrolls - proceed to check device and stash for maps/scarabs
            Log.Info("[OpenMap] Have portal scrolls - ready to check device and stash for maps/scarabs");
            return true;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            Log.Info($"[OpenMap] ===== EXECUTING Phase - Step: {_currentStep}, Attempt: {_stepAttempts} =====");

            switch (_currentStep)
            {
                case MapOpeningStep.ValidateRequirements:
                    return await ValidateRequirements(context);

                case MapOpeningStep.WithdrawMap:
                    return await WithdrawMapFromStash();

                case MapOpeningStep.MoveToDevice:
                    return await MoveToMapDevice(context);

                case MapOpeningStep.InsertMap:
                    return await InsertMap(context);

                case MapOpeningStep.InsertScarabs:
                    return await InsertScarabs(context);

                case MapOpeningStep.ActivateDevice:
                    return await ActivateMapDevice(context);

                case MapOpeningStep.WaitForPortal:
                    return await WaitForPortal(context);

                case MapOpeningStep.EnterPortal:
                    return await EnterPortal(context);

                default:
                    return PhaseResult.Failed($"Unknown step: {_currentStep}");
            }
        }

        private async Task<PhaseResult> ValidateRequirements(GameContext context)
        {
            var mapSettings = BeastRoutineSettings.Instance.MapDevice;
            var stashSettings = BeastRoutineSettings.Instance.Stashing;

            // CRITICAL: Ensure all UIs are closed before proceeding
            // This prevents conflicts when transitioning from stash withdrawal to map device
            if (LokiPoe.InGameState.StashUi.IsOpened ||
                LokiPoe.InGameState.MapDeviceUi.IsOpened)
            {
                Log.Info("[OpenMap] Closing open UIs before validation");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(500);

                // Verify closed - with attempt tracking to prevent infinite loops
                if (LokiPoe.InGameState.StashUi.IsOpened ||
                    LokiPoe.InGameState.MapDeviceUi.IsOpened)
                {
                    _stepAttempts++;
                    if (_stepAttempts > 10)
                    {
                        Log.Error("[OpenMap] Cannot close UIs after 10 attempts - please close game UIs manually");
                        return PhaseResult.Failed("Cannot close blocking UIs - restart bot or close game UIs manually");
                    }
                    Log.DebugFormat("[OpenMap] UIs still open after close (attempt {0}/10), waiting...", _stepAttempts);
                    return PhaseResult.InProgress("Waiting for UIs to close");
                }
                else
                {
                    // Reset attempts counter when successfully closed
                    _stepAttempts = 0;
                }
            }

            Log.Info("[OpenMap] Validation complete - proceeding to check map device first");

            // Always go to device first to check what's already loaded
            // Device check happens in InsertMap/InsertScarabs steps
            _currentStep = MapOpeningStep.MoveToDevice;
            _stepAttempts = 0;
            return PhaseResult.InProgress("Moving to device to check what's needed");
        }

        private async Task<PhaseResult> MoveToMapDevice(GameContext context)
        {
            // IMPORTANT: Always close device UI when entering this step
            // This prevents issues when transitioning from WithdrawMap back to device
            bool mapDeviceOpen = LokiPoe.InGameState.MapDeviceUi.IsOpened;
            bool masterDeviceOpen = LokiPoe.InGameState.MasterDeviceUi.IsOpened;

            if (mapDeviceOpen || masterDeviceOpen)
            {
                Log.Info("[OpenMap] Device UI unexpectedly open, closing before navigation");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(300);
                // Don't transition yet - let the bot actually move to device first
                return PhaseResult.InProgress("Closing device UI...");
            }

            var mapDevice = FindMapDevice();

            if (mapDevice == null)
            {
                _stepAttempts++;
                if (_stepAttempts > 10)
                {
                    return PhaseResult.Failed("Map device not found in hideout");
                }

                Log.Warn("[OpenMap] Map device not found, searching...");
                await Coroutine.Sleep(500);
                return PhaseResult.InProgress("Searching for map device");
            }

            float distance = mapDevice.Distance;

            if (distance <= 20f) // Within interaction range
            {
                Log.Info("[OpenMap] Reached map device");
                // Skip receptacle selection - go straight to inserting map
                _currentStep = MapOpeningStep.InsertMap;
                _stepAttempts = 0;
                return PhaseResult.InProgress("At map device, ready to insert items");
            }

            Log.DebugFormat("[OpenMap] Moving to map device (Distance: {0:F1})", distance);
            PlayerMoverManager.Current.MoveTowards(mapDevice.Position);

            await Coroutine.Sleep(100);
            return PhaseResult.InProgress($"Moving to map device ({distance:F1}m)");
        }

        private async Task<PhaseResult> InsertMap(GameContext context)
        {
            Log.Info("[OpenMap] Checking map device - need to open UI first");

            // MUST open device UI first to check what's inside
            bool mapDeviceOpen = LokiPoe.InGameState.MapDeviceUi.IsOpened;
            bool masterDeviceOpen = LokiPoe.InGameState.MasterDeviceUi.IsOpened;

            if (!mapDeviceOpen && !masterDeviceOpen)
            {
                Log.Info("[OpenMap] Opening map device UI to check contents");
                // Open map device UI using MapDeviceService
                if (!await MapDeviceService.OpenMapDevice())
                {
                    _stepAttempts++;
                    if (_stepAttempts > 5)
                    {
                        Log.Error("[OpenMap] Failed to open map device after 5 attempts");
                        return PhaseResult.Failed("Failed to open map device");
                    }

                    Log.WarnFormat("[OpenMap] Failed to open map device (attempt {0}/5)", _stepAttempts);
                    await Coroutine.Sleep(500);
                    return PhaseResult.InProgress("Retrying map device opening...");
                }
                
                await Coroutine.Sleep(300); // Wait for UI to fully load
                return PhaseResult.InProgress("Waiting for device UI to load...");
            }
            
            // NOW check if map device already has a map loaded
            bool hasMapInDevice = CheckMapDeviceForMaps();
            
            if (hasMapInDevice)
            {
                Log.Info("[OpenMap] Map already loaded in map device - skipping to scarabs");
                _currentStep = MapOpeningStep.InsertScarabs;
                _stepAttempts = 0;
                return PhaseResult.InProgress("Map already in device");
            }

            Log.Info("[OpenMap] No map in device - checking inventory for map");

            var mapItem = FindMapInInventory();
            
            if (mapItem == null)
            {
                // NO MAP IN INVENTORY - Need to withdraw from stash
                // CRITICAL: Close map device UI before transitioning to withdrawal
                if (LokiPoe.InGameState.MapDeviceUi.IsOpened || LokiPoe.InGameState.MasterDeviceUi.IsOpened)
                {
                    LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                    await Coroutine.Sleep(500);
                }
                
                _currentStep = MapOpeningStep.WithdrawMap;
                _stepAttempts = 0;
                return PhaseResult.InProgress("No map in inventory, withdrawing from stash");
            }

            var tier = ExtractMapTier(mapItem);
            Log.InfoFormat("[OpenMap] Inserting map from inventory: {0} (Tier: {1})", mapItem.Name, tier);

            // Verify device UI is still open (either MapDeviceUi or MasterDeviceUi)
            mapDeviceOpen = LokiPoe.InGameState.MapDeviceUi.IsOpened;
            masterDeviceOpen = LokiPoe.InGameState.MasterDeviceUi.IsOpened;

            if (!mapDeviceOpen && !masterDeviceOpen)
            {
                Log.Error("[OpenMap] Map device UI unexpectedly closed");
                return PhaseResult.Failed("Map device UI closed unexpectedly");
            }

            // Check if device already has items - if user pre-loaded, skip clearing
            var deviceControl = LokiPoe.InGameState.MapDeviceUi.InventoryControl;
            bool deviceHasItems = deviceControl?.Inventory?.Items?.Count > 0;

            if (deviceHasItems)
            {
                Log.Info("[OpenMap] Map device already has items loaded, skipping clear step");
            }
            else
            {
                // Clear any existing items from device
                if (!await MapDeviceService.ClearMapDevice())
                {
                    Log.Warn("[OpenMap] Failed to clear map device, continuing anyway");
                }
            }

            // Place map into device using MapDeviceService
            if (!await MapDeviceService.PlaceItemInDevice(mapItem))
            {
                _stepAttempts++;
                if (_stepAttempts > 5)
                {
                    Log.Error("[OpenMap] Failed to place map in device after 5 attempts");
                    return PhaseResult.Failed("Failed to place map in device");
                }

                Log.WarnFormat("[OpenMap] Failed to place map (attempt {0}/5)", _stepAttempts);
                await Coroutine.Sleep(500);
                return PhaseResult.InProgress("Retrying map placement...");
            }

            Log.Info("[OpenMap] Map placed in device successfully");
            _currentStep = MapOpeningStep.InsertScarabs;
            _stepAttempts = 0;
            return PhaseResult.InProgress("Map inserted, moving to scarabs");
        }

        private async Task<PhaseResult> InsertScarabs(GameContext context)
        {
            Log.Info("[OpenMap] Checking map device for existing scarabs");

            // First, check if map device UI already has scarabs loaded (user may have loaded manually)
            if (CheckMapDeviceForScarabs())
            {
                Log.Info("[OpenMap] Scarabs already loaded in map device (manual load detected)");
                _currentStep = MapOpeningStep.ActivateDevice;
                _stepAttempts = 0;
                return PhaseResult.InProgress("Scarabs ready (manual)");
            }

            // Also check context (legacy check)
            if (context.MapDevice.HasAllScarabs)
            {
                Log.Debug("[OpenMap] All scarabs already inserted (context check)");
                _currentStep = MapOpeningStep.ActivateDevice;
                _stepAttempts = 0;
                return PhaseResult.InProgress("Scarabs ready");
            }

            var settings = BeastRoutineSettings.Instance.MapDevice;

            Log.Info("[OpenMap] Inserting scarabs from inventory");
            Log.InfoFormat("[OpenMap] Need: {0}x Duplicating, {1}x Herd",
                settings.MinDuplicatingScarabs, settings.MinHerdScarabs);

            // Get inventory
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
            {
                Log.Error("[OpenMap] Cannot access inventory");
                return PhaseResult.Failed("Cannot access inventory");
            }

            // Find scarabs to insert
            var duplicatingScarabs = inventory.Items
                .Where(i => i != null && i.IsValid &&
                           i.Name != null &&
                           i.Name.ToLower().Contains("bestiary scarab of duplicating"))
                .Take(settings.MinDuplicatingScarabs)
                .ToList();

            var herdScarabs = inventory.Items
                .Where(i => i != null && i.IsValid &&
                           i.Name != null &&
                           i.Name.ToLower().Contains("bestiary scarab of the herd"))
                .Take(settings.MinHerdScarabs)
                .ToList();

            if (duplicatingScarabs.Count < settings.MinDuplicatingScarabs ||
                herdScarabs.Count < settings.MinHerdScarabs)
            {
                Log.ErrorFormat("[OpenMap] Not enough scarabs (have {0}/{1} Duplicating, {2}/{3} Herd)",
                    duplicatingScarabs.Count, settings.MinDuplicatingScarabs,
                    herdScarabs.Count, settings.MinHerdScarabs);
                return PhaseResult.Failed("Insufficient scarabs");
            }

            // Insert all scarabs using MapDeviceService
            var allScarabs = duplicatingScarabs.Concat(herdScarabs).ToList();

            foreach (var scarab in allScarabs)
            {
                Log.InfoFormat("[OpenMap] Placing scarab: {0}", scarab.Name);

                if (!await MapDeviceService.PlaceItemInDevice(scarab))
                {
                    Log.WarnFormat("[OpenMap] Failed to place scarab: {0}", scarab.Name);
                    _stepAttempts++;
                    if (_stepAttempts > 10)
                    {
                        Log.Error("[OpenMap] Failed to insert scarabs after too many attempts");
                        return PhaseResult.Failed("Scarab insertion failed");
                    }
                    await Coroutine.Sleep(500);
                    return PhaseResult.InProgress("Retrying scarab insertion...");
                }

                await Coroutine.Sleep(100); // Small delay between scarabs
            }

            Log.Info("[OpenMap] Successfully inserted all required scarabs");
            _currentStep = MapOpeningStep.ActivateDevice;
            _stepAttempts = 0;
            return PhaseResult.InProgress("Scarabs inserted");
        }

        private async Task<PhaseResult> ActivateMapDevice(GameContext context)
        {
            Log.Info("[OpenMap] Activating map device");

            bool masterDeviceOpen = LokiPoe.InGameState.MasterDeviceUi.IsOpened;
            bool mapDeviceOpen = LokiPoe.InGameState.MapDeviceUi.IsOpened;

            // If MasterDeviceUi is open, try to select Heist mod
            if (masterDeviceOpen)
            {
                try
                {
                    var deviceOptions = LokiPoe.InGameState.MasterDeviceUi.ZanaMods;
                    var heistMod = deviceOptions.FirstOrDefault(opt =>
                        opt.IsEnabled && opt.Name.Contains("Heist"));

                    if (heistMod != null)
                    {
                        Log.Info("[OpenMap] Selecting Heist modifier");
                        LokiPoe.InGameState.MasterDeviceUi.SelectZanaMod(heistMod);
                        await Coroutine.Sleep(300); // Wait for selection
                    }
                    else
                    {
                        Log.Info("[OpenMap] Heist modifier not available or not enabled");
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Warn($"[OpenMap] Failed to select Heist modifier: {ex.Message}");
                }

                // Activate using MasterDeviceUi
                Log.Info("[OpenMap] Activating MasterDeviceUi");
                LokiPoe.ProcessHookManager.ClearAllKeyStates();
                await Coroutine.Sleep(500);

                var activateResult = LokiPoe.InGameState.MasterDeviceUi.Activate();
                if (activateResult != LokiPoe.InGameState.ActivateResult.None)
                {
                    _stepAttempts++;
                    if (_stepAttempts > 5)
                    {
                        Log.ErrorFormat("[OpenMap] Failed to activate MasterDevice: {0}", activateResult);
                        return PhaseResult.Failed("Device activation failed");
                    }

                    Log.WarnFormat("[OpenMap] Failed to activate (attempt {0}/5): {1}", _stepAttempts, activateResult);
                    await Coroutine.Sleep(500);
                    return PhaseResult.InProgress("Retrying device activation...");
                }

                Log.Info("[OpenMap] MasterDeviceUi activated successfully");
            }
            else if (mapDeviceOpen)
            {
                // Use standard MapDeviceService for regular device
                if (!await MapDeviceService.ActivateMapDevice())
                {
                    _stepAttempts++;
                    if (_stepAttempts > 5)
                    {
                        Log.Error("[OpenMap] Failed to activate map device after 5 attempts");
                        return PhaseResult.Failed("Device activation failed");
                    }

                    Log.WarnFormat("[OpenMap] Failed to activate device (attempt {0}/5)", _stepAttempts);
                    await Coroutine.Sleep(500);
                    return PhaseResult.InProgress("Retrying device activation...");
                }

                Log.Info("[OpenMap] MapDeviceUi activated successfully");
            }
            else
            {
                Log.Error("[OpenMap] Neither MapDeviceUi nor MasterDeviceUi is opened!");
                return PhaseResult.Failed("No device UI is open");
            }

            Log.Info("[OpenMap] Map device activated successfully, waiting for portal");
            _currentStep = MapOpeningStep.WaitForPortal;
            _stepAttempts = 0;
            return PhaseResult.InProgress("Device activated, portal spawning...");
        }

        private async Task<PhaseResult> WaitForPortal(GameContext context)
        {
            var portal = MapDeviceService.FindMapPortal();

            if (portal == null)
            {
                _stepAttempts++;
                if (_stepAttempts > 30) // 3 second timeout at 100ms intervals
                {
                    return PhaseResult.Failed("Portal did not appear after 3 seconds");
                }

                await Coroutine.Sleep(100);
                return PhaseResult.Wait("Waiting for portal to appear...");
            }

            Log.InfoFormat("[OpenMap] Portal appeared! (Distance: {0:F1})", portal.Distance);
            _currentStep = MapOpeningStep.EnterPortal;
            _stepAttempts = 0;
            return PhaseResult.InProgress("Portal ready");
        }

        private async Task<PhaseResult> EnterPortal(GameContext context)
        {
            var portal = MapDeviceService.FindMapPortal();

            if (portal == null)
            {
                _stepAttempts++;
                if (_stepAttempts > 5)
                {
                    return PhaseResult.Failed("Portal disappeared");
                }

                await Coroutine.Sleep(500);
                return PhaseResult.InProgress("Searching for portal...");
            }

            float distance = portal.Distance;

            // Move closer if needed
            if (distance > 30f)
            {
                PlayerMoverManager.Current.MoveTowards(portal.Position);
                await Coroutine.Sleep(100);
                return PhaseResult.InProgress($"Moving to portal ({distance:F1}m)");
            }

            // Enter the portal using PortalService
            Log.Info("[OpenMap] Entering map portal");

            if (!await PortalService.EnterPortal(portal))
            {
                _stepAttempts++;
                if (_stepAttempts > 5)
                {
                    return PhaseResult.Failed("Failed to enter portal after 5 attempts");
                }

                Log.Warn("[OpenMap] Failed to enter portal, retrying");
                await Coroutine.Sleep(300);
                return PhaseResult.InProgress("Retrying portal entry...");
            }

            Log.Info("[OpenMap] Successfully entered map portal!");
            await Coroutine.Sleep(2000); // Wait for loading screen

            return PhaseResult.Success("Map opened and entered");
        }

        public void OnExit()
        {
            Log.Debug($"[OpenMap] Exiting phase at step: {_currentStep}");

            // Close any open UIs before exiting phase (e.g., stash from restocking)
            if (LokiPoe.InGameState.StashUi.IsOpened ||
                LokiPoe.InGameState.MapDeviceUi.IsOpened)
            {
                Log.Info("[OpenMap] Closing open UIs before phase exit");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            }

            // Reset state for next time
            _currentStep = MapOpeningStep.ValidateRequirements;
            _stepAttempts = 0;
        }

        /// <summary>
        /// Material cache removed - no longer needed
        /// </summary>

        #region Helper Methods

        /// <summary>
        /// Count scarabs of a specific type in the player's inventory
        /// </summary>
        private int CountScarabsInInventory(string scarabName)
        {
            try
            {
                var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
                if (inventory == null)
                {
                    Log.Warn("[OpenMap] Could not access inventory");
                    return 0;
                }

                var count = inventory.Items
                    .Where(item => item != null && item.IsValid)
                    .Count(item => item.Name != null &&
                                  item.Name.ToLower().Contains(scarabName.ToLower()));

                Log.DebugFormat("[OpenMap] Found {0}x {1} in inventory", count, scarabName);
                return count;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[OpenMap] Error counting scarabs: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Find the map device in hideout
        /// Pattern verified from ObjectExplorer examples
        /// </summary>
        private NetworkObject FindMapDevice()
        {
            try
            {
                // Use .OfType<T>() for better type filtering (from ObjectExplorer pattern)
                return LokiPoe.ObjectManager.Objects
                    .Where(obj => obj.IsValid &&
                                 obj.Distance < 200f &&
                                 (obj.Metadata.Contains("MapDevice") ||
                                  obj.Name.ToLower().Contains("map device")))
                    .OrderBy(obj => obj.Distance)
                    .FirstOrDefault();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[OpenMap] Error finding map device: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find Faustus NPC in hideout
        /// Pattern verified from ObjectExplorer examples - uses .OfType<Npc>()
        /// </summary>
        private Npc FindFaustus()
        {
            try
            {
                // Proper pattern from ObjectExplorer: use .OfType<Npc>() for type filtering
                return LokiPoe.ObjectManager.Objects
                    .OfType<Npc>()
                    .Where(npc => npc.IsValid &&
                                 npc.Distance < 300f &&
                                 (npc.Name.ToLower().Contains("faustus") ||
                                  npc.Metadata.ToLower().Contains("faustus")))
                    .OrderBy(npc => npc.Distance)
                    .FirstOrDefault();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[OpenMap] Error finding Faustus: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find stash chest in hideout
        /// Pattern verified from ObjectExplorer and ExitAndStashPhase.cs:629
        /// </summary>
        private NetworkObject FindStashChest()
        {
            try
            {
                return LokiPoe.ObjectManager.Objects
                    .Where(obj => obj.IsValid &&
                                 obj.Distance < 200f &&
                                 (obj.Metadata.Contains("Stash") ||
                                  obj.Name.ToLower().Contains("stash")))
                    .OrderBy(obj => obj.Distance)
                    .FirstOrDefault();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[OpenMap] Error finding stash: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find map portal in hideout after activation
        /// Pattern verified from ObjectExplorer
        /// </summary>
        private NetworkObject FindMapPortal()
        {
            try
            {
                return LokiPoe.ObjectManager.Objects
                    .Where(obj => obj.IsValid &&
                                 obj.Distance < 100f &&
                                 (obj.Metadata.Contains("Portal") ||
                                  obj.Metadata.Contains("MapDevice") && obj.Metadata.Contains("Active")))
                    .OrderBy(obj => obj.Distance)
                    .FirstOrDefault();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[OpenMap] Error finding portal: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find a suitable map in the player's inventory
        /// Prioritizes based on settings (preferred map, rotation, tier, etc.)
        /// </summary>
        private Item FindMapInInventory()
        {
            try
            {
                var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
                if (inventory == null)
                {
                    Log.Warn("[OpenMap] Could not access inventory");
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
                    Log.Info("[OpenMap] No maps found in inventory");
                    return null;
                }

                // Filter by tier if specified
                if (settings.MinMapTier > 0 || settings.MaxMapTier < 16)
                {
                    maps = maps.Where(m =>
                    {
                        int tier = ExtractMapTier(m);
                        if (tier == 0)
                        {
                            // Couldn't determine tier, include it to be safe
                            Log.WarnFormat("[OpenMap] Could not extract tier from map: {0}", m.Name);
                            return true;
                        }

                        bool inRange = tier >= settings.MinMapTier && tier <= settings.MaxMapTier;
                        if (!inRange)
                        {
                            Log.DebugFormat("[OpenMap] Skipping map {0} (Tier {1}, want {2}-{3})",
                                m.Name, tier, settings.MinMapTier, settings.MaxMapTier);
                        }
                        return inRange;
                    }).ToList();

                    if (!maps.Any())
                    {
                        Log.WarnFormat("[OpenMap] No maps found matching tier range {0}-{1}",
                            settings.MinMapTier, settings.MaxMapTier);
                        return null;
                    }
                }

                // Try to find preferred map if specified
                if (!string.IsNullOrEmpty(settings.PreferredMap))
                {
                    var preferredMap = maps.FirstOrDefault(m =>
                        m.Name.ToLower().Contains(settings.PreferredMap.ToLower()));

                    if (preferredMap != null)
                    {
                        Log.InfoFormat("[OpenMap] Found preferred map: {0}", preferredMap.Name);
                        return preferredMap;
                    }
                }

                // Handle map rotation if enabled
                if (settings.EnableMapRotation && !string.IsNullOrEmpty(settings.MapRotationList))
                {
                    var rotationMaps = settings.MapRotationList
                        .Split(',')
                        .Select(m => m.Trim())
                        .Where(m => !string.IsNullOrEmpty(m))
                        .ToList();

                    if (rotationMaps.Any())
                    {
                        // Try to find maps in rotation list order
                        foreach (var rotationMapName in rotationMaps)
                        {
                            var rotationMap = maps.FirstOrDefault(m =>
                                m.Name.ToLower().Contains(rotationMapName.ToLower()));

                            if (rotationMap != null)
                            {
                                Log.InfoFormat("[OpenMap] Found rotation map: {0}", rotationMap.Name);
                                return rotationMap;
                            }
                        }

                        Log.Warn("[OpenMap] No maps from rotation list found, using any available map");
                    }
                }

                // Otherwise, return first available map
                var selectedMap = maps.First();
                Log.InfoFormat("[OpenMap] Selected map: {0}", selectedMap.Name);
                return selectedMap;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[OpenMap] Error finding map in inventory: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extract map tier from item properties
        /// </summary>
        private int ExtractMapTier(Item map)
        {
            try
            {
                // Use the MapTier property directly (from DreamPoeBot API)
                if (map.MapTier > 0)
                {
                    return map.MapTier;
                }

                // Method 3: Parse from item name (fallback)
                // Some maps have tier in name like "Strand Map (Tier 16)"
                if (map.Name != null)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(map.Name, @"Tier\s*(\d+)|T(\d+)");
                    if (match.Success)
                    {
                        string tierStr = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                        if (int.TryParse(tierStr, out int tier))
                        {
                            Log.DebugFormat("[OpenMap] Extracted tier {0} from name: {1}", tier, map.Name);
                            return tier;
                        }
                    }
                }

                Log.WarnFormat("[OpenMap] Could not extract tier from map: {0}", map.Name);
                return 0; // Unknown tier
            }
            catch (System.Exception ex)
            {
                Log.Error($"[OpenMap] Error extracting map tier: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Check if map device has maps in storage slots
        /// Some map devices have extra slots where maps can be stored
        /// </summary>
        /// <summary>
        /// Check if map device UI has a map already loaded
        /// </summary>
        private bool CheckMapDeviceForMaps()
        {
            try
            {
                // Check both MapDeviceUi (standard) and MasterDeviceUi (witness selection)
                bool mapDeviceOpen = LokiPoe.InGameState.MapDeviceUi.IsOpened;
                bool masterDeviceOpen = LokiPoe.InGameState.MasterDeviceUi.IsOpened;

                if (!mapDeviceOpen && !masterDeviceOpen)
                {
                    Log.Debug("[OpenMap] Neither MapDeviceUi nor MasterDeviceUi is opened");
                    return false;
                }

                // Try MapDeviceUi first
                var deviceControl = mapDeviceOpen 
                    ? LokiPoe.InGameState.MapDeviceUi.InventoryControl 
                    : LokiPoe.InGameState.MasterDeviceUi.InventoryControl;

                if (deviceControl == null)
                {
                    Log.Debug("[OpenMap] Device control is null");
                    return false;
                }

                var deviceInventory = deviceControl.Inventory;
                if (deviceInventory == null)
                {
                    Log.Debug("[OpenMap] Device inventory is null");
                    return false;
                }
                
                if (deviceInventory.Items == null)
                {
                    Log.Debug("[OpenMap] Device inventory items is null");
                    return false;
                }

                int totalItems = deviceInventory.Items.Count;
                Log.InfoFormat("[OpenMap] Device has {0} total items (using {1})", 
                    totalItems, mapDeviceOpen ? "MapDeviceUi" : "MasterDeviceUi");

                // Log all items in device for debugging
                foreach (var item in deviceInventory.Items)
                {
                    if (item != null && item.IsValid)
                    {
                        Log.InfoFormat("[OpenMap] Device item: {0} (Class: {1})", 
                            item.Name ?? "Unknown", item.Class ?? "Unknown");
                    }
                }

                // Check if any item in device is a map
                bool hasMap = deviceInventory.Items.Any(item =>
                    item != null &&
                    item.IsValid &&
                    item.Class != null &&
                    item.Class.ToLower().Contains("map"));

                if (hasMap)
                {
                    Log.Info("[OpenMap] ✓ Map device HAS a map loaded");
                }
                else
                {
                    Log.Info("[OpenMap] ✗ Map device does NOT have a map");
                }

                return hasMap;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[OpenMap] Error checking map device for maps: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if map device UI has scarabs already loaded
        /// </summary>
        private bool CheckMapDeviceForScarabs()
        {
            try
            {
                // Check both MapDeviceUi (standard) and MasterDeviceUi (witness selection)
                bool mapDeviceOpen = LokiPoe.InGameState.MapDeviceUi.IsOpened;
                bool masterDeviceOpen = LokiPoe.InGameState.MasterDeviceUi.IsOpened;

                if (!mapDeviceOpen && !masterDeviceOpen)
                {
                    Log.Debug("[OpenMap] Neither MapDeviceUi nor MasterDeviceUi is opened");
                    return false;
                }

                // Try MapDeviceUi first
                var deviceControl = mapDeviceOpen 
                    ? LokiPoe.InGameState.MapDeviceUi.InventoryControl 
                    : LokiPoe.InGameState.MasterDeviceUi.InventoryControl;

                if (deviceControl == null)
                {
                    Log.Debug("[OpenMap] Device control is null");
                    return false;
                }

                var deviceInventory = deviceControl.Inventory;
                if (deviceInventory == null || deviceInventory.Items == null)
                {
                    Log.Debug("[OpenMap] Device inventory is null or has no items");
                    return false;
                }

                // Check if any item in device is a scarab
                bool hasScarabs = deviceInventory.Items.Any(item =>
                    item != null &&
                    item.IsValid &&
                    item.Name != null &&
                    item.Name.ToLower().Contains("scarab"));

                if (hasScarabs)
                {
                    int scarabCount = deviceInventory.Items.Count(item =>
                        item != null &&
                        item.IsValid &&
                        item.Name != null &&
                        item.Name.ToLower().Contains("scarab"));

                    Log.InfoFormat("[OpenMap] ✓ Map device HAS {0} scarab(s) loaded", scarabCount);
                }
                else
                {
                    Log.Info("[OpenMap] ✗ Map device does NOT have scarabs");
                }

                return hasScarabs;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[OpenMap] Error checking map device for scarabs: {ex.Message}");
                return false;
            }
        }

        private Item FindMapInDeviceStorage()
        {
            try
            {
                // TODO: Investigate if DreamPoeBot API exposes map device storage slots
                // This would require accessing the map device UI and checking slots
                //
                // For now, return null as this feature may not be available in API
                // If it exists, it would be similar to checking stash tabs

                Log.Debug("[OpenMap] Map device storage check not yet implemented");
                return null;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[OpenMap] Error checking map device storage: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Withdraw a map from stash when none available in inventory
        /// </summary>
        private async Task<PhaseResult> WithdrawMapFromStash()
        {
            var settings = BeastRoutineSettings.Instance.MapDevice;

            // IMPORTANT: Check if we already have a map - don't open stash if not needed
            var existingMap = FindMapInInventory();
            if (existingMap != null)
            {
                Log.InfoFormat("[OpenMap] Map already in inventory: {0}, skipping withdrawal", existingMap.Name);
                // Go directly to MoveToDevice since we now have a map
                _currentStep = MapOpeningStep.MoveToDevice;
                _stepAttempts = 0;
                return PhaseResult.InProgress("Map already available, returning to device");
            }

            Log.Info("[OpenMap] No map in inventory, will withdraw from stash");

            // Step 1: Find and navigate to stash
            var stash = FindStashChest();
            Log.Info($"[OpenMap] Stash object found: {stash != null}, Distance: {stash?.Distance ?? -1}");
            if (stash == null)
            {
                _stepAttempts++;
                if (_stepAttempts > 10)
                {
                    Log.Error("[OpenMap] Stash not found in hideout");
                    return PhaseResult.Failed("Stash not found");
                }

                Log.Warn("[OpenMap] Stash not found, searching...");
                await Coroutine.Sleep(500);
                return PhaseResult.InProgress("Searching for stash...");
            }

            // Step 2: Move to stash
            if (stash.Distance > 15f)
            {
                PlayerMoverManager.Current.MoveTowards(stash.Position);
                await Coroutine.Sleep(100);
                return PhaseResult.InProgress($"Moving to stash ({stash.Distance:F1}m)");
            }

            // Step 3: Close any open UIs before opening stash (prevents UI conflicts)
            if (LokiPoe.InGameState.MapDeviceUi.IsOpened ||
                LokiPoe.InGameState.StashUi.IsOpened)
            {
                Log.Info("[OpenMap] Closing open UIs before opening stash");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(300);
            }

            // Verify all UIs are closed - with a maximum attempt limit to prevent infinite loops
            int verifyAttempts = 0;
            while ((LokiPoe.InGameState.MapDeviceUi.IsOpened ||
                    LokiPoe.InGameState.StashUi.IsOpened) &&
                   verifyAttempts < 5)
            {
                Log.Debug("[OpenMap] Waiting for UIs to close...");
                await Coroutine.Sleep(200);
                verifyAttempts++;
            }

            // If UIs still won't close after 5 attempts, log warning and continue
            if (verifyAttempts >= 5)
            {
                Log.Warn("[OpenMap] UIs did not close after 5 attempts - proceeding anyway");
                _stepAttempts++;
                if (_stepAttempts > 3)
                {
                    return PhaseResult.Failed("Cannot close blocking UIs - restart bot or close game UIs manually");
                }
                return PhaseResult.InProgress("Retrying UI close...");
            }

            // Step 4: Open stash if not already open
            if (!LokiPoe.InGameState.StashUi.IsOpened)
            {
                Log.Info("[OpenMap] Stash is not opened, attempting to open...");
                await Coroutines.FinishCurrentAction();
                var interactResult = await Coroutines.InteractWith(stash);

                if (!interactResult)
                {
                    _stepAttempts++;
                    if (_stepAttempts > 5)
                    {
                        return PhaseResult.Failed("Failed to open stash");
                    }

                    Log.Warn($"[OpenMap] Failed to interact with stash (attempt {_stepAttempts}/5)");
                    await Coroutine.Sleep(300);
                    return PhaseResult.InProgress("Opening stash...");
                }

                Log.Info("[OpenMap] Stash interaction initiated, waiting for UI...");
                await Coroutine.Sleep(500); // Wait for stash UI
                return PhaseResult.InProgress("Waiting for stash UI...");
            }

            Log.Info("[OpenMap] STASH IS OPEN! Beginning tab search...");

            // Step 5: Search all stash tabs for suitable maps
            Log.InfoFormat("[OpenMap] Searching stash for maps (tier {0}-{1}, preferred: {2})",
                settings.MinMapTier, settings.MaxMapTier,
                string.IsNullOrEmpty(settings.PreferredMap) ? "any" : settings.PreferredMap);

            var tabControl = LokiPoe.InGameState.StashUi.TabControl;
            if (tabControl == null)
            {
                Log.Error("[OpenMap] Cannot access stash tab control");
                return PhaseResult.Failed("Stash tab control unavailable");
            }

            // Iterate through stash tabs to find maps
            // Note: TabControl doesn't expose TotalTabs, so we'll try up to 100 tabs
            const int maxTabs = 100;
            Log.Info($"[OpenMap] Starting tab iteration (max {maxTabs} tabs)");
            for (int tabIndex = 0; tabIndex < maxTabs; tabIndex++)
            {
                // Switch to tab
                if (tabControl.CurrentTabIndex != tabIndex)
                {
                    Log.Debug($"[OpenMap] Switching to tab index {tabIndex}");
                    tabControl.SwitchToTabMouse(tabIndex);
                    await Coroutine.Sleep(200); // Wait for tab to load
                }

                var tabName = tabControl.CurrentTabName;
                var stashInventory = LokiPoe.InGameState.StashUi.InventoryControl?.Inventory;

                Log.Debug($"[OpenMap] Tab {tabIndex}: Name='{tabName}', InventoryValid={stashInventory != null}");

                if (stashInventory == null)
                {
                    Log.Debug($"[OpenMap] Tab {tabIndex} has null inventory, skipping");
                    continue;
                }

                // Find all maps in this tab
                var mapsInTab = stashInventory.Items
                    .Where(item => item != null &&
                                  item.IsValid &&
                                  item.Class != null &&
                                  item.Class.ToLower().Contains("map"))
                    .ToList();

                if (!mapsInTab.Any())
                    continue;

                // Filter by tier if specified
                var suitableMaps = mapsInTab;
                if (settings.MinMapTier > 0 || settings.MaxMapTier < 16)
                {
                    suitableMaps = mapsInTab.Where(m =>
                    {
                        int tier = ExtractMapTier(m);
                        return tier >= settings.MinMapTier && tier <= settings.MaxMapTier;
                    }).ToList();
                }

                if (!suitableMaps.Any())
                    continue;

                // Try to find preferred map first
                Item mapToWithdraw = null;
                if (!string.IsNullOrEmpty(settings.PreferredMap))
                {
                    mapToWithdraw = suitableMaps.FirstOrDefault(m =>
                        m.Name.ToLower().Contains(settings.PreferredMap.ToLower()));
                }

                // Otherwise take first suitable map
                if (mapToWithdraw == null)
                    mapToWithdraw = suitableMaps.First();

                // Withdraw the map
                Log.InfoFormat("[OpenMap] Withdrawing map: {0} from tab: {1}", mapToWithdraw.Name, tabName);

                LokiPoe.ProcessHookManager.ClearAllKeyStates();
                var fastMoveResult = LokiPoe.InGameState.StashUi.InventoryControl.FastMove(mapToWithdraw.LocalId);

                if (fastMoveResult == FastMoveResult.None)
                {
                    Log.Info("[OpenMap] Map withdrawn successfully");

                    // Close stash using escape key
                    Log.Debug("[OpenMap] Closing stash UI...");
                    LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                    await Coroutine.Sleep(500);

                    // Simple verification - don't spam close if it's not working
                    if (LokiPoe.InGameState.StashUi.IsOpened)
                    {
                        Log.Debug("[OpenMap] Stash still open after first close, retrying once");
                        LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                        await Coroutine.Sleep(500);

                        if (LokiPoe.InGameState.StashUi.IsOpened)
                        {
                            Log.Warn("[OpenMap] Stash did not close after withdrawal, proceeding anyway");
                        }
                    }
                    else
                    {
                        Log.Debug("[OpenMap] Stash closed successfully");
                    }

                    // Go back to device to insert the map we just withdrew
                    _currentStep = MapOpeningStep.MoveToDevice;
                    _stepAttempts = 0;
                    return PhaseResult.InProgress("Map withdrawn, returning to device...");
                }
                else
                {
                    Log.WarnFormat("[OpenMap] Failed to withdraw map: {0}", fastMoveResult);
                }
            }

            // No suitable maps found in any tab
            Log.Error("[OpenMap] No suitable maps found in stash");
            Log.ErrorFormat("[OpenMap] Searched up to {0} tabs, found no maps matching tier {1}-{2}",
                maxTabs, settings.MinMapTier, settings.MaxMapTier);

            // Close stash using escape key
            Log.Debug("[OpenMap] Closing stash UI (no maps found)...");
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(500);

            // Simple retry if still open
            if (LokiPoe.InGameState.StashUi.IsOpened)
            {
                Log.Debug("[OpenMap] Stash still open, retrying close");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(500);
            }

            return PhaseResult.Failed("Out of maps - restock stash and restart bot");
        }

        #endregion

        /// <summary>
        /// Internal workflow steps for map opening
        /// </summary>
        private enum MapOpeningStep
        {
            ValidateRequirements,   // Check for map in inventory
            WithdrawMap,            // Withdraw map from stash if not available
            MoveToDevice,           // Move to map device
            InsertMap,              // Insert map into device
            InsertScarabs,          // Insert required scarabs
            ActivateDevice,         // Activate the map device
            WaitForPortal,          // Wait for portal to appear
            EnterPortal             // Enter the portal
        }
    }

}
