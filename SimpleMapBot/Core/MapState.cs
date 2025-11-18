using System;

namespace SimpleMapBot.Core
{
    /// <summary>
    /// Tracks the current state of map running
    /// </summary>
    public static class MapState
    {
        /// <summary>
        /// True if we're currently in the middle of a map
        /// </summary>
        public static bool MapInProgress { get; set; } = false;

        /// <summary>
        /// True if the current map is considered "complete" (explored enough)
        /// </summary>
        public static bool MapComplete { get; set; } = false;

        /// <summary>
        /// True if we created a portal to return to this map
        /// </summary>
        public static bool PortalCreated { get; set; } = false;

        /// <summary>
        /// Exploration progress percentage (0-100)
        /// </summary>
        public static int ExplorationPercent { get; set; } = 0;

        /// <summary>
        /// Reset all state (call when starting a new map)
        /// </summary>
        public static void Reset()
        {
            MapInProgress = false;
            MapComplete = false;
            PortalCreated = false;
            ExplorationPercent = 0;
        }
    }
}
