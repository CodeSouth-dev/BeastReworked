using System;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using Beasts.Core;
using Beasts.Configuration;
using Beasts.Services;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Exploration phase: Roams the zone looking for beasts and caches
    /// </summary>
    public class ExplorationPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "Exploration";

        private Vector2i _explorationTarget = Vector2i.Zero;
        private int _stuckCounter = 0;
        private int _pathfindingFailures = 0;
        private Vector2i _lastPosition = Vector2i.Zero;
        private DateTime _lastQuicksilverUse = DateTime.MinValue;
        private bool _combatDisabledLogged = false; // Track if we've already logged combat disable
        private const int QUICKSILVER_COOLDOWN_MS = 5000; // Don't spam quicksilver, wait 5 seconds between uses
        private const int EXPLORATION_TARGET_DISTANCE = 20; // Distance to target before picking new point

        public bool CanExecute(GameContext context)
        {
            // Don't explore in hideout or town - wait for user to enter a map
            var currentZone = LokiPoe.CurrentWorldArea?.Name;
            if (currentZone == null || IsHideoutOrTown(currentZone))
            {
                return false;
            }

            // Can explore if in a map
            return true;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            await Task.CompletedTask; // Method needs to be async for phase interface
            var myPos = context.MyPosition;

            // Disable combat during exploration - zoom mode! (log only once per phase)
            if (!_combatDisabledLogged)
            {
                CombatStateService.DisableCombat(this);
                _combatDisabledLogged = true;
            }

            // Check if stuck
            if (_lastPosition != Vector2i.Zero && myPos.Distance(_lastPosition) < 5)
            {
                _stuckCounter++;
                if (_stuckCounter > 5) // Reduced from 10 to 5 for faster recovery
                {
                    Log.Debug("[Exploration] Stuck detected (not moving), picking new point");
                    _explorationTarget = Vector2i.Zero;
                    _stuckCounter = 0;
                    _pathfindingFailures = 0; // Reset pathfinding failures too
                }
            }
            else
            {
                _stuckCounter = 0;
            }
            _lastPosition = myPos;

            // Pick new exploration target if needed (increased distance for faster zooming)
            if (_explorationTarget == Vector2i.Zero || myPos.Distance(_explorationTarget) < EXPLORATION_TARGET_DISTANCE)
            {
                _explorationTarget = PickExplorationPoint(myPos);
                Log.DebugFormat("[Exploration] New target: {0} (distance: {1})", _explorationTarget, myPos.Distance(_explorationTarget));
            }

            // Smart Quicksilver usage during exploration
            // Only use when:
            // - We're actually exploring (not near beasts/caches)
            // - Moving a decent distance
            // - Not in combat
            // - Quicksilver available and not on cooldown
            UseQuicksilverIfAppropriate(context, myPos);

            // Move toward exploration point
            var moveSuccess = PlayerMoverManager.Current.MoveTowards(_explorationTarget);
            if (!moveSuccess)
            {
                _pathfindingFailures++;
                
                // Immediately pick a new target on pathfinding failure
                Log.Debug($"[Exploration] Pathfinding failed (attempt {_pathfindingFailures}), picking new target");
                _explorationTarget = Vector2i.Zero;
                _stuckCounter = 0; // Reset stuck counter since we're changing targets
            }
            else
            {
                // Reset failure counter on successful movement
                _pathfindingFailures = 0;
            }

            return PhaseResult.InProgress($"Exploring toward {_explorationTarget}");
        }

        public void OnExit()
        {
            // Reset exploration state when leaving phase
            _explorationTarget = Vector2i.Zero;
            _stuckCounter = 0;
            _pathfindingFailures = 0;
            _combatDisabledLogged = false; // Reset for next exploration
        }

        /// <summary>
        /// Smart Quicksilver usage - only when actually exploring and moving far
        /// Avoids wasting charges when near mechanics or in combat
        /// </summary>
        private void UseQuicksilverIfAppropriate(GameContext context, Vector2i myPos)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Check cooldown to avoid spam
                if ((now - _lastQuicksilverUse).TotalMilliseconds < QUICKSILVER_COOLDOWN_MS)
                    return;

                // Don't use if we're close to a mechanic (beast or cache)
                // We want to save flask charges for when we need them
                var distanceToTarget = myPos.Distance(_explorationTarget);
                if (distanceToTarget < 30)
                {
                    // Too close to destination, don't waste quicksilver
                    return;
                }

                // Don't use if near beasts or caches - we're about to engage
                if (context.Mechanics.BeastNearby || context.Mechanics.CacheNearby)
                {
                    Log.Debug("[Exploration] Not using Quicksilver - mechanic nearby");
                    return;
                }

                // Don't use if in combat
                if (context.Combat.AnyEnemiesNearby)
                {
                    Log.Debug("[Exploration] Not using Quicksilver - in combat");
                    return;
                }

                // Note: Flask management (including Quicksilver) is handled by BeastCombatRoutine
                // Removed manual flask usage to avoid conflicts
                _lastQuicksilverUse = now;
                Log.Debug("[Exploration] Flask management delegated to BeastCombatRoutine");
            }
            catch (Exception ex)
            {
                Log.Error("[Exploration] Error using Quicksilver", ex);
            }
        }

        /// <summary>
        /// Picks a new exploration point
        /// Simple random approach - ExilePather handles pathfinding validation
        /// </summary>
        private Vector2i PickExplorationPoint(Vector2i currentPos)
        {
            var range = BeastRoutineSettings.Instance.ExplorationRange;
            
            // Pick a random point within range
            // If it's unreachable, movement will fail and we'll pick another
            var randomOffset = new Vector2i(
                (int)LokiPoe.Random.Next(-range, range),
                (int)LokiPoe.Random.Next(-range, range)
            );
            
            return currentPos + randomOffset;
        }

        /// <summary>
        /// Checks if the current zone is a hideout or town
        /// </summary>
        private bool IsHideoutOrTown(string zoneName)
        {
            if (string.IsNullOrEmpty(zoneName))
                return false;

            var lowerZone = zoneName.ToLower();

            // Common hideout names
            if (lowerZone.Contains("hideout"))
                return true;

            // Towns
            var towns = new[] {
                "lioneye's watch",
                "forest encampment",
                "sarn encampment",
                "highgate",
                "overseer's tower",
                "oriath",
                "karui shores",
                "bridge encampment",
                "ravaged square",
                "vastiri desert",
                "menagerie"
            };

            foreach (var town in towns)
            {
                if (lowerZone.Contains(town))
                    return true;
            }

            return false;
        }
    }
}
