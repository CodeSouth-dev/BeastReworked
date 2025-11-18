using System;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Coroutine;
using log4net;

namespace SimpleMapBot.Utilities
{
    /// <summary>
    /// Utility class for waiting with predicates and timeouts.
    /// Based on MapBot reference implementation.
    /// </summary>
    public static class Wait
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        /// <summary>
        /// Waits for a condition to become true within a timeout period.
        /// </summary>
        /// <param name="condition">The condition to check</param>
        /// <param name="description">Description of what we're waiting for (for logging)</param>
        /// <param name="checkIntervalMs">How often to check the condition (in milliseconds)</param>
        /// <param name="timeoutMs">Maximum time to wait (in milliseconds)</param>
        /// <returns>True if condition was met, false if timeout occurred</returns>
        public static async Task<bool> For(Func<bool> condition, string description, int checkIntervalMs = 100, int timeoutMs = 5000)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);

            Log.DebugFormat("[Wait] Waiting for: {0} (timeout: {1}ms)", description, timeoutMs);

            while (!condition())
            {
                if (DateTime.UtcNow - startTime > timeout)
                {
                    Log.WarnFormat("[Wait] Timeout while waiting for: {0}", description);
                    return false;
                }

                await Coroutine.Sleep(checkIntervalMs);
            }

            Log.DebugFormat("[Wait] Condition met: {0}", description);
            return true;
        }

        /// <summary>
        /// Sleeps for the specified duration.
        /// </summary>
        public static async Task Sleep(int milliseconds)
        {
            await Coroutine.Sleep(milliseconds);
        }

        /// <summary>
        /// Sleeps for a random duration within the specified range.
        /// </summary>
        public static async Task RandomSleep(int minMs, int maxMs)
        {
            var random = new Random();
            var delay = random.Next(minMs, maxMs);
            await Coroutine.Sleep(delay);
        }
    }
}
