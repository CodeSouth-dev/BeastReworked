namespace Beasts.Core
{
    /// <summary>
    /// Represents the current state of the mechanic hunting routine.
    /// Used for tracking and debugging, not a strict FSM.
    /// </summary>
    public enum MechanicHunterState
    {
        /// <summary>Roaming the zone looking for mechanics (beasts/caches)</summary>
        Exploring,

        /// <summary>Moving toward a detected mechanic</summary>
        ApproachingTarget,

        /// <summary>Clearing enemies around the mechanic</summary>
        ClearingArea,

        /// <summary>Capturing a beast (throwing net)</summary>
        CapturingBeast,

        /// <summary>Opening a smuggler's cache</summary>
        OpeningCache,

        /// <summary>Picking up items from ground</summary>
        Looting,

        /// <summary>Normal combat with random enemies</summary>
        Combat,

        /// <summary>Exiting the zone (portal/waypoint)</summary>
        Exiting,

        /// <summary>Idle/waiting</summary>
        Idle
    }
}
