using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using Beasts.Core;
using Beasts.Services;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Approach Target phase: Navigates to detected beast or cache
    /// </summary>
    public class ApproachTargetPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "ApproachTarget";

        private int _approachFailures = 0;

        public bool CanExecute(GameContext context)
        {
            return context.Mechanics.HasTarget && !context.Mechanics.InRange;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            await Task.CompletedTask; // Method needs to be async for phase interface
            var target = context.Mechanics.CurrentTarget;
            if (target == null)
                return PhaseResult.Failed("No mechanic target");

            // Enable combat when approaching cache (to clear enemies on the way)
            // For beasts, they'll be handled by CaptureBeastPhase
            if (context.Mechanics.CacheNearby)
            {
                CombatStateService.EnableCombat(this, target.Position);
            }

            var targetPos = target.Position;

            Log.DebugFormat("[ApproachTarget] Moving toward {0} at distance {1:F1}",
                target.Name, target.Distance);

            // Move toward target
            var moveSuccess = PlayerMoverManager.Current.MoveTowards(targetPos);

            if (!moveSuccess)
            {
                _approachFailures++;
                Log.WarnFormat("[ApproachTarget] Movement failed ({0} failures)", _approachFailures);

                if (_approachFailures > 5)
                {
                    Log.Error("[ApproachTarget] Too many failures, blacklisting target");
                    Blacklist.Add(target.Id, System.TimeSpan.FromMinutes(5), "Failed to approach");
                    _approachFailures = 0;
                    return PhaseResult.Failed("Too many approach failures");
                }
            }
            else
            {
                _approachFailures = 0;
            }

            return PhaseResult.InProgress($"Moving to {target.Name} ({target.Distance:F1} units)");
        }

        public void OnExit()
        {
            _approachFailures = 0;
            
            // DON'T disable combat here - if we were approaching a cache, combat should
            // stay enabled through OpenCachePhase and LootingPhase
            // LootingPhase will disable combat when all loot is collected
        }
    }
}
