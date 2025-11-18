using System;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using Beasts.Core;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Open Cache phase: Interacts with smuggler's cache
    /// </summary>
    public class OpenCachePhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "OpenCache";

        private int _interactAttempts = 0;

        public bool CanExecute(GameContext context)
        {
            return context.Mechanics.CacheNearby &&
                   context.Mechanics.InRange &&
                   !context.Combat.EnemiesNearMechanic;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            var cache = context.Mechanics.BestCache;
            if (cache == null || !cache.IsValid)
                return PhaseResult.Failed("Cache target lost");

            // Combat should already be enabled from ApproachTargetPhase
            // Keep it enabled while opening and looting

            Log.InfoFormat("[OpenCache] Opening cache: {0}", cache.Name);

            // Interact with the cache
            await Coroutines.FinishCurrentAction();
            var interactResult = await Coroutines.InteractWith(cache);

            if (!interactResult)
            {
                _interactAttempts++;
                Log.WarnFormat("[OpenCache] Interaction failed (attempt {0})", _interactAttempts);

                if (_interactAttempts > 5)
                {
                    Log.Error("[OpenCache] Too many failures, blacklisting cache");
                    Blacklist.Add(cache.Id, TimeSpan.FromMinutes(5), "Failed to open");
                    _interactAttempts = 0;
                    return PhaseResult.Failed("Failed to open cache");
                }

                return PhaseResult.InProgress($"Retrying cache interaction (attempt {_interactAttempts})");
            }
            else
            {
                Log.Info("[OpenCache] Successfully opened cache");
                _interactAttempts = 0;

                // Wait for loot to drop
                await Coroutine.Sleep(500);
                
                return PhaseResult.Success("Cache opened");
            }
        }

        public void OnExit()
        {
            _interactAttempts = 0;

            // DON'T disable combat here - let LootingPhase handle it
            // Combat needs to stay enabled while we loot the cache and clear spawned enemies
        }
    }
}
