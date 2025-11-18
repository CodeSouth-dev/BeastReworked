using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using Beasts.Configuration;
using Beasts.Perception;
using log4net;

namespace Beasts.Core
{
    /// <summary>
    /// Main bot for autonomous beast and cache farming.
    /// Uses StateController for clean phase management.
    ///
    /// Implements IBot for autonomous operation.
    /// Configure with SkillPlayerMover (PlayerMover) and BeastCombatRoutine (IRoutine) for full functionality.
    /// </summary>
    public class BeastRoutine : IBot
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BeastRoutine));

        // Core components
        private readonly PerceptionManager _perception;
        private readonly PhaseRegistry _phaseRegistry;
        private readonly StateController _stateController;

        // GUI
        private BeastRoutineGui _gui;

        // Bot coroutine (IBot requirement)
        private Coroutine _coroutine;
        private bool _forceExilePatherReload = false;

        public BeastRoutine()
        {
            Log.Info("[BeastRoutine] Constructing routine...");

            // Initialize core components
            _perception = new PerceptionManager();
            _phaseRegistry = new PhaseRegistry();
            _stateController = new StateController(_perception, _phaseRegistry);
        }

        #region IBot Implementation

        public string Name => "BeastRoutine";
        public string Description => "Autonomous beast and cache farming bot with map workflow";
        public string Author => "YourName";
        public string Version => "2.0.0";

        public JsonSettings Settings => BeastRoutineSettings.Instance;
        public UserControl Control => _gui ?? (_gui = new BeastRoutineGui());

        public void Initialize()
        {
            Log.Info("[BeastRoutine] Initializing...");
        }

        public void Deinitialize()
        {
            Log.Info("[BeastRoutine] Deinitializing...");
        }

        public void Start()
        {
            Log.Info("[BeastRoutine] Starting bot...");

            // Set default item evaluator
            ItemEvaluator.Instance = DefaultItemEvaluator.Instance;

            // Cache all bound keys
            LokiPoe.Input.Binding.Update();

            // Set tick rate (30ms between ticks)
            BotManager.MsBetweenTicks = 30;

            Log.Debug($"[Start] MsBetweenTicks: {BotManager.MsBetweenTicks}");
            Log.Debug($"[Start] KeyPickup: {LokiPoe.ConfigManager.KeyPickup}");

            // Enable process hook manager for client actions
            if (BeastRoutineSettings.Instance.ShouldEnableHooks)
            {
                LokiPoe.ProcessHookManager.Enable();
                Log.Info("[Start] ProcessHookManager enabled");
            }

            // IMPORTANT: Close any UIs that might be open from previous sessions
            // This prevents CloseBlockingWindows spam during initialization
            if (LokiPoe.IsInGame)
            {
                bool uisWereOpen = false;
                if (LokiPoe.InGameState.StashUi.IsOpened)
                {
                    Log.Info("[Start] Stash UI is open - closing it");
                    uisWereOpen = true;
                }
                if (LokiPoe.InGameState.MapDeviceUi.IsOpened)
                {
                    Log.Info("[Start] Map Device UI is open - closing it");
                    uisWereOpen = true;
                }
                if (LokiPoe.InGameState.HeistLockerUi.IsOpened)
                {
                    Log.Info("[Start] Heist Locker UI is open - closing it");
                    uisWereOpen = true;
                }

                // If any UIs were open, press Escape to close them
                if (uisWereOpen)
                {
                    Log.Info("[Start] Closing open UIs with Escape key");
                    LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                    System.Threading.Thread.Sleep(100);
                    LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                    System.Threading.Thread.Sleep(300);
                    Log.Info("[Start] UIs closed");
                }
            }

            // Reset coroutine
            _coroutine = null;

            // Reload ExilePather for pathfinding
            ExilePather.Reload();

            // Start all managers
            PluginManager.Start();
            RoutineManager.Start();
            PlayerMoverManager.Start();

            Log.Debug($"[Start] Current PlayerMover: {PlayerMoverManager.Current.Name}");
            Log.Debug($"[Start] Current Routine: {RoutineManager.Current.Name}");

            foreach (var plugin in PluginManager.EnabledPlugins)
            {
                Log.Debug($"[Start] Plugin enabled: {plugin.Name}");
            }

            Log.Info($"[BeastRoutine] Started - Initial state: {_stateController.GetCurrentState()}");
        }

        public void Stop()
        {
            Log.Info("[BeastRoutine] Stopping bot...");

            // Stop all managers
            PluginManager.Stop();
            RoutineManager.Stop();
            PlayerMoverManager.Stop();

            // Disable process hook manager
            if (BeastRoutineSettings.Instance.ShouldEnableHooks)
            {
                LokiPoe.ProcessHookManager.Disable();
                Log.Info("[Stop] ProcessHookManager disabled");
            }

            // Cleanup coroutine
            if (_coroutine != null)
            {
                _coroutine.Dispose();
                _coroutine = null;
            }

            Log.Info("[BeastRoutine] Stopped");
        }

        public void Tick()
        {
            // Initialize coroutine if needed
            if (_coroutine == null)
            {
                _coroutine = new Coroutine(() => MainCoroutine());
            }

            // Handle ExilePather reload based on game state
            if (LokiPoe.IsInGame)
            {
                if (_forceExilePatherReload)
                {
                    ExilePather.Reload(true);
                    _forceExilePatherReload = false;
                }
                else
                {
                    ExilePather.Reload();
                }
            }
            else
            {
                _forceExilePatherReload = true;
            }

            // Propagate tick to managers
            PluginManager.Tick();
            RoutineManager.Tick();
            PlayerMoverManager.Tick();

            // Check if coroutine finished (bot was stopped)
            if (_coroutine.IsFinished)
            {
                Log.Debug($"[Tick] Bot coroutine finished with status: {_coroutine.Status}");
                BotManager.Stop();
                return;
            }

            // Execute coroutine
            try
            {
                _coroutine.Resume();
            }
            catch
            {
                var c = _coroutine;
                _coroutine = null;
                c.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Logic hook - can be used by plugins to interact with the bot
        /// </summary>
        public async Task<LogicResult> Logic(Logic logic)
        {
            await Task.CompletedTask; // Method needs to be async for IBot interface

            // As IBot, we don't need to handle specific logic hooks
            // The MainCoroutine drives everything
            // This is here for plugin compatibility
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            // Handle inter-component communication
            // Can be used for settings queries, state requests, etc.
            
            if (message.Id == "GetCurrentState")
            {
                message.AddOutput(this, _stateController.GetCurrentState());
                return MessageResult.Processed;
            }

            return MessageResult.Unprocessed;
        }

        #endregion

        #region Main Coroutine

        /// <summary>
        /// Main bot coroutine - runs continuously and drives all bot logic
        /// This is the heart of the IBot implementation
        /// </summary>
        private async Task MainCoroutine()
        {
            Log.Info("[MainCoroutine] Starting main bot loop");

            while (true)
            {
                // Handle login screen
                if (LokiPoe.IsInLoginScreen)
                {
                    // Delegate login to plugins
                    var logic = new Logic("hook_login_screen", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                            break;
                    }
                }
                // Handle character selection
                else if (LokiPoe.IsInCharacterSelectionScreen)
                {
                    // Delegate character selection to plugins
                    var logic = new Logic("hook_character_selection", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                            break;
                    }
                }
                // Main game loop - where all the action happens
                else if (LokiPoe.IsInGame && ExilePather.IsReady)
                {
                    // Allow plugins to preempt bot logic if needed
                    var hooked = false;
                    var logic = new Logic("hook_ingame", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                        {
                            hooked = true;
                            break;
                        }
                    }

                    if (!hooked)
                    {
                        // Wait for game pause
                        if (LokiPoe.InstanceInfo.IsGamePaused)
                        {
                            // Just wait
                        }
                        // Handle death
                        else if (LokiPoe.Me.IsDead)
                        {
                            Log.Warn("[MainCoroutine] Player is dead - waiting for resurrection");
                            // TODO: Implement resurrection logic if needed
                            // For now, just wait
                        }
                        // Main bot logic - delegate to StateController
                        else
                        {
                            try
                            {
                                // This is where our state machine runs!
                                await _stateController.Update();

                                // Log state periodically
                                LogStateIfChanged();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("[MainCoroutine] Exception in StateController.Update", ex);
                                // Don't crash the bot, just log and continue
                            }
                        }
                    }
                }
                else
                {
                    // Loading screen or unknown state - just wait
                    await Coroutine.Sleep(1000);
                }

                // Yield to allow other operations
                await Coroutine.Yield();
            }
        }

        #endregion

        #region State Logging

        private string _lastLoggedState = "";
        private DateTime _lastStateLog = DateTime.MinValue;

        private void LogStateIfChanged()
        {
            var currentState = _stateController.GetCurrentState();
            var now = DateTime.UtcNow;

            // Log immediately on state change, or every 30 seconds
            if (currentState != _lastLoggedState || 
                (now - _lastStateLog).TotalSeconds > 30)
            {
                Log.Info($"[BeastRoutine] State: {currentState}");
                _lastLoggedState = currentState;
                _lastStateLog = now;
            }
        }

        #endregion

        public override string ToString()
        {
            return $"{Name}: {Description}";
        }
    }
}
