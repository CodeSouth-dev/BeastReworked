using System;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using log4net;

namespace Beasts.Services
{
    /// <summary>
    /// Consolidated stuck detection utility
    /// Eliminates duplicate stuck detection logic across multiple phases
    /// </summary>
    public class StuckDetector
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private Vector2i _lastPosition = Vector2i.Zero;
        private int _stuckCounter = 0;
        private readonly int _stuckThreshold;
        private readonly float _minimumMovementDistance;

        public StuckDetector(int stuckThreshold = 10, float minimumMovementDistance = 5f)
        {
            _stuckThreshold = stuckThreshold;
            _minimumMovementDistance = minimumMovementDistance;
        }

        /// <summary>
        /// Updates stuck detection state. Call this every tick.
        /// Returns true if stuck is detected.
        /// </summary>
        public bool Update(Vector2i currentPosition)
        {
            // First check
            if (_lastPosition == Vector2i.Zero)
            {
                _lastPosition = currentPosition;
                return false;
            }

            // Skip stuck check if player is immobilized by debuffs
            if (IsPlayerImmobilized())
            {
                _lastPosition = currentPosition;
                _stuckCounter = 0; // Reset counter when immobilized
                return false;
            }

            // Check movement
            float distance = currentPosition.Distance(_lastPosition);
            if (distance < _minimumMovementDistance)
            {
                _stuckCounter++;
                if (_stuckCounter >= _stuckThreshold)
                {
                    Log.WarnFormat("[StuckDetector] Stuck detected! No movement for {0} ticks (distance: {1:F1})",
                        _stuckCounter, distance);
                    return true;
                }
            }
            else
            {
                _stuckCounter = 0;
            }

            _lastPosition = currentPosition;
            return false;
        }

        /// <summary>
        /// Resets the stuck detection state
        /// </summary>
        public void Reset()
        {
            _lastPosition = Vector2i.Zero;
            _stuckCounter = 0;
        }

        /// <summary>
        /// Gets current stuck counter value
        /// </summary>
        public int StuckCount => _stuckCounter;

        /// <summary>
        /// Checks if player is immobilized by debuffs (frozen, stunned, etc.)
        /// </summary>
        private bool IsPlayerImmobilized()
        {
            var me = LokiPoe.Me;
            if (me == null)
                return false;

            // Check for common movement-blocking debuffs
            return me.HasAura("frozen") ||
                   me.HasAura("stunned") ||
                   me.HasAura("trapped") ||
                   me.HasAura("petrified");
        }
    }
}
