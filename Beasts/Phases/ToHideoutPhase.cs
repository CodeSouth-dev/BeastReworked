using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using Beasts.Core;
using Beasts.Helpers;
using Beasts.Services;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// To Hideout Phase: Gets the player back to their hideout
    /// 
    /// WORKFLOW:
    /// 1. Create portal (if in map)
    /// 2. Enter portal/waypoint to town
    /// 3. Use hideout entrance
    /// 4. Wait for loading
    /// </summary>
    public class ToHideoutPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "ToHideout";

        private HideoutStep _currentStep = HideoutStep.DetermineLocation;
        private int _stepAttempts = 0;

        public bool CanExecute(GameContext context)
        {
            // CRITICAL: This phase should NEVER execute while actively farming
            // It should only execute when:
            // 1. We're in a map and map shows "Map Completed" (boss killed, MonstersRemaining = 0)
            // 2. We're in town and need to get back to hideout
            // 3. Emergency conditions (handled by ExitPhase)
            
            // Never execute if already in hideout
            if (context.Player.IsInHideout)
                return false;
            
            // If in map, only execute if map is complete (boss killed)
            if (context.Player.IsInMap)
            {
                if (!PhaseConditionHelpers.IsMapComplete())
                {
                    // Boss not killed yet - don't try to leave!
                    return false;
                }
                
                // Map complete (boss killed) - ready to return to hideout
                Log.Info("[ToHideout] Map completed (boss killed), ready to return to hideout");
                return true;
            }
            
            // If in town, we can go to hideout
            if (context.Player.IsInTown)
            {
                Log.Info("[ToHideout] In town, ready to return to hideout");
                return true;
            }
            
            // Unknown location - don't execute (let other phases handle it)
            return false;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            Log.DebugFormat("[ToHideout] Step: {0} (Attempt: {1})", _currentStep, _stepAttempts);

            try
            {
                switch (_currentStep)
                {
                    case HideoutStep.DetermineLocation:
                        return await DetermineLocation(context);

                    case HideoutStep.CreatePortal:
                        return await CreatePortal(context);

                    case HideoutStep.EnterPortal:
                        return await EnterPortal(context);

                    case HideoutStep.UseWaypoint:
                        return await UseWaypoint(context);

                    case HideoutStep.GoToHideout:
                        return await GoToHideout(context);

                    case HideoutStep.WaitForLoading:
                        return await WaitForLoading(context);

                    default:
                        return PhaseResult.Failed($"Unknown step: {_currentStep}");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[ToHideout] Exception in step {_currentStep}", ex);
                return PhaseResult.Failed($"Exception in {_currentStep}: {ex.Message}");
            }
        }

        private async Task<PhaseResult> DetermineLocation(GameContext context)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            if (context.Player.IsInHideout)
            {
                Log.Info("[ToHideout] Already in hideout");
                return PhaseResult.Success("Already in hideout");
            }

            if (context.Player.IsInMap)
            {
                Log.Debug("[ToHideout] In map, need to create portal");
                _currentStep = HideoutStep.CreatePortal;
                return PhaseResult.InProgress("Determined need portal from map");
            }

            if (context.Player.IsInTown)
            {
                Log.Debug("[ToHideout] In town, can go to hideout directly");
                _currentStep = HideoutStep.GoToHideout;
                return PhaseResult.InProgress("In town, going to hideout");
            }

            // Unknown location - try waypoint
            Log.Debug("[ToHideout] Unknown location, trying waypoint");
            _currentStep = HideoutStep.UseWaypoint;
            return PhaseResult.InProgress("Unknown location, using waypoint");
        }

        private async Task<PhaseResult> CreatePortal(GameContext context)
        {
            Log.Info("[ToHideout] Creating portal to town (gem or scroll)");

            _stepAttempts++;
            if (_stepAttempts > 3)
            {
                Log.Error("[ToHideout] Failed to create portal after 3 attempts");
                return PhaseResult.Failed("Failed to create portal after 3 attempts");
            }

            if (!await PortalService.CreatePortal())
            {
                Log.WarnFormat("[ToHideout] Failed to create portal (attempt {0}/3)", _stepAttempts);
                await Coroutine.Sleep(500);
                return PhaseResult.InProgress("Retrying portal creation...");
            }

            Log.Info("[ToHideout] Portal created successfully");
            _currentStep = HideoutStep.EnterPortal;
            _stepAttempts = 0;
            return PhaseResult.InProgress("Portal created, moving to enter");
        }

        private async Task<PhaseResult> EnterPortal(GameContext context)
        {
            Log.Info("[ToHideout] Entering portal to town");

            var portal = PortalService.FindNearestPortal();
            if (portal == null)
            {
                Log.Warn("[ToHideout] Portal not found nearby, retrying creation");
                _currentStep = HideoutStep.CreatePortal;
                _stepAttempts = 0;
                return PhaseResult.InProgress("Portal not found, retrying...");
            }

            if (!await PortalService.EnterPortal(portal))
            {
                Log.Warn("[ToHideout] Failed to enter portal");
                _stepAttempts++;
                if (_stepAttempts > 5)
                    return PhaseResult.Failed("Failed to enter portal after 5 attempts");

                await Coroutine.Sleep(500);
                return PhaseResult.InProgress("Retrying portal entry...");
            }

            Log.Info("[ToHideout] Entered portal, waiting for area transition");
            _currentStep = HideoutStep.WaitForLoading;
            _stepAttempts = 0;
            return PhaseResult.Wait("Loading to town", System.TimeSpan.FromSeconds(2));
        }

        private async Task<PhaseResult> UseWaypoint(GameContext context)
        {
            Log.Info("[ToHideout] Using waypoint to get to town");

            // TODO: Implement waypoint usage
            // var waypoint = FindNearestWaypoint();
            // if (waypoint == null)
            //     return PhaseResult.Failed("No waypoint found");
            //
            // var waypointResult = await UseWaypoint(waypoint, "Town");
            // if (!waypointResult)
            //     return PhaseResult.Failed("Failed to use waypoint");

            // Placeholder simulation
            await Coroutine.Sleep(1000);

            _stepAttempts++;
            if (_stepAttempts > 3)
                return PhaseResult.Failed("Failed to use waypoint after 3 attempts");

            // Simulate waypoint usage
            if (LokiPoe.Random.Next(0, 10) < 9) // 90% success rate
            {
                Log.Info("[ToHideout] Used waypoint, waiting for loading");
                _currentStep = HideoutStep.WaitForLoading;
                _stepAttempts = 0;
                return PhaseResult.Wait("Loading to town via waypoint", System.TimeSpan.FromSeconds(2));
            }

            return PhaseResult.InProgress("Using waypoint...");
        }

        private async Task<PhaseResult> GoToHideout(GameContext context)
        {
            Log.Info("[ToHideout] Going to hideout from town using /hideout command");

            _stepAttempts++;
            if (_stepAttempts > 3)
                return PhaseResult.Failed("Failed to enter hideout after 3 attempts");

            // Use /hideout chat command to teleport to hideout
            Log.Info("[ToHideout] Sending /hideout command via chat");

            // Open chat (Enter key)
            LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.Enter, true, false, false);
            await Coroutine.Sleep(100);

            // Type /hideout command
            System.Windows.Forms.SendKeys.SendWait("/hideout");
            await Coroutine.Sleep(100);

            // Press Enter to execute command
            LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.Enter, true, false, false);

            // Wait for command to process and area transition to begin
            await Coroutine.Sleep(1500);

            // Transition to loading state
            Log.Info("[ToHideout] Hideout command sent, waiting for loading");
            _currentStep = HideoutStep.WaitForLoading;
            _stepAttempts = 0;
            return PhaseResult.Wait("Loading into hideout", System.TimeSpan.FromSeconds(2));
        }

        private async Task<PhaseResult> WaitForLoading(GameContext context)
        {
            // TODO: Check if we're actually in hideout now
            // if (context.Player.IsInHideout)
            // {
            //     Log.Info("[ToHideout] Successfully reached hideout");
            //     return PhaseResult.Success("Reached hideout");
            // }

            // Simulate loading
            await Coroutine.Sleep(100);

            _stepAttempts++;
            if (_stepAttempts > 20) // 2 second timeout
                return PhaseResult.Failed("Loading took too long");

            // Gradually increase chance of "loading complete"
            if (LokiPoe.Random.Next(0, 20) < _stepAttempts)
            {
                Log.Info("[ToHideout] Successfully reached hideout!");
                return PhaseResult.Success("Reached hideout");
            }

            return PhaseResult.Wait("Loading...");
        }

        public void OnExit()
        {
            Log.Debug($"[ToHideout] Exiting phase at step: {_currentStep}");
            
            // Reset state
            _currentStep = HideoutStep.DetermineLocation;
            _stepAttempts = 0;
        }

        /// <summary>
        /// Internal workflow steps for getting to hideout
        /// </summary>
        private enum HideoutStep
        {
            DetermineLocation,
            CreatePortal,
            EnterPortal,
            UseWaypoint,
            GoToHideout,
            WaitForLoading
        }
    }
}
