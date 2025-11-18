using System.Collections.Generic;
using System.Linq;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Configuration;
using Beasts.Core;
using log4net;

namespace Beasts.Perception
{
    /// <summary>
    /// Enhanced heist cache detector that tracks ALL caches in the map
    /// for comprehensive farming workflow
    /// </summary>
    public class HeistCacheDetector : IPerceptionModule
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "HeistCacheDetector";

        // All heist caches detected in current map
        public List<Chest> AllHeistCaches { get; private set; } = new List<Chest>();
        
        // Best single cache for immediate targeting (backward compatibility)
        public Chest BestCache { get; private set; }
        
        // Caches we've successfully looted this session
        public HashSet<int> CompletedCacheIds { get; private set; } = new HashSet<int>();

        // Map tracking
        private string _currentAreaId = "";
        private bool _fullScanCompleted = false;

        public void Update()
        {
            if (!BeastRoutineSettings.Instance.CacheSettings.Enabled)
            {
                AllHeistCaches.Clear();
                BestCache = null;
                return;
            }

            // Reset detection if we changed areas
            if (LokiPoe.CurrentWorldArea?.Id != _currentAreaId)
            {
                ResetForNewArea();
                _currentAreaId = LokiPoe.CurrentWorldArea?.Id ?? "";
            }

            // Perform comprehensive cache detection
            DetectAllHeistCaches();

            // Set best cache for backward compatibility with existing phases
            UpdateBestCache();

            LogCacheStatus();
        }

        /// <summary>
        /// Comprehensive detection of all heist caches in the current map
        /// </summary>
        private void DetectAllHeistCaches()
        {
            // Get all chest objects in the area
            var allChests = LokiPoe.ObjectManager.GetObjectsByType<Chest>()
                .Where(c => c != null && c.IsValid)
                .ToList();

            // Filter to heist caches only
            var heistCaches = allChests
                .Where(c => IsHeistCache(c) && 
                           !c.IsOpened && 
                           !CompletedCacheIds.Contains(c.Id))
                .ToList();

            // Update our cache list
            AllHeistCaches = heistCaches;

            // Mark scan as completed if we found any caches or thoroughly scanned
            if (!_fullScanCompleted)
            {
                _fullScanCompleted = true;
                Log.InfoFormat("[HeistCache] Initial scan complete: found {0} heist caches", AllHeistCaches.Count);
                
                // Log all discovered caches for debugging
                foreach (var cache in AllHeistCaches)
                {
                    Log.DebugFormat("[HeistCache] Discovered: {0} at {1} (Distance: {2}, Metadata: {3})",
                        cache.Name, cache.Position, cache.Distance, cache.Metadata);
                }
            }
        }

        /// <summary>
        /// Update the best cache for immediate targeting (nearest available)
        /// </summary>
        private void UpdateBestCache()
        {
            BestCache = AllHeistCaches
                .Where(c => !Blacklist.Contains(c.Id))
                .OrderBy(c => c.Distance)
                .FirstOrDefault();
        }

        /// <summary>
        /// Enhanced heist cache identification
        /// </summary>
        private bool IsHeistCache(Chest chest)
        {
            // Enhanced metadata checking
            if (chest.Metadata != null)
            {
                var metadata = chest.Metadata.ToLower();
                
                // Primary heist identifiers
                if (metadata.Contains("heistchest") ||
                    metadata.Contains("heistlargechest") ||
                    metadata.Contains("heistsmallchest") ||
                    metadata.Contains("smugglercache") ||
                    metadata.Contains("smuggler"))
                    return true;

                // Secondary identifiers
                if (metadata.Contains("heist") && metadata.Contains("chest"))
                    return true;

                // Generic cache identifiers that might be heist-related
                if (metadata.Contains("contraband") ||
                    metadata.Contains("stash") ||
                    metadata.Contains("cache"))
                {
                    // Additional validation for generic terms
                    // You might want to be more specific here based on actual game data
                    return true;
                }

                // Blueprint/contract specific chests
                if (metadata.Contains("blueprint") ||
                    metadata.Contains("contract"))
                    return true;
            }

            // Enhanced name checking
            if (chest.Name != null)
            {
                var name = chest.Name.ToLower();
                
                // Direct name matches
                if (name.Contains("smuggler's cache") ||
                    name.Contains("smuggler cache") ||
                    name.Contains("heist chest") ||
                    name.Contains("contraband stash"))
                    return true;

                // Partial matches that are likely heist content
                if ((name.Contains("smuggler") && name.Contains("cache")) ||
                    (name.Contains("heist") && (name.Contains("chest") || name.Contains("cache"))) ||
                    name.Contains("contraband"))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Mark a cache as completed (successfully looted)
        /// </summary>
        public void MarkCacheCompleted(int cacheId)
        {
            CompletedCacheIds.Add(cacheId);
            
            // Remove from current cache list
            AllHeistCaches.RemoveAll(c => c.Id == cacheId);
            
            Log.DebugFormat("[HeistCache] Marked cache {0} as completed", cacheId);
        }

        /// <summary>
        /// Get all remaining caches that haven't been completed
        /// </summary>
        public List<Chest> GetRemainingCaches()
        {
            return AllHeistCaches
                .Where(c => c.IsValid && 
                           !c.IsOpened && 
                           !CompletedCacheIds.Contains(c.Id) &&
                           !Blacklist.Contains(c.Id))
                .ToList();
        }

        /// <summary>
        /// Check if all heist caches in the current map have been completed
        /// </summary>
        public bool AreAllCachesCompleted()
        {
            var remainingCaches = GetRemainingCaches();
            return !remainingCaches.Any();
        }

        /// <summary>
        /// Get cache statistics for the current map
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            var remaining = GetRemainingCaches();
            return new CacheStatistics
            {
                TotalFound = AllHeistCaches.Count + CompletedCacheIds.Count,
                Completed = CompletedCacheIds.Count,
                Remaining = remaining.Count,
                InProgress = remaining.Count > 0
            };
        }

        /// <summary>
        /// Reset state for a new area
        /// </summary>
        private void ResetForNewArea()
        {
            AllHeistCaches.Clear();
            CompletedCacheIds.Clear();
            BestCache = null;
            _fullScanCompleted = false;
            
            Log.Info("[HeistCache] Reset state for new area");
        }

        /// <summary>
        /// Log current cache detection status
        /// </summary>
        private void LogCacheStatus()
        {
            // Only log periodically to avoid spam
            if (_fullScanCompleted && AllHeistCaches.Count > 0)
            {
                var stats = GetCacheStatistics();

                // Log every 10 seconds or when status changes significantly
                Log.DebugFormat("[HeistCache] Status: {0} total, {1} completed, {2} remaining",
                    stats.TotalFound, stats.Completed, stats.Remaining);
            }
        }

        public float CompletionPercentage
        {
            get
            {
                var stats = GetCacheStatistics();
                return stats.TotalFound > 0 ? (float)stats.Completed / stats.TotalFound * 100f : 0f;
            }
        }

        /// <summary>
        /// Get caches within a specific distance
        /// </summary>
        public List<Chest> GetCachesWithinRange(float maxDistance)
        {
            return GetRemainingCaches()
                .Where(c => c.Distance <= maxDistance)
                .OrderBy(c => c.Distance)
                .ToList();
        }

        /// <summary>
        /// Get the nearest cache to a specific position
        /// </summary>
        public Chest GetNearestCacheTo(Vector2i position)
        {
            return GetRemainingCaches()
                .OrderBy(c => c.Position.Distance(position))
                .FirstOrDefault();
        }

        /// <summary>
        /// Check if a specific cache is a high-priority target
        /// </summary>
        public bool IsHighPriorityCache(Chest cache)
        {
            // Could be enhanced based on cache type, contents, etc.
            // For now, all heist caches are considered high priority
            return IsHeistCache(cache);
        }
    }

    /// <summary>
    /// Cache statistics for reporting
    /// </summary>
    public class CacheStatistics
    {
        public int TotalFound { get; set; }
        public int Completed { get; set; }
        public int Remaining { get; set; }
        public bool InProgress { get; set; }

        public float CompletionPercentage => TotalFound > 0 ? (float)Completed / TotalFound * 100f : 0f;

        public override string ToString()
        {
            return $"Caches: {Completed}/{TotalFound} ({CompletionPercentage:F1}%), {Remaining} remaining";
        }
    }
}
