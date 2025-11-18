using System.Threading.Tasks;
using Beasts.Core;

namespace Beasts.Phases
{
    /// <summary>
    /// Base interface for execution phases.
    /// Each phase represents a distinct behavior mode (exploration, combat, looting, etc.)
    /// </summary>
    public interface IPhase
    {
        string Name { get; }
        bool CanExecute(GameContext context);
        Task<PhaseResult> Execute(GameContext context);  // ✅ Returns PhaseResult
        void OnExit();
    }
}
