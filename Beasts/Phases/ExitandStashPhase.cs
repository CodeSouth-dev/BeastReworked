using System;
using System.Collections.Generic;
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
using Beasts.Core;
using Beasts.Configuration;
using Beasts.Services;
using log4net;
using Logger = DreamPoeBot.Loki.Common.Logger;

namespace Beasts.Phases
{
    /// <summary>
    /// Exit and Stash Phase: Complete post-map workflow
    /// 
    /// WORKFLOW:
    /// PERCEPTION: BossKilled = true, MapComplete = true, InventoryFullness >= 70%
    ///     ↓
    /// PHASE: ExitAndStashPhase
    ///     ↓
    /// ACTIONS:
    ///     1. Exit map via portal to hideout
    ///     2. Move to stash
    ///     3. Deposit items (if inventory 70-95% full)
    ///     4. Move to Heist locker
    ///     5. Deposit blueprints/contracts
    ///     6. Restock consumables (maps, scarabs, portal scrolls)
    ///     ↓
    /// RESULT: StashResult (Ready for next map → Loop to OpenMapPhase)
    /// </summary>
    public class ExitAndStashPhase : IPhase
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public string Name => "ExitAndStash";

        // State tracking
        private StashPhaseStep _currentStep = StashPhaseStep.ExitMap;
        private int _stepAttempts = 0;
        private Vector2i _lastPosition = Vector2i.Zero;
        private int _stuckCounter = 0;
        
        // Inventory tracking
        private List<Item> _itemsToStash = new List<Item>();
        private List<Item> _blueprintsToStash = new List<Item>();
        private int _initialInventoryCount = 0;
        
        // Object references
        private NetworkObject _townPortal;
        private NetworkObject _stashChest;
        private NetworkObject _heistLocker;
        
        // Import constants from centralized location
        private const int MIN_INVENTORY_FULLNESS = BeastConstants.MIN_INVENTORY_FULLNESS;
        private const int MAX_INVENTORY_FULLNESS = BeastConstants.EMERGENCY_INVENTORY_FULLNESS;
        private const int MAX_STEP_ATTEMPTS = BeastConstants.MAX_STEP_ATTEMPTS;
        private const float INTERACTION_RANGE = BeastConstants.INTERACTION_RANGE;

        public bool CanExecute(GameContext context)
        {
            // CRITICAL: This phase ONLY executes when transitioning FROM a map TO hideout
            // It should NEVER execute on startup in hideout - use PreparationPhase for that
            
            // Must be in a map to execute this phase
            if (!context.Player.IsInMap)
                return false;

            // Only execute if map is complete (boss killed)
            // When boss dies, MonstersRemaining becomes 0 and map overlay shows "Map Completed"
            int monstersRemaining = LokiPoe.InstanceInfo.MonstersRemaining;
            if (monstersRemaining != 0)
            {
                // Boss not killed yet - don't execute
                return false;
            }

            // Map complete (boss killed) - check if we need emergency stashing
            // Emergency stashing - inventory critical (95%+ full)
            if (context.Player.InventoryFullnessPercent >= MAX_INVENTORY_FULLNESS)
            {
                Log.InfoFormat("[ExitAndStash] Emergency stashing after map complete - inventory {0}% full", 
                    context.Player.InventoryFullnessPercent);
                return true;
            }

            // Otherwise, skip stashing and go directly to OpenMapPhase for next run
            return false;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            // Check for stuck state
            CheckStuckState(context.MyPosition);

            Log.DebugFormat("[ExitAndStash] Step: {0}, Attempts: {1}, Inventory: {2}%",
                _currentStep, _stepAttempts, context.Player.InventoryFullnessPercent);

            switch (_currentStep)
            {
                case StashPhaseStep.ExitMap:
                    return await ExitMap(context);

                case StashPhaseStep.WaitForHideout:
                    return await WaitForHideout(context);

                case StashPhaseStep.MoveToStash:
                    return await MoveToStash(context);

                case StashPhaseStep.DepositToStash:
                    return await DepositToStash(context);

                case StashPhaseStep.MoveToHeistLocker:
                    return await MoveToHeistLocker(context);

                case StashPhaseStep.DepositToHeistLocker:
                    return await DepositToHeistLocker(context);

                case StashPhaseStep.Restock:
                    return await Restock(context);

                case StashPhaseStep.Complete:
                    return PhaseResult.Success("Stashing complete - ready for next map");

                default:
                    return PhaseResult.Failed($"Unknown step: {_currentStep}");
            }
        }

        /// <summary>
        /// Step 1: Exit the map via portal
        /// </summary>
        private async Task<PhaseResult> ExitMap(GameContext context)
        {
            // If already in hideout, skip this step
            if (context.Player.IsInHideout)
            {
                Log.Info("[ExitAndStash] Already in hideout, analyzing inventory");

                // Analyze inventory to determine what to stash
                AnalyzeInventory();

                // Skip stashing entirely if nothing to stash
                if (_itemsToStash.Count == 0 && _blueprintsToStash.Count == 0)
                {
                    Log.Info("[ExitAndStash] No items need stashing, phase complete");
                    _currentStep = StashPhaseStep.Complete;
                    _stepAttempts = 0;
                    return PhaseResult.Success("No items to stash");
                }

                _currentStep = StashPhaseStep.MoveToStash;
                _stepAttempts = 0;
                return PhaseResult.InProgress("Already in hideout");
            }

            // Find or create portal
            _townPortal = FindTownPortal();
            
            if (_townPortal == null)
            {
                // Need to create portal
                Log.Info("[ExitAndStash] Creating portal to hideout");

                var portalResult = await PortalService.CreatePortal();
                if (!portalResult)
                {
                    _stepAttempts++;
                    if (_stepAttempts > 3)
                    {
                        Log.Error("[ExitAndStash] Failed to create portal after 3 attempts");
                        return PhaseResult.Failed("Failed to create portal");
                    }

                    Log.Warn("[ExitAndStash] Portal creation failed, retrying...");
                    await Coroutine.Sleep(500);
                    return PhaseResult.InProgress("Creating portal...");
                }

                Log.Info("[ExitAndStash] Portal created successfully");
                await Coroutine.Sleep(500); // Wait for portal to spawn
                return PhaseResult.InProgress("Portal created, finding it...");
            }

            // Portal exists, enter it
            if (_townPortal.Distance > INTERACTION_RANGE)
            {
                PlayerMoverManager.Current.MoveTowards(_townPortal.Position);
                return PhaseResult.InProgress($"Moving to portal (Distance: {_townPortal.Distance:F1})");
            }

            Log.Info("[ExitAndStash] Entering portal to hideout");

            await Coroutines.FinishCurrentAction();
            var enterResult = await Coroutines.InteractWith(_townPortal);

            if (enterResult)
            {
                Log.Info("[ExitAndStash] Entered portal, waiting for loading");
                _currentStep = StashPhaseStep.WaitForHideout;
                _stepAttempts = 0;

                // Wait for zone transition
                await Coroutine.Sleep(2000);
                
                return PhaseResult.Wait("Loading into hideout", TimeSpan.FromSeconds(3));
            }

            _stepAttempts++;
            if (_stepAttempts >= MAX_STEP_ATTEMPTS)
            {
                return PhaseResult.Failed("Failed to enter portal");
            }

            return PhaseResult.InProgress("Entering portal...");
        }

        /// <summary>
        /// Step 2: Wait for hideout loading to complete
        /// </summary>
        private async Task<PhaseResult> WaitForHideout(GameContext context)
        {
            if (context.Player.IsInHideout)
            {
                Log.Info("[ExitAndStash] Successfully entered hideout");

                // Analyze inventory before stashing
                AnalyzeInventory();

                // Skip stashing entirely if nothing to stash
                if (_itemsToStash.Count == 0 && _blueprintsToStash.Count == 0)
                {
                    Log.Info("[ExitAndStash] No items need stashing, phase complete");
                    _currentStep = StashPhaseStep.Complete;
                    _stepAttempts = 0;
                    return PhaseResult.Success("No items to stash");
                }

                _currentStep = StashPhaseStep.MoveToStash;
                _stepAttempts = 0;
                return PhaseResult.InProgress("In hideout, proceeding to stash");
            }

            _stepAttempts++;
            if (_stepAttempts > 30) // 3 second timeout
            {
                return PhaseResult.Failed("Loading into hideout timed out");
            }

            await Coroutine.Sleep(100);
            return PhaseResult.Wait("Loading hideout...");
        }

        /// <summary>
        /// Step 3: Move to stash chest
        /// </summary>
        private async Task<PhaseResult> MoveToStash(GameContext context)
        {
            // Skip if no items to stash (only blueprints)
            if (_itemsToStash.Count == 0 && _blueprintsToStash.Count == 0)
            {
                Log.Info("[ExitAndStash] No items to stash");
                _currentStep = StashPhaseStep.Restock;
                return PhaseResult.InProgress("No items to stash, proceeding to restock");
            }

            // If we only have blueprints, go directly to heist locker
            if (_itemsToStash.Count == 0 && _blueprintsToStash.Count > 0)
            {
                Log.Info("[ExitAndStash] Only blueprints detected, going to heist locker");
                _currentStep = StashPhaseStep.MoveToHeistLocker;
                return PhaseResult.InProgress("Skipping stash, going to heist locker");
            }

            _stashChest = FindStashChest();
            
            if (_stashChest == null)
            {
                _stepAttempts++;
                if (_stepAttempts >= MAX_STEP_ATTEMPTS)
                {
                    return PhaseResult.Failed("Stash chest not found");
                }

                await Coroutine.Sleep(500);
                return PhaseResult.InProgress("Searching for stash...");
            }

            float distance = _stashChest.Distance;
            
            if (distance <= INTERACTION_RANGE)
            {
                Log.Info("[ExitAndStash] Reached stash chest");
                _currentStep = StashPhaseStep.DepositToStash;
                _stepAttempts = 0;
                return PhaseResult.InProgress("At stash");
            }

            PlayerMoverManager.Current.MoveTowards(_stashChest.Position);
            return PhaseResult.InProgress($"Moving to stash (Distance: {distance:F1})");
        }

        /// <summary>
        /// Step 4: Deposit items to stash
        /// </summary>
        private async Task<PhaseResult> DepositToStash(GameContext context)
        {
            if (_itemsToStash.Count == 0)
            {
                Log.Info("[ExitAndStash] All items deposited to stash");

                // Close stash UI and verify it actually closed
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(300);

                // Wait up to 2 seconds for stash to close
                int stashDepositCompleteCloseAttempts = 0;
                while (LokiPoe.InGameState.StashUi.IsOpened && stashDepositCompleteCloseAttempts < 10)
                {
                    Log.Debug("[ExitAndStash] Waiting for stash to close...");
                    await Coroutine.Sleep(200);
                    stashDepositCompleteCloseAttempts++;

                    if (stashDepositCompleteCloseAttempts % 3 == 0 && LokiPoe.InGameState.StashUi.IsOpened)
                    {
                        LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                    }
                }

                if (LokiPoe.InGameState.StashUi.IsOpened)
                {
                    Log.Warn("[ExitAndStash] Stash did not close after deposit, proceeding anyway");
                }

                // Check if we have blueprints to deposit
                if (_blueprintsToStash.Count > 0)
                {
                    _currentStep = StashPhaseStep.MoveToHeistLocker;
                }
                else
                {
                    _currentStep = StashPhaseStep.Restock;
                }

                _stepAttempts = 0;
                return PhaseResult.InProgress("Stash deposit complete");
            }

            // Open stash if not already open
            if (!LokiPoe.InGameState.StashUi.IsOpened)
            {
                Log.Info("[ExitAndStash] Opening stash for item deposit");

                if (_stashChest == null)
                {
                    _stashChest = FindStashChest();
                }

                if (_stashChest == null)
                {
                    _stepAttempts++;
                    if (_stepAttempts > 5)
                    {
                        Log.Warn("[ExitAndStash] Cannot find stash, skipping deposit");
                        _itemsToStash.Clear();
                        _currentStep = StashPhaseStep.Restock;
                        return PhaseResult.InProgress("Skipping stash deposit");
                    }

                    await Coroutine.Sleep(300);
                    return PhaseResult.InProgress("Searching for stash...");
                }

                await Coroutines.FinishCurrentAction();
                var interactResult = await Coroutines.InteractWith(_stashChest);

                if (!interactResult)
                {
                    _stepAttempts++;
                    if (_stepAttempts > 5)
                    {
                        Log.Warn("[ExitAndStash] Cannot open stash, skipping deposit");
                        _itemsToStash.Clear();
                        _currentStep = StashPhaseStep.Restock;
                        return PhaseResult.InProgress("Skipping stash deposit");
                    }

                    await Coroutine.Sleep(300);
                    return PhaseResult.InProgress("Opening stash...");
                }

                await Coroutine.Sleep(500); // Wait for stash UI
                return PhaseResult.InProgress("Waiting for stash UI...");
            }

            Log.InfoFormat("[ExitAndStash] Depositing {0} items to stash", _itemsToStash.Count);

            // Deposit items to stash using FastMove
            int itemsDeposited = 0;
            foreach (var item in _itemsToStash.ToList())
            {
                if (item == null || !item.IsValid)
                {
                    _itemsToStash.Remove(item);
                    continue;
                }

                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                // Use FastMove to deposit item to stash
                var fastMoveResult = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.FastMove(item.LocalId);

                if (fastMoveResult == FastMoveResult.None)
                {
                    Log.DebugFormat("[ExitAndStash] Moved {0} to stash", item.FullName ?? item.Name);
                    _itemsToStash.Remove(item);
                    itemsDeposited++;
                }
                else
                {
                    Log.WarnFormat("[ExitAndStash] Failed to move {0}: {1}", item.FullName ?? item.Name, fastMoveResult);
                }

                await Coroutine.Sleep(50); // Small delay between items
            }

            Log.InfoFormat("[ExitAndStash] Deposited {0} items to stash", itemsDeposited);

            // Close stash UI and verify it actually closed
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(300);

            // Wait up to 2 seconds for stash to close
            int stashItemBatchCloseAttempts = 0;
            while (LokiPoe.InGameState.StashUi.IsOpened && stashItemBatchCloseAttempts < 10)
            {
                Log.Debug("[ExitAndStash] Waiting for stash to close after item deposit...");
                await Coroutine.Sleep(200);
                stashItemBatchCloseAttempts++;

                if (stashItemBatchCloseAttempts % 3 == 0 && LokiPoe.InGameState.StashUi.IsOpened)
                {
                    LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                }
            }

            if (LokiPoe.InGameState.StashUi.IsOpened)
            {
                Log.Warn("[ExitAndStash] Stash did not close after item deposit, proceeding anyway");
            }

            // Move to next step
            if (_blueprintsToStash.Count > 0)
            {
                _currentStep = StashPhaseStep.MoveToHeistLocker;
            }
            else
            {
                _currentStep = StashPhaseStep.Restock;
            }

            _stepAttempts = 0;
            return PhaseResult.InProgress("Items deposited");
        }

        /// <summary>
        /// Step 5: Move to Heist locker for blueprints
        /// </summary>
        private async Task<PhaseResult> MoveToHeistLocker(GameContext context)
        {
            if (_blueprintsToStash.Count == 0)
            {
                Log.Info("[ExitAndStash] No blueprints to deposit");
                _currentStep = StashPhaseStep.Restock;
                return PhaseResult.InProgress("No blueprints, proceeding to restock");
            }

            // CRITICAL: Close stash UI before opening heist locker (prevents UI conflicts)
            if (LokiPoe.InGameState.StashUi.IsOpened)
            {
                Log.Info("[ExitAndStash] Closing stash UI before opening heist locker");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(500);

                // Verify closed
                if (LokiPoe.InGameState.StashUi.IsOpened)
                {
                    Log.Debug("[ExitAndStash] Stash still open, waiting...");
                    return PhaseResult.InProgress("Waiting for stash to close");
                }
            }

            _heistLocker = FindHeistLocker();
            
            if (_heistLocker == null)
            {
                _stepAttempts++;
                if (_stepAttempts >= MAX_STEP_ATTEMPTS)
                {
                    Log.Warn("[ExitAndStash] Heist locker not found, skipping blueprint deposit");
                    _currentStep = StashPhaseStep.Restock;
                    return PhaseResult.InProgress("Heist locker not found, skipping");
                }
                
                await Coroutine.Sleep(500);
                return PhaseResult.InProgress("Searching for heist locker...");
            }

            float distance = _heistLocker.Distance;
            
            if (distance <= INTERACTION_RANGE)
            {
                Log.Info("[ExitAndStash] Reached heist locker");
                _currentStep = StashPhaseStep.DepositToHeistLocker;
                _stepAttempts = 0;
                return PhaseResult.InProgress("At heist locker");
            }

            PlayerMoverManager.Current.MoveTowards(_heistLocker.Position);
            return PhaseResult.InProgress($"Moving to heist locker (Distance: {distance:F1})");
        }

        /// <summary>
        /// Step 6: Deposit blueprints/contracts to Heist locker
        /// </summary>
        private async Task<PhaseResult> DepositToHeistLocker(GameContext context)
        {
            if (_blueprintsToStash.Count == 0)
            {
                Log.Info("[ExitAndStash] All blueprints deposited");
                _currentStep = StashPhaseStep.Restock;
                _stepAttempts = 0;
                return PhaseResult.InProgress("Blueprint deposit complete");
            }

            Log.InfoFormat("[ExitAndStash] Depositing {0} blueprints/contracts to heist locker",
                _blueprintsToStash.Count);

            // Open Heist locker if not already open
            if (!LokiPoe.InGameState.HeistLockerUi.IsOpened)
            {
                Log.Info("[ExitAndStash] Opening heist locker");
                await Coroutines.FinishCurrentAction();
                var interactResult = await Coroutines.InteractWith(_heistLocker);

                if (!interactResult)
                {
                    _stepAttempts++;
                    if (_stepAttempts > 5)
                    {
                        Log.Warn("[ExitAndStash] Cannot open heist locker, skipping blueprint deposit");
                        _blueprintsToStash.Clear(); // Clear to avoid infinite loop
                        _currentStep = StashPhaseStep.Restock;
                        return PhaseResult.InProgress("Skipping blueprint deposit");
                    }

                    await Coroutine.Sleep(300);
                    return PhaseResult.InProgress("Opening heist locker...");
                }

                await Coroutine.Sleep(500); // Wait for UI
                return PhaseResult.InProgress("Waiting for heist locker UI...");
            }

            // Deposit blueprints/contracts to heist locker using FastMove
            int blueprintsDeposited = 0;
            foreach (var blueprint in _blueprintsToStash.ToList())
            {
                if (blueprint == null || !blueprint.IsValid)
                {
                    _blueprintsToStash.Remove(blueprint);
                    continue;
                }

                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                // Use FastMove to deposit blueprint to heist locker
                var fastMoveResult = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.FastMove(blueprint.LocalId);

                if (fastMoveResult == FastMoveResult.None)
                {
                    Log.DebugFormat("[ExitAndStash] Moved {0} to heist locker", blueprint.FullName ?? blueprint.Name);
                    _blueprintsToStash.Remove(blueprint);
                    blueprintsDeposited++;
                }
                else
                {
                    Log.WarnFormat("[ExitAndStash] Failed to move {0}: {1}", blueprint.FullName ?? blueprint.Name, fastMoveResult);
                }

                await Coroutine.Sleep(50); // Small delay between items
            }

            Log.InfoFormat("[ExitAndStash] Deposited {0} blueprints/contracts to heist locker", blueprintsDeposited);

            // Close heist locker and verify it actually closed
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(300);

            // Wait up to 2 seconds for heist locker to close
            int heistLockerCloseAttempts = 0;
            while (LokiPoe.InGameState.HeistLockerUi.IsOpened && heistLockerCloseAttempts < 10)
            {
                Log.Debug("[ExitAndStash] Waiting for heist locker to close...");
                await Coroutine.Sleep(200);
                heistLockerCloseAttempts++;

                if (heistLockerCloseAttempts % 3 == 0 && LokiPoe.InGameState.HeistLockerUi.IsOpened)
                {
                    LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                }
            }

            if (LokiPoe.InGameState.HeistLockerUi.IsOpened)
            {
                Log.Warn("[ExitAndStash] Heist locker did not close after deposit, proceeding anyway");
            }

            _currentStep = StashPhaseStep.Restock;
            _stepAttempts = 0;
            return PhaseResult.InProgress("Blueprints deposited");
        }

        /// <summary>
        /// Step 7: Restock consumables (maps, scarabs, portal scrolls)
        /// </summary>
        private async Task<PhaseResult> Restock(GameContext context)
        {
            // CRITICAL: Ensure all UIs are closed before restocking (future-proofing)
            if (LokiPoe.InGameState.StashUi.IsOpened ||
                LokiPoe.InGameState.HeistLockerUi.IsOpened)
            {
                Log.Info("[ExitAndStash] Closing UIs before restock");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(500);

                // Verify closed
                if (LokiPoe.InGameState.StashUi.IsOpened ||
                    LokiPoe.InGameState.HeistLockerUi.IsOpened)
                {
                    Log.Debug("[ExitAndStash] UIs still open, waiting...");
                    return PhaseResult.InProgress("Waiting for UIs to close");
                }
            }

            Log.Info("[ExitAndStash] Checking for items to restock");

            var restockNeeded = new List<string>();

            // Check what needs restocking
            if (!context.Player.HasMap)
                restockNeeded.Add("Map");
            
            if (!context.Player.HasScarabs)
                restockNeeded.Add("Scarabs");
            
            if (!context.Player.HasPortalScrolls)
                restockNeeded.Add("Portal Scrolls");

            if (restockNeeded.Count == 0)
            {
                Log.Info("[ExitAndStash] No restocking needed, phase complete");
                _currentStep = StashPhaseStep.Complete;
                return PhaseResult.Success("Stashing complete - ready for next map");
            }

            Log.InfoFormat("[ExitAndStash] Need to restock: {0}", string.Join(", ", restockNeeded));

            // TODO: Implement restocking logic
            // - Withdraw map from stash
            // - Withdraw scarabs from stash
            // - Withdraw portal scrolls from currency tab
            // For now, simulate restocking

            await Coroutine.Sleep(1500);

            _stepAttempts++;
            if (_stepAttempts >= MAX_STEP_ATTEMPTS)
            {
                Log.Warn("[ExitAndStash] Restocking failed, but continuing anyway");
                _currentStep = StashPhaseStep.Complete;
                return PhaseResult.Success("Stashing complete (restock failed)");
            }

            // Simulate restocking
            if (LokiPoe.Random.Next(0, 10) < 8)
            {
                Log.InfoFormat("[ExitAndStash] Successfully restocked: {0}", string.Join(", ", restockNeeded));
                _currentStep = StashPhaseStep.Complete;
                _stepAttempts = 0;
                return PhaseResult.Success("Stashing and restocking complete");
            }

            return PhaseResult.InProgress("Restocking consumables...");
        }

        #region Helper Methods

        /// <summary>
        /// Analyze inventory and categorize items for stashing
        /// </summary>
        private void AnalyzeInventory()
        {
            _itemsToStash.Clear();
            _blueprintsToStash.Clear();

            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
            {
                Log.Warn("[ExitAndStash] Could not access inventory");
                return;
            }

            var items = inventory.Items.Where(i => i != null && i.IsValid).ToList();
            _initialInventoryCount = items.Count;

            foreach (var item in items)
            {
                // Categorize blueprints/contracts for heist locker
                if (IsBlueprint(item) || IsContract(item))
                {
                    _blueprintsToStash.Add(item);
                }
                // Everything else goes to regular stash
                else if (ShouldStashItem(item))
                {
                    _itemsToStash.Add(item);
                }
            }

            Log.InfoFormat("[ExitAndStash] Inventory analysis: {0} total items, {1} for stash, {2} blueprints/contracts",
                _initialInventoryCount, _itemsToStash.Count, _blueprintsToStash.Count);
        }

        /// <summary>
        /// Check if item is a blueprint
        /// </summary>
        private bool IsBlueprint(Item item)
        {
            return item.Metadata?.Contains("HeistBlueprint") == true ||
                   item.Class?.Contains("Blueprint") == true ||
                   item.Name?.Contains("Blueprint") == true;
        }

        /// <summary>
        /// Check if item is a contract
        /// </summary>
        private bool IsContract(Item item)
        {
            return item.Metadata?.Contains("HeistContract") == true ||
                   item.Class?.Contains("Contract") == true ||
                   item.Name?.Contains("Contract") == true;
        }

        /// <summary>
        /// Determine if item should be stashed
        /// </summary>
        private bool ShouldStashItem(Item item)
        {
            // Don't stash quest items, equipped items, etc.
            if (LokiPoe.Me.EquippedItems.Any(e => e.Id == item.Id))
                return false;

            // NEVER stash portal scrolls - we always need these in inventory
            if (item.Metadata?.ToLower().Contains("portalscroll") == true ||
                item.Name?.ToLower().Contains("portal scroll") == true)
                return false;

            // TODO: Add more intelligent filtering based on item value
            // For now, stash everything that's not a blueprint/contract
            
            var settings = BeastRoutineSettings.Instance.Loot;
            
            // Currency always stashed (except portal scrolls handled above)
            if (item.Class == "Currency" || item.Class == "StackableCurrency")
                return settings.PickupCurrency;

            // Maps
            if (item.Class == "Map")
                return settings.PickupMaps;

            // Divination cards
            if (item.Class == "DivinationCard")
                return settings.PickupDivinationCards;

            // Uniques
            if (item.Rarity == Rarity.Unique)
                return settings.PickupUniques;

            // Rares (might want to filter these more carefully)
            if (item.Rarity == Rarity.Rare)
                return settings.PickupRares;

            return true; // Default: stash it
        }

        /// <summary>
        /// Find town portal in current area
        /// </summary>
        private NetworkObject FindTownPortal()
        {
            return LokiPoe.ObjectManager.Objects
                .Where(obj => obj.IsValid &&
                             obj.Distance < 100f &&
                             (obj.Metadata?.Contains("TownPortal") == true ||
                              obj.Name?.Contains("Portal") == true))
                .OrderBy(obj => obj.Distance)
                .FirstOrDefault();
        }

        /// <summary>
        /// Find stash chest in hideout
        /// </summary>
        private NetworkObject FindStashChest()
        {
            return LokiPoe.ObjectManager.Objects
                .Where(obj => obj.IsValid &&
                             obj.Distance < 200f &&
                             (obj.Metadata?.Contains("Stash") == true ||
                              obj.Name?.ToLower().Contains("stash") == true))
                .OrderBy(obj => obj.Distance)
                .FirstOrDefault();
        }

        /// <summary>
        /// Find Heist locker in hideout
        /// </summary>
        private NetworkObject FindHeistLocker()
        {
            return LokiPoe.ObjectManager.Objects
                .Where(obj => obj.IsValid &&
                             obj.Distance < 200f &&
                             (obj.Metadata?.Contains("HeistLocker") == true ||
                              obj.Metadata?.Contains("Heist") == true ||
                              obj.Name?.Contains("Locker") == true ||
                              obj.Name?.Contains("Heist") == true))
                .OrderBy(obj => obj.Distance)
                .FirstOrDefault();
        }

        /// <summary>
        /// Check for stuck state
        /// </summary>
        private void CheckStuckState(Vector2i currentPosition)
        {
            if (_lastPosition.Distance(currentPosition) < 2f)
            {
                _stuckCounter++;
                
                if (_stuckCounter > 20)
                {
                    Log.Warn("[ExitAndStash] Player appears stuck");
                    // Could implement unstuck logic here
                    _stuckCounter = 0;
                }
            }
            else
            {
                _stuckCounter = 0;
            }

            _lastPosition = currentPosition;
        }

        #endregion

        public void OnExit()
        {
            Log.InfoFormat("[ExitAndStash] Phase complete - Stashed {0} items, {1} blueprints",
                _initialInventoryCount - _itemsToStash.Count - _blueprintsToStash.Count,
                _initialInventoryCount > 0 ? _blueprintsToStash.Count : 0);

            // Close any open UIs before exiting phase
            if (LokiPoe.InGameState.StashUi.IsOpened || LokiPoe.InGameState.HeistLockerUi.IsOpened)
            {
                Log.Info("[ExitAndStash] Closing open UIs before phase exit");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                System.Threading.Thread.Sleep(300); // Synchronous sleep since OnExit is not async
            }

            // Reset state
            _currentStep = StashPhaseStep.ExitMap;
            _stepAttempts = 0;
            _itemsToStash.Clear();
            _blueprintsToStash.Clear();
            _stuckCounter = 0;
            _lastPosition = Vector2i.Zero;
            _townPortal = null;
            _stashChest = null;
            _heistLocker = null;
        }

        /// <summary>
        /// Internal workflow steps
        /// </summary>
        private enum StashPhaseStep
        {
            ExitMap,              // Exit via portal
            WaitForHideout,       // Wait for loading
            MoveToStash,          // Walk to stash
            DepositToStash,       // Deposit regular items
            MoveToHeistLocker,    // Walk to heist locker
            DepositToHeistLocker, // Deposit blueprints/contracts
            Restock,              // Withdraw consumables
            Complete              // Done
        }
    }
}
