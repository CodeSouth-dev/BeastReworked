using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using SimpleMapBot.Configuration;
using SimpleMapBot.Core;
using log4net;

namespace SimpleMapBot.Tasks
{
    public class EnterMapTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public string Name => "EnterMapTask";
        public string Description => "Enters the map portal";
        public bool IsEnabled => true;

        public async Task<bool> Run()
        {
            var cwa = LokiPoe.CurrentWorldArea;

            // Only run in hideout
            if (!cwa.IsHideoutArea && !cwa.IsTown)
                return false;

            // Find map device portal OR player-created portal (for re-entry)
            var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                .FirstOrDefault(p => p.Distance < 100 && (p.Metadata.Contains("MapDevice") || MapState.PortalCreated));

            if (portal == null)
            {
                Log.Warn("[EnterMapTask] No map portal found");
                return false;
            }

            bool isReEntry = MapState.PortalCreated;
            Log.InfoFormat("[EnterMapTask] Found {0} portal at distance {1:F1}",
                isReEntry ? "return" : "map device", portal.Distance);

            // Move to portal if needed
            if (portal.Distance > 20)
            {
                PlayerMoverManager.Current.MoveTowards(portal.Position);
                await Coroutine.Sleep(100);
                return false;
            }

            // Enter portal
            Log.Info("[EnterMapTask] Entering map portal");
            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            if (!await Coroutines.InteractWith(portal))
            {
                Log.Warn("[EnterMapTask] Failed to interact with portal");
                return false;
            }

            // Wait for zone transition
            for (int i = 0; i < 50; i++)
            {
                await Coroutine.Sleep(100);

                // Check if we've changed zones
                var newArea = LokiPoe.CurrentWorldArea;
                if (newArea != null && !newArea.IsHideoutArea && !newArea.IsTown)
                {
                    Log.InfoFormat("[EnterMapTask] Entered map: {0}", newArea.Name);

                    // Set map state
                    if (!MapState.MapInProgress)
                    {
                        // New map - reset all state
                        MapState.Reset();
                        MapState.MapInProgress = true;
                        Log.Info("[EnterMapTask] Started new map");
                    }
                    else
                    {
                        // Re-entering map after banking
                        MapState.PortalCreated = false; // Used the portal
                        Log.Info("[EnterMapTask] Re-entered map to continue");
                    }

                    return true;
                }
            }

            Log.Warn("[EnterMapTask] Failed to enter map (timeout)");
            return false;
        }
    }
}
