namespace Beasts.Configuration
{
    /// <summary>
    /// Settings for when to exit the zone
    /// </summary>
    public class ExitConditions
    {
        // Inventory management
        public bool ExitOnInventoryFull { get; set; } = true;

        // Resource management
        public bool ExitOnLowResources { get; set; } = true;
        public int MinHealthPercent { get; set; } = 10;
        public int MinFlasks { get; set; } = 1;

        // Portal scroll management
        public bool ExitOnLowPortalScrolls { get; set; } = true;
        public int MinPortalScrolls { get; set; } = 1;

        // TODO: Add more exit conditions
        // public bool ExitAfterTimeMinutes { get; set; } = false;
        // public int MaxTimeInZoneMinutes { get; set; } = 30;
        // public bool ExitAfterMechanicsFound { get; set; } = false;
        // public int MaxMechanicsToFind { get; set; } = 10;
    }
}
