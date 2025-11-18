using Beasts.Core;
using DreamPoeBot.Loki.Game;

namespace Beasts.Helpers
{
    /// <summary>
    /// Helper methods for common phase execution conditions
    /// Simplifies CanExecute logic across phases
    /// </summary>
    public static class PhaseConditionHelpers
    {
        /// <summary>
        /// Checks if the map is complete (boss killed, monsters remaining = 0)
        /// </summary>
        public static bool IsMapComplete()
        {
            return LokiPoe.InstanceInfo.MonstersRemaining == BeastConstants.MAP_COMPLETE_MONSTERS_REMAINING;
        }

        /// <summary>
        /// Checks if inventory is critically full (95%+)
        /// </summary>
        public static bool IsInventoryCriticallyFull(GameContext context)
        {
            return context.Player.InventoryFullnessPercent >= BeastConstants.EMERGENCY_INVENTORY_FULLNESS;
        }

        /// <summary>
        /// Checks if inventory is comfortably full (80%+)
        /// </summary>
        public static bool IsInventoryComfortablyFull(GameContext context)
        {
            return context.Player.InventoryFullnessPercent >= BeastConstants.COMFORTABLE_INVENTORY_FULLNESS;
        }

        /// <summary>
        /// Checks if player is in hideout
        /// </summary>
        public static bool IsInHideout(GameContext context)
        {
            return context.Player.IsInHideout;
        }

        /// <summary>
        /// Checks if player is in a map
        /// </summary>
        public static bool IsInMap(GameContext context)
        {
            return context.Player.IsInMap;
        }

        /// <summary>
        /// Checks if player is in town
        /// </summary>
        public static bool IsInTown(GameContext context)
        {
            return context.Player.IsInTown;
        }

        /// <summary>
        /// Checks if map is complete and player should exit
        /// </summary>
        public static bool ShouldExitMap(GameContext context)
        {
            return IsInMap(context) && IsMapComplete();
        }

        /// <summary>
        /// Checks if player should emergency stash (in map, map complete, inventory full)
        /// </summary>
        public static bool ShouldEmergencyStash(GameContext context)
        {
            return ShouldExitMap(context) && IsInventoryCriticallyFull(context);
        }
    }
}

