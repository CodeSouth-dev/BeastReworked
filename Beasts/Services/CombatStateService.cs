using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using log4net;

namespace Beasts.Services
{
    /// <summary>
    /// Service for managing combat state across phases
    /// Centralizes combat enable/disable logic
    /// </summary>
    public static class CombatStateService
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();
        private static string _lastSender = "";
        private static bool _lastState = false;

        /// <summary>
        /// Enables combat at a specific position
        /// </summary>
        /// <param name="sender">The phase requesting combat</param>
        /// <param name="position">The position to focus combat around</param>
        public static void EnableCombat(object sender, Vector2i position)
        {
            var msg = new Message("EnableCombat", sender, position);
            RoutineManager.Current.Message(msg);
            
            // Only log if state changed
            var senderName = sender.GetType().Name;
            if (_lastSender != senderName || !_lastState)
            {
                Log.InfoFormat("[CombatState] Combat ENABLED by {0}", senderName);
                _lastSender = senderName;
                _lastState = true;
            }
        }

        /// <summary>
        /// Disables combat (zoom mode)
        /// </summary>
        /// <param name="sender">The phase requesting combat disable</param>
        public static void DisableCombat(object sender)
        {
            var msg = new Message("DisableCombat", sender);
            RoutineManager.Current.Message(msg);
            
            // Only log if state changed
            var senderName = sender.GetType().Name;
            if (_lastSender != senderName || _lastState)
            {
                Log.InfoFormat("[CombatState] Combat DISABLED by {0}", senderName);
                _lastSender = senderName;
                _lastState = false;
            }
        }
    }
}

