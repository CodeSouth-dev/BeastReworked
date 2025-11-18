using System;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using Beasts.Core;
using Beasts.Services;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Looting phase: Picks up valuable items from the ground
    /// </summary>
    public class LootingPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "Looting";

        private int _pickupFailures = 0;

        public bool CanExecute(GameContext context)
        {
            // Loot when valuable items are nearby (even if enemies are present)
            // Combat routine will handle clearing enemies while we loot
            return context.Loot.ValuableItemsNearby;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            var item = context.Loot.ClosestItem;
            if (item == null)
            {
                // No more items to loot - we're done here
                return PhaseResult.Success("All loot collected");
            }

            // Combat should already be enabled from ApproachTargetPhase (for cache loot)
            // Keep it enabled while looting

            var itemName = item.Item?.Name ?? "Unknown";

            // Move to item if too far
            if (item.Distance > 10f)
            {
                PlayerMoverManager.Current.MoveTowards(item.Position);
                return PhaseResult.InProgress($"Moving to item: {itemName}");
            }

            // Attempt to pick up
            await Coroutines.FinishCurrentAction();

            // Click on item
            if (!await Coroutines.InteractWith(item))
            {
                _pickupFailures++;
                Log.WarnFormat("[Looting] Failed to pickup {0} (attempt {1})", itemName, _pickupFailures);

                if (_pickupFailures > 3)
                {
                    Blacklist.Add(item.Id, TimeSpan.FromMinutes(2), "Failed to pickup");
                    _pickupFailures = 0;
                    return PhaseResult.InProgress("Failed pickup, moving to next item");
                }

                return PhaseResult.InProgress($"Retrying pickup: {itemName}");
            }

            Log.InfoFormat("[Looting] Picked up {0}", itemName);
            _pickupFailures = 0;

            // Small delay after pickup
            await Coroutine.Sleep(100);

            return PhaseResult.InProgress("Item picked up");
        }

        public void OnExit()
        {
            _pickupFailures = 0;
            
            // Disable combat when done looting
            CombatStateService.DisableCombat(this);
        }
    }
}
