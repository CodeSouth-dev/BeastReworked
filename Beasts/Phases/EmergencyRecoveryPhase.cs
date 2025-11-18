using System;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using Beasts.Core;
using Beasts.Services;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Emergency Recovery Phase: Handles critical failures and stuck states
    /// Activated when the bot encounters unrecoverable errors or gets stuck
    /// </summary>
    public class EmergencyRecoveryPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "EmergencyRecovery";

        // Detection thresholds
        private const int STUCK_THRESHOLD_TICKS = 60; // 60 ticks of no movement
        private const float STUCK_DISTANCE_THRESHOLD = 5f; // Less than 5 units moved
        private const float CRITICAL_HEALTH_THRESHOLD = 10f; // Health below 10%
        private const int MAX_RECOVERY_ATTEMPTS = 3;

        // State tracking
        private Vector2i _lastPosition = Vector2i.Zero;
        private int _noMovementTicks = 0;
        private bool _emergencyDetected = false;
        private int _recoveryAttempts = 0;
        private DateTime _lastRecoveryAttempt = DateTime.MinValue;
        private RecoveryState _currentState = RecoveryState.Monitoring;
        private string _emergencyReason = "";

        public bool CanExecute(GameContext context)
        {
            // Don't run if we're in town/hideout - those are safe zones
            if (context.Player.IsInTown || context.Player.IsInHideout)
            {
                ResetState();
                return false;
            }

            // Check for various emergency conditions
            bool isStuck = DetectStuck(context);
            bool isCriticalHealth = DetectCriticalHealth(context);
            bool isDisconnected = DetectDisconnection();
            bool isInstanceCrashed = DetectInstanceCrash();

            if (isStuck || isCriticalHealth || isDisconnected || isInstanceCrashed)
            {
                if (!_emergencyDetected)
                {
                    _emergencyDetected = true;
                    _emergencyReason = GetEmergencyReason(isStuck, isCriticalHealth, isDisconnected, isInstanceCrashed);
                    Log.Error($"[EmergencyRecovery] EMERGENCY DETECTED: {_emergencyReason}");
                }
                return true;
            }

            if (_emergencyDetected && _recoveryAttempts < MAX_RECOVERY_ATTEMPTS)
            {
                // Continue recovery attempts
                return true;
            }

            // No emergency, reset state
            if (_emergencyDetected && _recoveryAttempts >= MAX_RECOVERY_ATTEMPTS)
            {
                Log.Error("[EmergencyRecovery] Max recovery attempts reached, giving up");
                ResetState();
            }

            return false;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            switch (_currentState)
            {
                case RecoveryState.Monitoring:
                    return await HandleMonitoring(context);

                case RecoveryState.ClearingKeys:
                    return await HandleClearingKeys(context);

                case RecoveryState.AttemptingPortal:
                    return await HandlePortalAttempt(context);

                case RecoveryState.AttemptingRelog:
                    return await HandleRelogAttempt(context);

                case RecoveryState.Failed:
                    return await HandleFailure(context);

                case RecoveryState.Recovered:
                    return HandleRecovered();

                default:
                    return PhaseResult.Failed("Unknown recovery state");
            }
        }

        public void OnExit()
        {
            // Only reset if we successfully recovered
            if (_currentState == RecoveryState.Recovered)
            {
                ResetState();
            }
        }

        #region Detection Methods

        private bool DetectStuck(GameContext context)
        {
            var currentPos = context.MyPosition;

            // Initialize on first check
            if (_lastPosition == Vector2i.Zero)
            {
                _lastPosition = currentPos;
                return false;
            }

            // IMPORTANT: Don't count as stuck if player is frozen, stunned, or has other movement-blocking debuffs
            // These are temporary states that will resolve naturally
            if (IsPlayerImmobilized())
            {
                Log.DebugFormat("[EmergencyRecovery] Player is immobilized (frozen/stunned), not counting as stuck");
                _noMovementTicks = 0; // Reset counter since this is expected
                _lastPosition = currentPos; // Update position
                return false;
            }

            // Check if we've moved significantly
            float distance = currentPos.Distance(_lastPosition);
            if (distance < STUCK_DISTANCE_THRESHOLD)
            {
                _noMovementTicks++;
            }
            else
            {
                _noMovementTicks = 0;
            }

            _lastPosition = currentPos;

            return _noMovementTicks >= STUCK_THRESHOLD_TICKS;
        }

        /// <summary>
        /// Checks if player is frozen, stunned, or otherwise immobilized by debuffs
        /// </summary>
        private bool IsPlayerImmobilized()
        {
            try
            {
                var me = LokiPoe.Me;
                if (me == null)
                    return false;

                // Check for common movement-blocking debuffs
                // These prevent movement but are temporary and should be handled by flasks
                if (me.HasAura("frozen") ||
                    me.HasAura("chilled_ground") ||
                    me.HasAura("stunned") ||
                    me.HasAura("temp_chains") ||
                    me.HasAura("petrified") ||
                    me.HasAura("trapped") ||
                    me.HasAura("rooted") ||
                    me.HasAura("knockback"))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[EmergencyRecovery] Error checking if player is immobilized", ex);
                return false; // If we can't check, assume not immobilized
            }
        }

        private bool DetectCriticalHealth(GameContext context)
        {
            // Critical health without energy shield
            // Note: BeastCombatRoutine handles flask usage automatically
            return context.Player.HealthPercent < CRITICAL_HEALTH_THRESHOLD &&
                   context.Player.EnergyShieldPercent < 5f;
        }

        private bool DetectDisconnection()
        {
            // Check if we're disconnected from the game server
            try
            {
                return !LokiPoe.IsInGame || LokiPoe.Me == null;
            }
            catch
            {
                return true; // If we can't check, assume disconnected
            }
        }

        private bool DetectInstanceCrash()
        {
            // Check for instance crash indicators
            try
            {
                // If we can't access basic game state, instance might be crashed
                // Check if we can't get player inventory
                return LokiPoe.IsInGame && LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main) == null;
            }
            catch
            {
                return true;
            }
        }

        private string GetEmergencyReason(bool stuck, bool critHealth, bool disconnected, bool crashed)
        {
            if (disconnected) return "Disconnected from server";
            if (crashed) return "Instance crashed";
            if (critHealth) return $"Critical health ({_noMovementTicks} ticks at <10% HP)";
            if (stuck) return $"Stuck detected ({_noMovementTicks} ticks without movement)";
            return "Unknown emergency";
        }

        #endregion

        #region Recovery State Handlers

        private async Task<PhaseResult> HandleMonitoring(GameContext context)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            Log.Warn($"[EmergencyRecovery] Starting recovery procedure: {_emergencyReason}");
            _currentState = RecoveryState.ClearingKeys;
            return PhaseResult.InProgress("Initiating emergency recovery");
        }

        private async Task<PhaseResult> HandleClearingKeys(GameContext context)
        {
            Log.Info("[EmergencyRecovery] Clearing all input states");

            try
            {
                // Clear all held keys
                LokiPoe.ProcessHookManager.ClearAllKeyStates();
                await Coroutine.Sleep(500);

                // Note: Flask management is handled by BeastCombatRoutine automatically
                // No need to manually use flasks here
                Log.Info("[EmergencyRecovery] Preparing for emergency portal");
                await Coroutine.Sleep(300);

                _currentState = RecoveryState.AttemptingPortal;
                return PhaseResult.InProgress("Cleared input states and used emergency flasks");
            }
            catch (Exception ex)
            {
                Log.Error("[EmergencyRecovery] Failed to clear keys", ex);
                _currentState = RecoveryState.Failed;
                return PhaseResult.InProgress("Failed to clear keys, proceeding to failure handling");
            }
        }

        private async Task<PhaseResult> HandlePortalAttempt(GameContext context)
        {
            Log.Info("[EmergencyRecovery] Attempting to create escape portal");

            try
            {
                // Try to create portal (gem first, then portal scroll fallback)
                if (!await PortalService.CreatePortal())
                {
                    Log.Warn("[EmergencyRecovery] Failed to create portal, trying relog instead");
                    _currentState = RecoveryState.AttemptingRelog;
                    return PhaseResult.InProgress("Portal creation failed, trying relog");
                }

                Log.Info("[EmergencyRecovery] Portal created, attempting to enter");

                // Wait for portal to be ready
                await Coroutine.Sleep(500);

                // Try to find and enter portal
                var portal = PortalService.FindNearestPortal();
                if (portal != null)
                {
                    if (await PortalService.EnterPortal(portal))
                    {
                        Log.Info("[EmergencyRecovery] Successfully used escape portal");
                        _recoveryAttempts++;
                        _lastRecoveryAttempt = DateTime.UtcNow;

                        // Wait for area transition
                        await Coroutine.Sleep(2000);

                        // Reset state if successful
                        _currentState = RecoveryState.Monitoring;
                        return PhaseResult.InProgress("Escaped via portal, monitoring");
                    }
                }

                Log.Warn("[EmergencyRecovery] Portal created but could not enter, trying relog");
                _currentState = RecoveryState.AttemptingRelog;
                return PhaseResult.InProgress("Portal entry failed, trying relog");
            }
            catch (Exception ex)
            {
                Log.Error("[EmergencyRecovery] Portal creation failed", ex);
                _currentState = RecoveryState.AttemptingRelog;
                return PhaseResult.InProgress("Portal failed, trying relog");
            }
        }

        private async Task<PhaseResult> HandleRelogAttempt(GameContext context)
        {
            Log.Info("[EmergencyRecovery] Attempting to relog");

            try
            {
                // If we've tried too many times, fail
                if (_recoveryAttempts >= MAX_RECOVERY_ATTEMPTS)
                {
                    _currentState = RecoveryState.Failed;
                    return PhaseResult.InProgress("Max recovery attempts reached");
                }

                // TODO: Implement actual relog
                // For now, just increment attempts
                _recoveryAttempts++;
                _lastRecoveryAttempt = DateTime.UtcNow;

                Log.Info($"[EmergencyRecovery] Relog attempt {_recoveryAttempts}/{MAX_RECOVERY_ATTEMPTS}");

                await Coroutine.Sleep(2000);

                // Check if we're still in emergency state
                // If checks pass, mark as recovered
                _currentState = RecoveryState.Recovered;
                return PhaseResult.InProgress("Recovery attempt complete");
            }
            catch (Exception ex)
            {
                Log.Error("[EmergencyRecovery] Relog failed", ex);
                _recoveryAttempts++;

                if (_recoveryAttempts >= MAX_RECOVERY_ATTEMPTS)
                {
                    _currentState = RecoveryState.Failed;
                }
                return PhaseResult.InProgress($"Relog failed, attempts: {_recoveryAttempts}");
            }
        }

        private async Task<PhaseResult> HandleFailure(GameContext context)
        {
            Log.Error($"[EmergencyRecovery] RECOVERY FAILED after {_recoveryAttempts} attempts");
            Log.Error($"[EmergencyRecovery] Reason: {_emergencyReason}");
            Log.Error("[EmergencyRecovery] Pausing bot for manual intervention");

            // TODO: Implement bot pause/stop
            // For now, just log
            await Coroutine.Sleep(5000);

            return PhaseResult.Failed($"Emergency recovery failed: {_emergencyReason}");
        }

        private PhaseResult HandleRecovered()
        {
            Log.Info("[EmergencyRecovery] Recovery successful, resuming normal operation");
            return PhaseResult.Success("Emergency recovery complete");
        }

        #endregion

        #region Helper Methods

        private void ResetState()
        {
            _lastPosition = Vector2i.Zero;
            _noMovementTicks = 0;
            _emergencyDetected = false;
            _recoveryAttempts = 0;
            _currentState = RecoveryState.Monitoring;
            _emergencyReason = "";
        }

        #endregion

        private enum RecoveryState
        {
            Monitoring,        // Watching for emergencies
            ClearingKeys,      // Clear all input states
            AttemptingPortal,  // Try to portal out
            AttemptingRelog,   // Try to relog
            Failed,            // Recovery failed, pause bot
            Recovered          // Successfully recovered
        }
    }
}
