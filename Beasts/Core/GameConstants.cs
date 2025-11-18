namespace Beasts.Core
{
    /// <summary>
    /// Game-related constants used throughout the bot
    /// </summary>
    public static class BeastConstants
    {
        // Inventory thresholds
        public const int MIN_INVENTORY_FULLNESS = 70;
        public const int EMERGENCY_INVENTORY_FULLNESS = 95;
        public const int COMFORTABLE_INVENTORY_FULLNESS = 80;

        // Distance and range constants
        public const float INTERACTION_RANGE = 15f;
        public const float LOOT_PICKUP_RANGE = 10f;
        public const int DEFAULT_DETECTION_RANGE = 80;

        // Movement constants
        public const int MIN_SKILL_CAST_DELAY_MS = 100;
        public const int SINGLE_USE_DISTANCE = 20;
        public const int EXPLORATION_RANGE = 50;

        // Retry and attempt limits
        public const int MAX_INTERACTION_ATTEMPTS = 5;
        public const int MAX_PICKUP_FAILURES = 3;
        public const int MAX_STEP_ATTEMPTS = 10;

        // Timing constants
        public const int QUICKSILVER_COOLDOWN_MS = 5000;
        public const int BUFF_CHECK_INTERVAL_MS = 5000;
        public const int PATH_REFRESH_RATE_MS = 1000;

        // Map completion
        public const int MAP_COMPLETE_MONSTERS_REMAINING = 0;

        // Cooldowns and delays
        public const int SHORT_DELAY_MS = 50;
        public const int MEDIUM_DELAY_MS = 100;
        public const int LONG_DELAY_MS = 500;
        public const int LOOT_DROP_WAIT_MS = 500;
    }
}

