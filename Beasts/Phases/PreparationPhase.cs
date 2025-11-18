using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Core;
using Beasts.Configuration;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Preparation Phase: STARTUP ONLY - Restocks portal scrolls for initial map opening
    /// 
    /// IMPORTANT: This phase is for STARTUP ONLY and does NOT handle stashing
    /// Stashing is handled by ExitAndStashPhase after returning from maps
    /// 
    /// WORKFLOW:
    /// 1. Check if missing portal scrolls
    /// 2. Navigate to stash
    /// 3. Withdraw portal scrolls from stash
    /// 
    /// RESULT: Ready for OpenMapPhase to begin
    /// 
    /// NOTE: Maps and scarabs are handled by OpenMapPhase which checks device/stash as needed
    /// </summary>
    public class PreparationPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "Preparation";

        // State tracking
        private PrepPhaseStep _currentStep = PrepPhaseStep.AnalyzeInventory;
        private int _stepAttempts = 0;
        private Vector2i _lastPosition = Vector2i.Zero;
        private int _stuckCounter = 0;
        
        // Inventory tracking (deprecated - kept for OnExit logging only)
        private List<Item> _itemsToStash = new List<Item>();
        private List<Item> _blueprintsToStash = new List<Item>();
        private int _initialInventoryCount = 0;
        
        // Object references
        private NetworkObject _stashChest;
        
        // Configuration
        private const int MIN_INVENTORY_FULLNESS = 70; // Stash if >= 70% full
        private const int MAX_STEP_ATTEMPTS = 10;
        private const float INTERACTION_RANGE = 15f;

        public bool CanExecute(GameContext context)
        {
            if (!context.Player.IsInHideout)
                return false;

            // Only execute PreparationPhase when missing portal scrolls for startup
            // This ensures we don't interfere with normal map workflow
            bool needPortalScrolls = !context.Player.HasPortalScrolls;
            
            if (needPortalScrolls)
            {
                Log.Info("[Preparation] Need to restock portal scrolls from stash");
                return true;
            }

            // Have portal scrolls - let OpenMapPhase handle the rest
            // Stashing is handled by ExitAndStashPhase after returning from maps
            return false;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            // Check for stuck state
            CheckStuckState(context.MyPosition);

            Log.DebugFormat("[Preparation] Step: {0}, Attempts: {1}, Inventory: {2}%",
                _currentStep, _stepAttempts, context.Player.InventoryFullnessPercent);

            switch (_currentStep)
            {
                case PrepPhaseStep.AnalyzeInventory:
                    return await AnalyzeInventory(context);

                case PrepPhaseStep.Restock:
                    return await Restock(context);

                case PrepPhaseStep.Complete:
                    return PhaseResult.Success("Preparation complete - ready for map opening");

                default:
                    // Old stashing steps removed - PreparationPhase is STARTUP ONLY
                    // Stashing is handled by ExitAndStashPhase
                    Log.ErrorFormat("[Preparation] Invalid step reached: {0} - this should not happen", _currentStep);
                    _currentStep = PrepPhaseStep.Restock;
                    return PhaseResult.InProgress("Recovered from invalid step");
            }
        }

        /// <summary>
        /// Step 1: Validate startup conditions (PreparationPhase is STARTUP ONLY - no stashing)
        /// </summary>
        private async Task<PhaseResult> AnalyzeInventory(GameContext context)
        {
            await Task.CompletedTask; // Method needs to be async for consistency

            Log.Info("[Preparation] PreparationPhase is for STARTUP ONLY - going directly to restock portal scrolls");

            // PreparationPhase should NEVER stash items
            // Stashing is handled by ExitAndStashPhase after returning from maps
            // This phase ONLY restocks portal scrolls for startup
            
            _itemsToStash.Clear();
            _blueprintsToStash.Clear();
            
            // Go directly to restock - no inventory analysis needed
            _currentStep = PrepPhaseStep.Restock;
            _stepAttempts = 0;
            return PhaseResult.InProgress("Restocking portal scrolls for startup");
        }

        /// <summary>
        /// DEPRECATED: PreparationPhase no longer handles stashing
        /// This method is kept for reference but should never be called
        /// Stashing is now handled by ExitAndStashPhase
        /// </summary>
        [Obsolete("PreparationPhase no longer handles stashing - use ExitAndStashPhase")]
        private async Task<PhaseResult> MoveToStash(GameContext context)
        {
            await Task.CompletedTask;
            Log.Error("[Preparation] DEPRECATED METHOD CALLED: MoveToStash - this should never happen!");
            _currentStep = PrepPhaseStep.Restock;
            return PhaseResult.Failed("Deprecated method called - phase logic error");
        }

        /// <summary>
        /// DEPRECATED: PreparationPhase no longer handles stashing
        /// This method is kept for reference but should never be called
        /// Stashing is now handled by ExitAndStashPhase
        /// </summary>
        [Obsolete("PreparationPhase no longer handles stashing - use ExitAndStashPhase")]
        private async Task<PhaseResult> DepositToStash(GameContext context)
        {
            await Task.CompletedTask;
            Log.Error("[Preparation] DEPRECATED METHOD CALLED: DepositToStash - this should never happen!");
            _currentStep = PrepPhaseStep.Restock;
            return PhaseResult.Failed("Deprecated method called - phase logic error");
        }

        /// <summary>
        /// DEPRECATED: PreparationPhase no longer handles heist locker
        /// This method is kept for reference but should never be called
        /// Stashing is now handled by ExitAndStashPhase
        /// </summary>
        [Obsolete("PreparationPhase no longer handles stashing - use ExitAndStashPhase")]
        private async Task<PhaseResult> MoveToHeistLocker(GameContext context)
        {
            await Task.CompletedTask;
            Log.Error("[Preparation] DEPRECATED METHOD CALLED: MoveToHeistLocker - this should never happen!");
            _currentStep = PrepPhaseStep.Restock;
            return PhaseResult.Failed("Deprecated method called - phase logic error");
        }

        /// <summary>
        /// DEPRECATED: PreparationPhase no longer handles heist locker
        /// This method is kept for reference but should never be called
        /// Stashing is now handled by ExitAndStashPhase
        /// </summary>
        [Obsolete("PreparationPhase no longer handles stashing - use ExitAndStashPhase")]
        private async Task<PhaseResult> DepositToHeistLocker(GameContext context)
        {
            await Task.CompletedTask;
            Log.Error("[Preparation] DEPRECATED METHOD CALLED: DepositToHeistLocker - this should never happen!");
            _currentStep = PrepPhaseStep.Restock;
            return PhaseResult.Failed("Deprecated method called - phase logic error");
        }

        /// <summary>
        /// Step 6: Restock consumables (portal scrolls only - maps/scarabs handled by OpenMapPhase)
        /// </summary>
        private async Task<PhaseResult> Restock(GameContext context)
        {
            // Ensure all UIs are closed before restocking
            if (LokiPoe.InGameState.StashUi.IsOpened ||
                LokiPoe.InGameState.HeistLockerUi.IsOpened)
            {
                Log.Info("[Preparation] Closing UIs before restock");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(500);

                if (LokiPoe.InGameState.StashUi.IsOpened ||
                    LokiPoe.InGameState.HeistLockerUi.IsOpened)
                {
                    Log.Debug("[Preparation] UIs still open, waiting...");
                    return PhaseResult.InProgress("Waiting for UIs to close");
                }
            }

            Log.Info("[Preparation] Checking for portal scrolls to restock");

            // PreparationPhase only handles portal scrolls
            // Maps and scarabs are handled by OpenMapPhase
            if (context.Player.HasPortalScrolls)
            {
                Log.Info("[Preparation] Portal scrolls available, phase complete");
                _currentStep = PrepPhaseStep.Complete;
                return PhaseResult.Success("Preparation complete - ready for map opening");
            }

            Log.Info("[Preparation] Need to restock portal scrolls from stash");

            // Step 1: Find and navigate to stash
            if (_stashChest == null)
            {
                _stashChest = FindStashChest();
            }

            if (_stashChest == null)
            {
                _stepAttempts++;
                if (_stepAttempts >= MAX_STEP_ATTEMPTS)
                {
                    Log.Error("[Preparation] Stash not found in hideout after multiple attempts");
                    return PhaseResult.Failed("Stash not found - cannot restock portal scrolls");
                }

                Log.Warn("[Preparation] Stash not found, searching...");
                await Coroutine.Sleep(500);
                return PhaseResult.InProgress("Searching for stash...");
            }

            // Step 2: Move to stash
            float distance = _stashChest.Distance;
            if (distance > INTERACTION_RANGE)
            {
                Log.DebugFormat("[Preparation] Moving to stash for portal scrolls (Distance: {0:F1})", distance);
                PlayerMoverManager.Current.MoveTowards(_stashChest.Position);
                await Coroutine.Sleep(100);
                return PhaseResult.InProgress($"Moving to stash ({distance:F1}m)");
            }

            // Step 3: Open stash if not already open
            if (!LokiPoe.InGameState.StashUi.IsOpened)
            {
                Log.Info("[Preparation] Opening stash to withdraw portal scrolls");
                await Coroutines.FinishCurrentAction();
                var interactResult = await Coroutines.InteractWith(_stashChest);

                if (!interactResult)
                {
                    _stepAttempts++;
                    if (_stepAttempts > 5)
                    {
                        Log.Error("[Preparation] Failed to open stash after 5 attempts");
                        return PhaseResult.Failed("Cannot open stash");
                    }

                    Log.WarnFormat("[Preparation] Failed to open stash (attempt {0}/5)", _stepAttempts);
                    await Coroutine.Sleep(300);
                    return PhaseResult.InProgress("Opening stash...");
                }

                await Coroutine.Sleep(500); // Wait for stash UI
                return PhaseResult.InProgress("Waiting for stash UI...");
            }

            Log.Info("[Preparation] Stash opened - withdrawing portal scrolls");

            // Step 4: Navigate to currency tab and withdraw portal scrolls
            var tabControl = LokiPoe.InGameState.StashUi.TabControl;
            if (tabControl == null)
            {
                Log.Error("[Preparation] Cannot access stash tab control");
                return PhaseResult.Failed("Stash tab control unavailable");
            }

            // Search for portal scrolls in stash tabs
            // We'll look in the first 10 tabs (currency tab is usually early)
            for (int tabIndex = 0; tabIndex < 10; tabIndex++)
            {
                if (tabControl.CurrentTabIndex != tabIndex)
                {
                    Log.DebugFormat("[Preparation] Switching to tab {0} to search for portal scrolls", tabIndex);
                    tabControl.SwitchToTabMouse(tabIndex);
                    await Coroutine.Sleep(200);
                }

                var stashInventory = LokiPoe.InGameState.StashUi.InventoryControl?.Inventory;
                if (stashInventory == null)
                    continue;

                // Find portal scrolls in this tab
                var portalScroll = stashInventory.Items
                    .FirstOrDefault(item => item != null &&
                                          item.IsValid &&
                                          (item.Metadata?.ToLower().Contains("portalscroll") == true ||
                                           item.Name?.ToLower().Contains("portal scroll") == true));

                if (portalScroll != null)
                {
                    Log.InfoFormat("[Preparation] Found portal scrolls in tab {0}, withdrawing stack", tabIndex);

                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    var fastMoveResult = LokiPoe.InGameState.StashUi.InventoryControl.FastMove(portalScroll.LocalId);

                    if (fastMoveResult == FastMoveResult.None)
                    {
                        Log.Info("[Preparation] Portal scrolls withdrawn successfully");

                        // Close stash
                        LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                        await Coroutine.Sleep(300);

                        _currentStep = PrepPhaseStep.Complete;
                        _stepAttempts = 0;
                        return PhaseResult.Success("Portal scrolls restocked - ready for map opening");
                    }
                    else
                    {
                        Log.WarnFormat("[Preparation] Failed to withdraw portal scrolls: {0}", fastMoveResult);
                    }
                }
            }

            // Portal scrolls not found in first 10 tabs
            Log.Warn("[Preparation] Portal scrolls not found in stash (searched first 10 tabs)");
            
            // Close stash
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(300);

            return PhaseResult.Failed("Out of portal scrolls - please restock stash and restart bot");
        }

        #region Helper Methods

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
                    Log.Warn("[Preparation] Player appears stuck");
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
            Log.InfoFormat("[Preparation] Phase complete - Stashed {0} items, {1} blueprints",
                _initialInventoryCount - _itemsToStash.Count - _blueprintsToStash.Count,
                _initialInventoryCount > 0 ? _blueprintsToStash.Count : 0);

            // Close any open UIs before exiting phase
            if (LokiPoe.InGameState.StashUi.IsOpened || LokiPoe.InGameState.HeistLockerUi.IsOpened)
            {
                Log.Info("[Preparation] Closing open UIs before phase exit");
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                System.Threading.Thread.Sleep(300);
            }

            // Reset state
            _currentStep = PrepPhaseStep.AnalyzeInventory;
            _stepAttempts = 0;
            _itemsToStash.Clear();
            _blueprintsToStash.Clear();
            _stuckCounter = 0;
            _lastPosition = Vector2i.Zero;
            _stashChest = null;
        }

        /// <summary>
        /// Internal workflow steps (STARTUP ONLY - no stashing)
        /// </summary>
        private enum PrepPhaseStep
        {
            AnalyzeInventory,     // Validate startup (always goes to Restock)
            Restock,              // Withdraw portal scrolls from stash
            Complete              // Done - ready for map opening
        }
    }
}

