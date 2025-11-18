namespace Beasts.Perception
{
    /// <summary>
    /// Base interface for perception modules.
    /// Each module is responsible for detecting a specific aspect of the game state.
    /// </summary>
    public interface IPerceptionModule
    {
        /// <summary>
        /// Name of this perception module
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Update the perception data.
        /// Called once per tick before phase execution.
        /// </summary>
        void Update();
    }
}
