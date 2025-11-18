using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using Beasts.Core;
using Beasts.Configuration;
using Beasts.Services;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Exit phase: Uses portal scroll to leave the zone and return to town
    /// </summary>
    public class ExitPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "Exit";

        private bool _portalCreated = false;
        private int _portalAttempts = 0;
        private const int MAX_PORTAL_ATTEMPTS = 3;

        public bool CanExecute(GameContext context)
        {
            // Can execute if we need to exit and we're in a map
            var exitConditions = BeastRoutineSettings.Instance.ExitConditions;

            if (!context.Player.IsInMap)
                return false;

            // Check exit conditions
            bool shouldExit = false;

            if (exitConditions.ExitOnInventoryFull && context.Player.InventoryFull)
            {
                Log.Info("[Exit] Inventory full - triggering exit");
                shouldExit = true;
            }

            if (exitConditions.ExitOnLowResources &&
                context.Player.HealthPercent < exitConditions.MinHealthPercent)
            {
                Log.Info("[Exit] Low health - triggering exit");
                shouldExit = true;
            }

            if (exitConditions.ExitOnLowPortalScrolls &&
                context.Player.PortalScrollCount < exitConditions.MinPortalScrolls)
            {
                Log.Info("[Exit] Low portal scrolls - triggering exit");
                shouldExit = true;
            }

            return shouldExit;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            if (!_portalCreated)
            {
                Log.Info("[Exit] Creating portal to exit map...");

                // Use PortalService to create portal (tries gem, falls back to scroll)
                if (!await PortalService.CreatePortal())
                {
                    _portalAttempts++;
                    if (_portalAttempts >= MAX_PORTAL_ATTEMPTS)
                    {
                        Log.Error("[Exit] Failed to create portal after multiple attempts");
                        return PhaseResult.Failed("Portal creation failed");
                    }

                    Log.WarnFormat("[Exit] Failed to create portal (attempt {0}/{1})", _portalAttempts, MAX_PORTAL_ATTEMPTS);
                    await Coroutine.Sleep(500);
                    return PhaseResult.InProgress("Retrying portal creation...");
                }

                _portalCreated = true;
                Log.Info("[Exit] Portal created successfully");
                return PhaseResult.InProgress("Portal created, waiting to enter...");
            }

            // Find the portal object
            var portal = PortalService.FindNearestPortal();

            if (portal == null)
            {
                Log.Warn("[Exit] Portal not found nearby, resetting...");
                _portalCreated = false;
                _portalAttempts = 0;
                return PhaseResult.InProgress("Looking for portal...");
            }

            // Enter portal using PortalService
            if (portal.Distance > 30)
            {
                Log.InfoFormat("[Exit] Moving to portal (distance: {0:F1})", portal.Distance);
                PlayerMoverManager.Current.MoveTowards(portal.Position);
                await Coroutine.Sleep(100);
                return PhaseResult.InProgress("Moving to portal...");
            }

            Log.Info("[Exit] Entering portal to return to town");

            if (!await PortalService.EnterPortal(portal))
            {
                Log.Warn("[Exit] Failed to enter portal");
                return PhaseResult.InProgress("Attempting to enter portal...");
            }

            // Wait for area transition
            await Coroutine.Sleep(2000);

            if (context.Player.IsInTown)
            {
                Log.Info("[Exit] Successfully returned to town");
                return PhaseResult.Success("Exited to town");
            }

            return PhaseResult.InProgress("Waiting for transition to town...");
        }

        public void OnExit()
        {
            _portalCreated = false;
            _portalAttempts = 0;
        }
    }
}
