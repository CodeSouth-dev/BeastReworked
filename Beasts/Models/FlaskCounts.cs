namespace Beasts.Models
{
    /// <summary>
    /// Stores information about available flasks
    /// </summary>
    public class FlaskCounts
    {
        public int TotalFlasks { get; set; }
        public int LifeFlasks { get; set; }
        public int ManaFlasks { get; set; }
        public int UtilityFlasks { get; set; }
        public int InstantFlasks { get; set; }

        public FlaskCounts()
        {
            TotalFlasks = 0;
            LifeFlasks = 0;
            ManaFlasks = 0;
            UtilityFlasks = 0;
            InstantFlasks = 0;
        }
    }
}

