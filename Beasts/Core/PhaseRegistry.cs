using System;
using System.Collections.Generic;
using System.Linq;
using DreamPoeBot.Common;
using Beasts.Phases;
using log4net;

namespace Beasts.Core
{
    /// <summary>
    /// Registry for all available phases with priority-based execution
    /// </summary>
    public class PhaseRegistry
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PhaseRegistry));

        private readonly List<PhaseEntry> _phases = new List<PhaseEntry>();

        public PhaseRegistry()
        {
            RegisterPhases();
        }

        private void RegisterPhases()
        {
            // Emergency recovery (highest priority - handles stuck/crashed states)
            Register(new EmergencyRecoveryPhase(), priority: 105);

            // Workflow phases (critical priority)
            Register(new ExitPhase(), priority: 100);
            Register(new ExitAndStashPhase(), priority: 98);

            // Boss handling
            Register(new KillBossPhase(), priority: 95);

            // Farming phases
            Register(new CaptureBeastPhase(), priority: 90);
            Register(new OpenCachePhase(), priority: 80);
            Register(new ClearAreaPhase(), priority: 70);
            Register(new ApproachTargetPhase(), priority: 60);
            Register(new LootingPhase(), priority: 50);
            Register(new CombatPhase(), priority: 40);

            // Map workflow phases (low priority, only when in hideout)
            Register(new PreparationPhase(), priority: 25);  // Startup stashing/restocking
            Register(new OpenMapPhase(), priority: 20);
            Register(new ToHideoutPhase(), priority: 15);

            // Default exploration
            Register(new ExplorationPhase(), priority: 10);

            Log.InfoFormat("[PhaseRegistry] Registered {0} phases", _phases.Count);
        }

        private void Register(IPhase phase, int priority)
        {
            _phases.Add(new PhaseEntry
            {
                Phase = phase,
                Priority = priority
            });

            Log.DebugFormat("[PhaseRegistry] Registered: {0} (Priority: {1})",
                phase.Name, priority);
        }

        /// <summary>
        /// Get the highest priority phase that can execute
        /// </summary>
        public IPhase GetBestPhase(GameContext context)
        {
            var candidates = _phases
                .Where(p => p.Phase.CanExecute(context))
                .OrderByDescending(p => p.Priority)
                .ToList();

            if (candidates.Any())
            {
                var selected = candidates.First();
                Log.DebugFormat("[PhaseRegistry] Selected phase: {0} (Priority: {1})",
                    selected.Phase.Name, selected.Priority);
                return selected.Phase;
            }

            Log.Warn("[PhaseRegistry] No phase can execute!");
            return null;
        }

        /// <summary>
        /// Get a specific phase by type (useful for manual phase selection)
        /// </summary>
        public T GetPhase<T>() where T : IPhase
        {
            return (T)_phases.FirstOrDefault(p => p.Phase is T)?.Phase;
        }

        private class PhaseEntry
        {
            public IPhase Phase { get; set; }
            public int Priority { get; set; }
        }
    }
}
