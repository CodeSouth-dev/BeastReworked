using System.Collections.Generic;

namespace Beasts.Helpers
{
    /// <summary>
    /// Helper class for matching monster metadata paths against a whitelist
    /// Handles both exact matches and prefix matches (for paths ending with _)
    /// </summary>
    public static class MetadataPathMatcher
    {
        /// <summary>
        /// Checks if a metadata path matches any path in the provided set
        /// </summary>
        /// <param name="metadata">The metadata path to check</param>
        /// <param name="pathSet">Set of whitelisted paths to match against</param>
        /// <returns>True if metadata matches any path in the set</returns>
        public static bool IsMatch(string metadata, HashSet<string> pathSet)
        {
            if (string.IsNullOrEmpty(metadata) || pathSet == null)
                return false;

            foreach (var path in pathSet)
            {
                if (IsPathMatch(metadata, path))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a metadata path matches a specific pattern
        /// Paths ending with _ use StartsWith matching (for variants)
        /// Other paths use exact matching
        /// </summary>
        /// <param name="metadata">The metadata path to check</param>
        /// <param name="pattern">The pattern to match against</param>
        /// <returns>True if metadata matches the pattern</returns>
        public static bool IsPathMatch(string metadata, string pattern)
        {
            if (string.IsNullOrEmpty(metadata) || string.IsNullOrEmpty(pattern))
                return false;

            // Paths ending with _ are prefixes (match variants)
            if (pattern.EndsWith("_"))
            {
                var pathPrefix = pattern.TrimEnd('_');
                return metadata.StartsWith(pathPrefix);
            }

            // Exact match for specific paths
            return metadata.Equals(pattern);
        }
    }
}

