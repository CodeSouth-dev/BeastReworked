using System.Linq;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Configuration;
using Beasts.Core;
using log4net;

namespace Beasts.Perception
{
    /// <summary>
    /// Detects smuggler's caches for blueprint farming
    /// </summary>
    public class CacheDetector : IPerceptionModule
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "CacheDetector";

        public Chest BestCache { get; private set; }

        public void Update()
        {
            if (!BeastRoutineSettings.Instance.CacheSettings.Enabled)
            {
                BestCache = null;
                return;
            }

            // Find all nearby smuggler's caches
            var caches = LokiPoe.ObjectManager.GetObjectsByType<Chest>()
                .Where(c => c != null &&
                           c.IsValid &&
                           IsSmugglerCache(c) &&
                           !c.IsOpened &&
                           !Blacklist.Contains(c.Id) &&
                           c.Distance < BeastRoutineSettings.Instance.CacheSettings.MaxDetectionRange)
                .OrderBy(c => c.Distance)
                .ToList();

            BestCache = caches.FirstOrDefault();

            if (BestCache != null)
            {
                Log.DebugFormat("[CacheDetector] Found cache: {0} (Distance: {1})",
                    BestCache.Name, BestCache.Distance);
            }
        }

        /// <summary>
        /// Checks if a chest is a smuggler's cache
        /// </summary>
        private bool IsSmugglerCache(Chest c)
        {
            // Check metadata for Heist/smuggler indicators
            if (c.Metadata != null)
            {
                if (c.Metadata.Contains("HeistChest"))
                    return true;

                if (c.Metadata.Contains("SmugglerCache"))
                    return true;

                if (c.Metadata.Contains("Smuggler"))
                    return true;
            }

            // Check name
            if (c.Name != null)
            {
                if (c.Name.Contains("Smuggler's Cache"))
                    return true;

                if (c.Name.Contains("Cache"))
                    return true;
            }

            // TODO: May need to refine based on actual game data

            return false;
        }
    }
}
