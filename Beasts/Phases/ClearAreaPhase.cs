using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Core;
using Beasts.Configuration;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Clear Area phase: Kills enemies around a mechanic (beast/cache) before interacting
    /// </summary>
    public class ClearAreaPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "ClearArea";

        public bool CanExecute(GameContext context)
        {
            return context.Mechanics.HasTarget && context.Combat.EnemiesNearMechanic;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            var target = context.Combat.BestTarget;
            if (target == null)
                return PhaseResult.InProgress("No enemies to clear");

            // Enable combat around the mechanic position
            var mechanicPos = context.Mechanics.BestBeast?.Position ?? context.Mechanics.BestCache?.Position ?? context.MyPosition;
            var msg = new Message("EnableCombat", this, mechanicPos);
            RoutineManager.Current.Message(msg);

            return await PerformCombat(context, target);
        }

        public void OnExit()
        {
            // Disable combat when area is cleared
            var msg = new Message("DisableCombat", this);
            RoutineManager.Current.Message(msg);
        }

        private async Task<PhaseResult> PerformCombat(GameContext context, Monster target)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            var myPos = context.MyPosition;
            var targetPos = target.Position;
            var dist = myPos.Distance(targetPos);

            var settings = BeastRoutineSettings.Instance.Combat;

            // Check if we need to move closer
            if (dist > settings.MaxMeleeRange)
            {
                Log.DebugFormat("[ClearArea] Moving to target (distance: {0})", dist);
                PlayerMoverManager.Current.MoveTowards(targetPos);
                return PhaseResult.InProgress($"Approaching enemy (Distance: {dist:F1})");
            }

            // Attack the target
            var slot = settings.PrimarySkillSlot;
            var skill = LokiPoe.InGameState.SkillBarHud.Slot(slot);

            if (skill == null)
            {
                Log.Error("[ClearArea] No skill configured in primary slot");
                return PhaseResult.Failed("No combat skill configured");
            }

            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
                slot,
                settings.AlwaysAttackInPlace,
                targetPos);

            if (useResult != LokiPoe.InGameState.UseResult.None)
            {
                Log.WarnFormat("[ClearArea] Skill use failed: {0}", useResult);
            }

            return PhaseResult.InProgress($"Clearing enemies near mechanic");
        }
    }
}
