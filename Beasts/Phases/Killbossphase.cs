using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Core;
using Beasts.Configuration;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Kill Boss Phase: Handles boss arena detection, entry, combat, and looting
    /// 
    /// WORKFLOW:
    /// PERCEPTION: HeistCaches = [], TargetBeasts = [], BossExists = true, BossAlive = true
    ///     ↓
    /// PHASE: KillBossPhase
    ///     ↓
    /// ACTIONS:
    ///     1. Pathfind to boss room entrance
    ///     2. Enter boss arena (click portal/door)
    ///     3. Engage boss combat
    ///     4. Execute combat rotation until boss dies
    ///     5. Loot boss drops
    ///     ↓
    /// RESULT: BossKillResult (Boss dead → Exit map)
    /// </summary>
    public class KillBossPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "KillBoss";

        // State tracking
        private BossPhaseStep _currentStep = BossPhaseStep.FindBossArena;
        private int _stepAttempts = 0;
        private DateTime _combatStartTime;
        private Vector2i _lastPosition = Vector2i.Zero;
        private int _stuckCounter = 0;
        
        // Boss tracking
        private Monster _currentBoss;
        private NetworkObject _bossArenaEntrance; // Portal or door to boss room
        private bool _inBossRoom = false;
        private bool _bossKilled = false;

        // Configuration
        private const int MAX_STEP_ATTEMPTS = 10;
        private const float BOSS_ARENA_INTERACTION_RANGE = 15f;
        private const float BOSS_COMBAT_RANGE = 30f;
        private const int BOSS_TIMEOUT_SECONDS = 300; // 5 minutes max for boss fight
        private const int LOOT_WAIT_MS = 2000; // Wait for loot to drop

        public bool CanExecute(GameContext context)
        {
            // Execute when:
            // 1. In a map
            // 2. No more heist caches to loot
            // 3. No more target beasts to capture
            // 4. Boss exists and is alive
            // 5. Map is sufficiently explored (>80%)
            
            if (!context.Player.IsInMap)
                return false;

            // Check if farming is complete
            bool farmingComplete = context.Mechanics.HeistCaches.Count == 0 &&
                                  context.Mechanics.TargetBeasts.Count == 0;

            // Check if boss exists
            bool bossAvailable = context.Combat.BossNearby || DetectBossArena() != null;

            // Check exploration level (approximate)
            bool sufficientlyExplored = context.Player.MapExplorationPercent > 80f;

            return farmingComplete && bossAvailable && sufficientlyExplored;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            // Check for stuck state
            CheckStuckState(context.MyPosition);

            // Safety timeout for boss fight
            if (_currentStep == BossPhaseStep.BossCombat &&
                (DateTime.Now - _combatStartTime).TotalSeconds > BOSS_TIMEOUT_SECONDS)
            {
                Log.ErrorFormat("[KillBoss] Boss fight timeout after {0} seconds, aborting",
                    BOSS_TIMEOUT_SECONDS);
                return PhaseResult.Success("Boss fight timeout - moving to exit");
            }

            Log.DebugFormat("[KillBoss] Step: {0}, Attempts: {1}, InBossRoom: {2}, BossKilled: {3}",
                _currentStep, _stepAttempts, _inBossRoom, _bossKilled);

            // Execute current step
            switch (_currentStep)
            {
                case BossPhaseStep.FindBossArena:
                    return await FindBossArena(context);

                case BossPhaseStep.MoveToBossArena:
                    return await MoveToBossArena(context);

                case BossPhaseStep.EnterBossRoom:
                    return await EnterBossRoom(context);

                case BossPhaseStep.BossCombat:
                    return await BossCombat(context);

                case BossPhaseStep.LootBoss:
                    return await LootBoss(context);

                case BossPhaseStep.Complete:
                    return PhaseResult.Success("Boss killed and looted - ready to exit map");

                default:
                    Log.ErrorFormat("[KillBoss] Unknown step: {0}", _currentStep);
                    return PhaseResult.Failed("Unknown boss phase step");
            }
        }

        /// <summary>
        /// Step 1: Scan map for boss arena entrance (portal, special door, etc.)
        /// </summary>
        private async Task<PhaseResult> FindBossArena(GameContext context)
        {
            _bossArenaEntrance = DetectBossArena();

            if (_bossArenaEntrance != null)
            {
                Log.InfoFormat("[KillBoss] Found boss arena entrance: {0} at distance {1}",
                    _bossArenaEntrance.Name, _bossArenaEntrance.Distance);
                
                _currentStep = BossPhaseStep.MoveToBossArena;
                _stepAttempts = 0;
                return PhaseResult.InProgress($"Boss arena found: {_bossArenaEntrance.Name}");
            }

            // If we can't find entrance but boss exists, might already be in boss room
            if (context.Combat.BossNearby)
            {
                Log.Info("[KillBoss] Boss detected nearby, assuming already in boss room");
                _inBossRoom = true;
                _currentStep = BossPhaseStep.BossCombat;
                _combatStartTime = DateTime.Now;
                return PhaseResult.InProgress("Boss detected - engaging combat");
            }

            _stepAttempts++;
            
            if (_stepAttempts >= MAX_STEP_ATTEMPTS)
            {
                Log.Warn("[KillBoss] Could not find boss arena after multiple attempts");
                return PhaseResult.Success("Boss arena not found - skipping boss");
            }

            // Keep exploring to find boss
            await Coroutine.Sleep(500);
            return PhaseResult.InProgress("Searching for boss arena entrance...");
        }

        /// <summary>
        /// Step 2: Move to boss arena entrance
        /// </summary>
        private async Task<PhaseResult> MoveToBossArena(GameContext context)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            if (_bossArenaEntrance == null || !_bossArenaEntrance.IsValid)
            {
                Log.Warn("[KillBoss] Boss arena entrance is no longer valid");
                _currentStep = BossPhaseStep.FindBossArena;
                _stepAttempts = 0;
                return PhaseResult.InProgress("Boss entrance lost - rescanning");
            }

            float distance = _bossArenaEntrance.Distance;

            if (distance <= BOSS_ARENA_INTERACTION_RANGE)
            {
                Log.Info("[KillBoss] Reached boss arena entrance");
                _currentStep = BossPhaseStep.EnterBossRoom;
                _stepAttempts = 0;
                return PhaseResult.InProgress("At boss entrance");
            }

            // Move toward entrance
            Log.DebugFormat("[KillBoss] Moving to boss arena entrance (Distance: {0:F1})", distance);
            
            if (!PlayerMoverManager.Current.MoveTowards(_bossArenaEntrance.Position))
            {
                _stepAttempts++;
                Log.WarnFormat("[KillBoss] Failed to move to boss arena (Attempt {0}/{1})",
                    _stepAttempts, MAX_STEP_ATTEMPTS);
            }

            return PhaseResult.InProgress($"Moving to boss arena (Distance: {distance:F1})");
        }

        /// <summary>
        /// Step 3: Enter boss room (interact with portal/door)
        /// </summary>
        private async Task<PhaseResult> EnterBossRoom(GameContext context)
        {
            if (_bossArenaEntrance == null || !_bossArenaEntrance.IsValid)
            {
                Log.Warn("[KillBoss] Boss arena entrance is no longer valid");
                _currentStep = BossPhaseStep.FindBossArena;
                return PhaseResult.InProgress("Boss entrance lost - rescanning");
            }

            Log.InfoFormat("[KillBoss] Entering boss room: {0}", _bossArenaEntrance.Name);

            // Finish any current action
            await Coroutines.FinishCurrentAction();

            // Interact with entrance (portal, door, etc.)
            var interactResult = await Coroutines.InteractWith(_bossArenaEntrance);

            if (interactResult)
            {
                Log.Info("[KillBoss] Successfully entered boss room");
                _inBossRoom = true;
                _currentStep = BossPhaseStep.BossCombat;
                _combatStartTime = DateTime.Now;
                _stepAttempts = 0;
                
                // Wait for zone transition/loading
                await Coroutine.Sleep(2000);
                
                return PhaseResult.InProgress("Entered boss room - engaging combat");
            }

            _stepAttempts++;
            
            if (_stepAttempts >= MAX_STEP_ATTEMPTS)
            {
                return PhaseResult.Failed("Failed to enter boss room after multiple attempts");
            }

            return PhaseResult.InProgress($"Attempting to enter boss room (Attempt {_stepAttempts})");
        }

        /// <summary>
        /// Step 4: Boss combat - fight until boss is dead
        /// </summary>
        private async Task<PhaseResult> BossCombat(GameContext context)
        {
            // Find boss if we don't have reference
            if (_currentBoss == null || !_currentBoss.IsValid || !_currentBoss.IsAliveHostile)
            {
                _currentBoss = FindBoss();
            }

            // Check if boss is dead
            if (_currentBoss == null || !_currentBoss.IsAliveHostile)
            {
                Log.Info("[KillBoss] Boss is dead!");
                _bossKilled = true;

                // Notify PerceptionManager that boss was killed
                var perceptionMgr = context.PerceptionManager as Perception.PerceptionManager;
                if (perceptionMgr != null)
                {
                    perceptionMgr.MarkBossKilled();
                }
                else
                {
                    Log.Warn("[KillBoss] Cannot notify PerceptionManager - reference not available");
                }

                _currentStep = BossPhaseStep.LootBoss;
                _stepAttempts = 0;

                // Wait for loot to drop
                await Coroutine.Sleep(LOOT_WAIT_MS);

                return PhaseResult.InProgress("Boss killed - collecting loot");
            }

            // Combat logic
            float bossDistance = _currentBoss.Distance;
            float bossHealthPercent = _currentBoss.HealthPercentTotal;

            Log.DebugFormat("[KillBoss] Fighting boss: {0} | HP: {1:F1}% | Distance: {2:F1} | Time: {3:F0}s",
                _currentBoss.Name,
                bossHealthPercent,
                bossDistance,
                (DateTime.Now - _combatStartTime).TotalSeconds);

            // Move closer if too far
            if (bossDistance > BOSS_COMBAT_RANGE)
            {
                PlayerMoverManager.Current.MoveTowards(_currentBoss.Position);
                return PhaseResult.InProgress($"Repositioning for boss combat (Distance: {bossDistance:F1})");
            }

            // Use combat skills
            var settings = BeastRoutineSettings.Instance.Combat;
            var slot = settings.PrimarySkillSlot;
            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
                slot, settings.AlwaysAttackInPlace, _currentBoss.Position);

            return PhaseResult.InProgress($"Fighting boss ({bossHealthPercent:F1}% HP remaining)");
        }

        /// <summary>
        /// Step 5: Loot boss drops
        /// </summary>
        private async Task<PhaseResult> LootBoss(GameContext context)
        {
            // Check for loot around boss death location
            var lootItems = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
                .Where(item => item.IsValid && 
                              item.Distance < 50f &&
                              IsValuableItem(item))
                .OrderBy(item => item.Distance)
                .ToList();

            if (lootItems.Any())
            {
                var closestLoot = lootItems.First();
                
                if (closestLoot.Distance > 10f)
                {
                    PlayerMoverManager.Current.MoveTowards(closestLoot.Position);
                    return PhaseResult.InProgress($"Moving to boss loot (Distance: {closestLoot.Distance:F1})");
                }

                // Pick up loot
                await Coroutines.FinishCurrentAction();
                var pickupResult = await Coroutines.InteractWith(closestLoot);

                if (pickupResult)
                {
                    Log.InfoFormat("[KillBoss] Looted item: {0}", closestLoot.Name);
                }

                return PhaseResult.InProgress("Collecting boss loot...");
            }

            // No more loot, we're done
            Log.Info("[KillBoss] Boss loot collected, phase complete!");
            _currentStep = BossPhaseStep.Complete;
            return PhaseResult.Success("Boss killed and looted - ready to exit map");
        }

        /// <summary>
        /// Detect boss arena entrance (portal, special door, etc.)
        /// </summary>
        private NetworkObject DetectBossArena()
        {
            // Look for common boss arena indicators
            var candidates = LokiPoe.ObjectManager.Objects
                .Where(obj => obj.IsValid && obj.Distance < 200f)
                .Where(obj =>
                {
                    string name = obj.Name.ToLower();
                    string metadata = obj.Metadata.ToLower();

                    // Common boss arena patterns
                    return name.Contains("arena") ||
                           name.Contains("boss") ||
                           name.Contains("portal") && name.Contains("boss") ||
                           metadata.Contains("bossportal") ||
                           metadata.Contains("bossarena") ||
                           metadata.Contains("questportal") ||
                           // Map-specific boss portals
                           name.Contains("aspirant") || // Lab
                           name.Contains("atziri") ||
                           name.Contains("shaper") ||
                           name.Contains("elder") ||
                           // Common transition objects
                           (obj is AreaTransition transition &&
                            (transition.Metadata.Contains("boss") ||
                             transition.Name.ToLower().Contains("boss")));
                })
                .OrderBy(obj => obj.Distance)
                .ToList();

            if (candidates.Any())
            {
                Log.DebugFormat("[KillBoss] Found {0} potential boss arena candidates", candidates.Count);
                return candidates.First();
            }

            return null;
        }

        /// <summary>
        /// Find the boss monster in the current area
        /// </summary>
        private Monster FindBoss()
        {
            var bosses = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Where(m => m.IsValid &&
                           m.IsAliveHostile &&
                           m.Rarity == Rarity.Unique)
                .OrderBy(m => m.Distance)
                .ToList();

            if (bosses.Any())
            {
                var boss = bosses.First();
                Log.InfoFormat("[KillBoss] Boss detected: {0} (Rarity: {1}, HP: {2:F1}%)",
                    boss.Name, boss.Rarity, boss.HealthPercentTotal);
                return boss;
            }

            return null;
        }

        /// <summary>
        /// Check if item is valuable enough to loot from boss
        /// </summary>
        private bool IsValuableItem(WorldItem item)
        {
            // Boss loot is generally valuable, but you can filter here
            // For now, pick up everything from boss
            return true;
        }

        /// <summary>
        /// Detect if player is stuck and handle it
        /// </summary>
        private void CheckStuckState(Vector2i currentPosition)
        {
            if (_lastPosition.Distance(currentPosition) < 2f)
            {
                _stuckCounter++;
                
                if (_stuckCounter > 20)
                {
                    Log.Warn("[KillBoss] Player appears stuck, attempting recovery");
                    // Could implement unstuck logic here
                    _stuckCounter = 0;
                }
            }
            else
            {
                _stuckCounter = 0;
            }

            _lastPosition = currentPosition;
        }

        public void OnExit()
        {
            Reset();
        }

        private void Reset()
        {
            _currentStep = BossPhaseStep.FindBossArena;
            _stepAttempts = 0;
            _currentBoss = null;
            _bossArenaEntrance = null;
            _inBossRoom = false;
            _bossKilled = false;
            _stuckCounter = 0;
            _lastPosition = Vector2i.Zero;

            Log.Debug("[KillBoss] Phase reset");
        }

        private enum BossPhaseStep
        {
            FindBossArena,      // Scan for boss portal/entrance
            MoveToBossArena,    // Path to entrance
            EnterBossRoom,      // Interact with entrance
            BossCombat,         // Fight boss
            LootBoss,           // Collect drops
            Complete            // Done
        }
    }
}
