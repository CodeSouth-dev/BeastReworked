namespace Beasts.Configuration
{
    /// <summary>
    /// Settings for smuggler's cache detection
    /// </summary>
    public class CacheSettings
    {
        // Enable/disable cache farming
        public bool Enabled { get; set; } = true;

        // Detection range
        public float MaxDetectionRange { get; set; } = 80f;

        // TODO: Add cache type filters if needed
        // public bool OnlyOpenCachesWithBlueprints { get; set; } = false;
    }
}
