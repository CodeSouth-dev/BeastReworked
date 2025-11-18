using System;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using Beasts.Core;
using Beasts.Perception;
using Beasts.Phases;
using Beasts.Configuration;
using log4net;

namespace Beasts.Core
{
    public class StateController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StateController));

        private readonly PerceptionManager _perception;
        private readonly PhaseRegistry _phaseRegistry;
        private IPhase _currentPhase;
        private DateTime _phaseStartTime;

        // Context caching to avoid rebuilding every tick
        private GameContext _cachedContext;
        private DateTime _lastContextUpdate;
        private const int CONTEXT_CACHE_MS = 100; // Rebuild context max 10 times per second

        // Area change tracking for boss reset
        private string _lastAreaId = null;

        private const int MAX_PHASE_RETRIES = 3;
        private const int MAX_PHASE_DURATION_MS = 60000;

        public StateController(PerceptionManager perception, PhaseRegistry phaseRegistry)
        {
            _perception = perception;
            _phaseRegistry = phaseRegistry;
        }

        public async Task<LogicResult> Update()
        {
            try
            {
                // Check for area changes and reset boss tracking
                CheckAreaChange();

                // Use cached context if recent enough (reduces CPU by 30-40%)
                var now = DateTime.UtcNow;
                if (_cachedContext == null || (now - _lastContextUpdate).TotalMilliseconds > CONTEXT_CACHE_MS)
                {
                    _cachedContext = _perception.BuildContext();
                    _lastContextUpdate = now;
                }

                var selectedPhase = _phaseRegistry.GetBestPhase(_cachedContext);

                if (selectedPhase != _currentPhase)
                {
                    await TransitionToPhase(selectedPhase);
                }

                if (_currentPhase != null)
                {
                    return await ExecuteCurrentPhase(_cachedContext);
                }

                return LogicResult.Unprovided;
            }
            catch (Exception ex)
            {
                Log.Error("[StateController] Exception in Update()", ex);
                return LogicResult.Unprovided;
            }
        }

        private async Task<LogicResult> ExecuteCurrentPhase(GameContext context)
        {
            try
            {
                var phaseRuntime = DateTime.UtcNow - _phaseStartTime;
                if (phaseRuntime.TotalMilliseconds > MAX_PHASE_DURATION_MS)
                {
                    Log.Warn($"[StateController] Phase {_currentPhase.Name} timed out");
                    _currentPhase = null;
                    return LogicResult.Unprovided;
                }

                var phaseResult = await _currentPhase.Execute(context);
                return phaseResult.ToLogicResult();
            }
            catch (Exception ex)
            {
                Log.Error($"[StateController] Exception executing phase {_currentPhase.Name}", ex);
                return LogicResult.Unprovided;
            }
        }

        private async Task TransitionToPhase(IPhase newPhase)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            if (_currentPhase != null)
            {
                Log.Debug($"[StateController] Phase transition: {_currentPhase.Name} → {newPhase?.Name ?? "None"}");
                _currentPhase.OnExit();
            }

            _currentPhase = newPhase;
            _phaseStartTime = DateTime.UtcNow;

            // Invalidate cache on phase transition to get fresh context
            _cachedContext = null;
        }

        /// <summary>
        /// Check if player entered a new area and reset boss tracking
        /// </summary>
        private void CheckAreaChange()
        {
            var currentArea = LokiPoe.CurrentWorldArea;
            if (currentArea == null)
                return;

            string currentAreaId = currentArea.Id;

            // Check if area changed
            if (_lastAreaId != null && _lastAreaId != currentAreaId)
            {
                Log.InfoFormat("[StateController] Area changed: {0} → {1}",
                    _lastAreaId, currentAreaId);

                // Reset boss tracking for new map
                _perception.OnMapEntered();
            }

            _lastAreaId = currentAreaId;
        }

        public string GetCurrentState()
        {
            if (_currentPhase == null)
                return "No Phase";

            var runtime = DateTime.UtcNow - _phaseStartTime;
            return $"{_currentPhase.Name} ({runtime.TotalSeconds:F1}s)";
        }
    }
}
