using System;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using Beasts.Configuration;
using Beasts.Core;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Capture Beast phase: Kills beast to trigger automatic capture
    /// Note: No net throwing needed - Einhar captures beasts automatically when killed
    /// </summary>
    public class CaptureBeastPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "CaptureBeast";

        public bool CanExecute(GameContext context)
        {
            // Can execute if there's a beast nearby that we want to capture
            return context.Mechanics.BeastNearby;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            await Task.CompletedTask; // Method needs to be async for phase interface

            var beast = context.Mechanics.BestBeast;
            if (beast == null || !beast.IsValid || !beast.IsAliveHostile)
            {
                return PhaseResult.Success("Beast captured or died");
            }

            // Enable combat at beast position so BeastCombatRoutine will fight
            var msg = new Message("EnableCombat", this, beast.Position);
            RoutineManager.Current.Message(msg);

            Log.InfoFormat("[CaptureBeast] Engaging beast: {0} (HP: {1}%)",
                beast.Name, beast.HealthPercentTotal);

            // Move to the beast - PlayerMoverManager will handle movement
            var moveSuccess = PlayerMoverManager.Current.MoveTowards(beast.Position);
            if (!moveSuccess)
            {
                Log.Warn("[CaptureBeast] Failed to move to beast");
                return PhaseResult.InProgress("Moving to beast...");
            }

            // RoutineManager.Tick() (called by BeastRoutine) handles actual combat
            // We just wait here and let the routine kill the beast

            // If beast is dead or invalid, it was captured
            if (!beast.IsAliveHostile || !beast.IsValid)
            {
                Log.Info("[CaptureBeast] Beast killed - Einhar will capture it");
                Blacklist.Add(beast.Id, TimeSpan.FromMinutes(10), "Beast captured");
                return PhaseResult.Success("Beast captured");
            }

            return PhaseResult.InProgress("Killing beast for capture...");
        }

        public void OnExit()
        {
            // Disable combat when leaving beast capture
            var msg = new Message("DisableCombat", this);
            RoutineManager.Current.Message(msg);
        }
    }
}
