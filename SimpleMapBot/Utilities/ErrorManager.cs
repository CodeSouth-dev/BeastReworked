using System;
using System.Diagnostics;
using DreamPoeBot.Loki.Bot;
using log4net;

namespace SimpleMapBot.Utilities
{
    /// <summary>
    /// Manages errors and tracks consecutive failures to prevent infinite loops.
    /// Based on MapBot reference implementation.
    /// </summary>
    public static class ErrorManager
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private static int _errorCount = 0;
        private static readonly int MaxConsecutiveErrors = 10;
        private static readonly Stopwatch _errorResetTimer = Stopwatch.StartNew();
        private static readonly TimeSpan ErrorResetTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Reports an error. If too many consecutive errors occur, the bot will stop.
        /// Errors are reset after 2 minutes of no errors.
        /// </summary>
        public static void ReportError()
        {
            // Reset error count if enough time has passed
            if (_errorResetTimer.Elapsed > ErrorResetTimeout)
            {
                Log.Debug("[ErrorManager] Resetting error count after timeout.");
                _errorCount = 0;
            }

            _errorCount++;
            _errorResetTimer.Restart();

            Log.WarnFormat("[ErrorManager] Error reported. Current error count: {0}/{1}",
                _errorCount, MaxConsecutiveErrors);

            if (_errorCount >= MaxConsecutiveErrors)
            {
                Log.Error($"[ErrorManager] Maximum consecutive errors reached ({MaxConsecutiveErrors}). Stopping bot to prevent infinite loop.");
                BotManager.Stop();
            }
        }

        /// <summary>
        /// Resets the error counter. Call this after successful operations to clear error state.
        /// </summary>
        public static void Reset()
        {
            if (_errorCount > 0)
            {
                Log.Debug($"[ErrorManager] Resetting error count (was {_errorCount}).");
                _errorCount = 0;
            }
            _errorResetTimer.Restart();
        }

        /// <summary>
        /// Gets the current error count.
        /// </summary>
        public static int ErrorCount => _errorCount;
    }
}
