using System.Collections.Generic;
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
    /// Comprehensive Farming Phase: Simultaneously clears maps, hunts beasts, and loots heist caches
    /// 
    /// PRIORITY SYSTEM:
    /// 1. Combat (immediate threats)
    /// 2. Beast capture (when ready and low HP)
    /// 3. Heist cache opening (valuable loot)
    /// 4. Beast hunting (damage beasts to capture threshold)
    /// 5. Map exploration and clearing
    /// 
    /// WORKFLOW:
    /// - Maintains awareness of ALL targets (beasts, caches, enemies)
    /// - Routes efficiently between objectives
    /// - Engages combat proactively while moving
    /// - Prioritizes high-value targets dynamically
    /// </summary>
    public class ComprehensiveFarmingPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "ComprehensiveFarming";

        // Target tracking
        private readonly HashSet<int> _completedCacheIds = new HashSet<int>();
        private readonly HashSet<int> _capturedBeastIds = new HashSet<int>();
        private readonly Dictionary<int, float> _beastHealthTracking = new Dictionary<int, float>();
        
        // Route planning
        private List<FarmingTarget> _allTargets = new List<FarmingTarget>();
        private FarmingTarget _currentPrimaryTarget;
        private Vector2i _explorationTarget = Vector2i.Zero;
        
        // State management
        private FarmingStep _currentStep = FarmingStep.ScanAndPlan;
        private int _stepAttempts = 0;
        private int _stuckCounter = 0;
        private Vector2i _lastPosition = Vector2i.Zero;
        
        // Statistics
        private int _beastsCaptured = 0;
        private int _cachesOpened = 0;
        private int _enemiesKilled = 0;

        public bool CanExecute(GameContext context)
        {
            // Run in any map/combat area
            return context.Player.IsInMap;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            // Update target tracking first
            UpdateTargetTracking(context);

            Log.DebugFormat("[ComprehensiveFarming] Step: {0}, Primary: {1}, Targets: {2}, Stats: B{3}/C{4}/E{5}",
                _currentStep,
                _currentPrimaryTarget?.Name ?? "None",
                _allTargets.Count,
                _beastsCaptured, _cachesOpened, _enemiesKilled);

            switch (_currentStep)
            {
                case FarmingStep.ScanAndPlan:
                    return await ScanAndPlanTargets(context);

                case FarmingStep.Combat:
                    return await HandleCombat(context);

                case FarmingStep.CaptureBeast:
                    return await CaptureBeast(context);

                case FarmingStep.OpenCache:
                    return await OpenCache(context);

                case FarmingStep.ApproachTarget:
                    return await ApproachTarget(context);

                case FarmingStep.LootItems:
                    return await LootItems(context);

                case FarmingStep.Explore:
                    return await ExploreAndClear(context);

                default:
                    return PhaseResult.Failed($"Unknown step: {_currentStep}");
            }
        }

        /// <summary>
        /// Scan for all farming targets and prioritize them
        /// </summary>
        private async Task<PhaseResult> ScanAndPlanTargets(GameContext context)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            _allTargets.Clear();

            // Scan for heist caches
            var caches = LokiPoe.ObjectManager.GetObjectsByType<Chest>()
                .Where(c => c != null && c.IsValid && IsHeistCache(c) && 
                           !c.IsOpened && !_completedCacheIds.Contains(c.Id))
                .Select(c => new FarmingTarget
                {
                    Type = FarmingTargetType.HeistCache,
                    Object = c,
                    Position = c.Position,
                    Priority = CalculateCachePriority(c),
                    Name = c.Name
                })
                .ToList();

            // Scan for beasts
            var beasts = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Where(m => m != null && m.IsValid && IsBeast(m) && 
                           ShouldCapture(m) && !_capturedBeastIds.Contains(m.Id) &&
                           !Blacklist.Contains(m.Id))
                .Select(m => new FarmingTarget
                {
                    Type = FarmingTargetType.Beast,
                    Object = m,
                    Position = m.Position,
                    Priority = CalculateBeastPriority(m),
                    Name = m.Name
                })
                .ToList();

            // Scan for general enemies (for clearing)
            var enemies = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Where(m => m != null && m.IsValid && m.IsAliveHostile && 
                           !IsBeast(m) && !Blacklist.Contains(m.Id))
                .Select(m => new FarmingTarget
                {
                    Type = FarmingTargetType.Enemy,
                    Object = m,
                    Position = m.Position,
                    Priority = CalculateEnemyPriority(m),
                    Name = m.Name
                })
                .ToList();

            _allTargets.AddRange(caches);
            _allTargets.AddRange(beasts);
            _allTargets.AddRange(enemies);

            Log.InfoFormat("[ComprehensiveFarming] Scan complete: {0} caches, {1} beasts, {2} enemies",
                caches.Count, beasts.Count, enemies.Count);

            // Select next action based on priorities
            _currentStep = SelectOptimalAction(context);
            _stepAttempts = 0;

            return PhaseResult.InProgress($"Scanned {_allTargets.Count} targets");
        }

        /// <summary>
        /// Handle immediate combat threats
        /// </summary>
        private async Task<PhaseResult> HandleCombat(GameContext context)
        {
            var threats = _allTargets
                .Where(t => t.Type == FarmingTargetType.Enemy && 
                           t.Object.Distance < BeastRoutineSettings.Instance.Combat.CombatRange)
                .OrderBy(t => t.Object.Distance)
                .ThenByDescending(t => t.Priority)
                .ToList();

            if (!threats.Any())
            {
                Log.Debug("[ComprehensiveFarming] No immediate threats, changing strategy");
                _currentStep = SelectOptimalAction(context);
                return PhaseResult.InProgress("No threats, reselecting target");
            }

            var threat = threats.First();
            Log.DebugFormat("[ComprehensiveFarming] Engaging threat: {0} (Distance: {1})", 
                threat.Name, threat.Object.Distance);

            var combatResult = await PerformCombat(context, threat.Object as Monster);

            var monster = threat.Object as Monster;
            if (monster != null && !monster.IsAliveHostile)
            {
                _enemiesKilled++;
                _allTargets.Remove(threat);
                Log.DebugFormat("[ComprehensiveFarming] Killed {0} (Total: {1})", threat.Name, _enemiesKilled);
            }

            // After each combat action, reassess situation
            _currentStep = FarmingStep.ScanAndPlan;
            return PhaseResult.InProgress($"Combat: {threats.Count} threats");
        }

        /// <summary>
        /// Capture a beast that's ready (low HP)
        /// </summary>
        private async Task<PhaseResult> CaptureBeast(GameContext context)
        {
            if (_currentPrimaryTarget?.Type != FarmingTargetType.Beast)
            {
                _currentStep = FarmingStep.ScanAndPlan;
                return PhaseResult.InProgress("No beast target selected");
            }

            var beast = _currentPrimaryTarget.Object as Monster;
            if (beast == null || !beast.IsValid || !beast.IsAliveHostile)
            {
                _allTargets.Remove(_currentPrimaryTarget);
                _currentPrimaryTarget = null;
                _currentStep = FarmingStep.ScanAndPlan;
                return PhaseResult.InProgress("Beast target invalid");
            }

            // Check if beast is ready for capture
            if (beast.HealthPercentTotal > BeastRoutineSettings.Instance.BeastCapture.CaptureHealthPercent)
            {
                // Beast not ready yet, attack it
                Log.DebugFormat("[ComprehensiveFarming] Damaging beast {0} (HP: {1}%)", 
                    beast.Name, beast.HealthPercentTotal);
                
                var combatResult = await PerformCombat(context, beast);
                return PhaseResult.InProgress($"Damaging beast (HP: {beast.HealthPercentTotal:F1}%)");
            }

            // Make sure we're close enough
            if (beast.Distance > 20f)
            {
                PlayerMoverManager.Current.MoveTowards(beast.Position);
                return PhaseResult.InProgress($"Moving to capture beast (Distance: {beast.Distance:F1})");
            }

            // Capture the beast
            Log.InfoFormat("[ComprehensiveFarming] Capturing beast: {0} (HP: {1}%)",
                beast.Name, beast.HealthPercentTotal);

            // Note: Net throwing is not needed - Einhar automatically captures beasts when killed
            // Just mark this beast as being processed to avoid re-engagement
            _capturedBeastIds.Add(beast.Id);
            _beastsCaptured++;
            _allTargets.Remove(_currentPrimaryTarget);
            _currentPrimaryTarget = null;
            _currentStep = FarmingStep.ScanAndPlan;

            Log.InfoFormat("[ComprehensiveFarming] Beast captured! Total: {0}", _beastsCaptured);
            return PhaseResult.InProgress("Beast captured");
        }

        /// <summary>
        /// Open a heist cache
        /// </summary>
        private async Task<PhaseResult> OpenCache(GameContext context)
        {
            if (_currentPrimaryTarget?.Type != FarmingTargetType.HeistCache)
            {
                _currentStep = FarmingStep.ScanAndPlan;
                return PhaseResult.InProgress("No cache target selected");
            }

            var cache = _currentPrimaryTarget.Object as Chest;
            if (cache == null || !cache.IsValid || cache.IsOpened)
            {
                _allTargets.Remove(_currentPrimaryTarget);
                _currentPrimaryTarget = null;
                _currentStep = FarmingStep.ScanAndPlan;
                return PhaseResult.InProgress("Cache target invalid");
            }

            // Check for enemies near cache first
            var nearbyEnemies = _allTargets
                .Where(t => t.Type == FarmingTargetType.Enemy && 
                           t.Position.Distance(cache.Position) < 25f)
                .ToList();

            if (nearbyEnemies.Any())
            {
                Log.DebugFormat("[ComprehensiveFarming] {0} enemies near cache, clearing first", 
                    nearbyEnemies.Count);
                _currentStep = FarmingStep.Combat;
                return PhaseResult.InProgress("Clearing enemies near cache");
            }

            // Make sure we're close enough
            if (cache.Distance > 10f)
            {
                PlayerMoverManager.Current.MoveTowards(cache.Position);
                return PhaseResult.InProgress($"Moving to cache (Distance: {cache.Distance:F1})");
            }

            // Open the cache
            Log.InfoFormat("[ComprehensiveFarming] Opening cache: {0}", cache.Name);

            await Coroutines.FinishCurrentAction();
            var interactResult = await Coroutines.InteractWith(cache);

            if (interactResult)
            {
                _completedCacheIds.Add(cache.Id);
                _cachesOpened++;
                Log.InfoFormat("[ComprehensiveFarming] Cache opened! Total: {0}", _cachesOpened);
                
                // Wait for loot to drop
                await Coroutine.Sleep(300);
                _currentStep = FarmingStep.LootItems;
                return PhaseResult.InProgress("Cache opened, looting");
            }
            else
            {
                _stepAttempts++;
                if (_stepAttempts > 5)
                {
                    Log.WarnFormat("[ComprehensiveFarming] Failed to open cache {0} after {1} attempts", 
                        cache.Name, _stepAttempts);
                    _completedCacheIds.Add(cache.Id); // Mark as completed to avoid retry
                    _allTargets.Remove(_currentPrimaryTarget);
                    _currentPrimaryTarget = null;
                    _currentStep = FarmingStep.ScanAndPlan;
                }
                return PhaseResult.InProgress("Retrying cache interaction");
            }
        }

        /// <summary>
        /// Approach the current primary target
        /// </summary>
        private async Task<PhaseResult> ApproachTarget(GameContext context)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            if (_currentPrimaryTarget == null)
            {
                _currentStep = FarmingStep.ScanAndPlan;
                return PhaseResult.InProgress("No target to approach");
            }

            var target = _currentPrimaryTarget.Object;
            if (!target.IsValid)
            {
                _allTargets.Remove(_currentPrimaryTarget);
                _currentPrimaryTarget = null;
                _currentStep = FarmingStep.ScanAndPlan;
                return PhaseResult.InProgress("Target became invalid");
            }

            var distance = target.Distance;
            var targetPos = target.Position;

            // Check if we're close enough for the target type
            float requiredDistance = GetRequiredDistance(_currentPrimaryTarget.Type);
            if (distance <= requiredDistance)
            {
                // We're close enough, transition to appropriate action
                _currentStep = GetTargetAction(_currentPrimaryTarget.Type);
                _stepAttempts = 0;
                return PhaseResult.InProgress($"Reached {_currentPrimaryTarget.Type}");
            }

            // Move toward target
            Log.DebugFormat("[ComprehensiveFarming] Approaching {0}: {1} (Distance: {2})", 
                _currentPrimaryTarget.Type, _currentPrimaryTarget.Name, distance);

            var moveSuccess = PlayerMoverManager.Current.MoveTowards(targetPos);
            if (!moveSuccess)
            {
                _stepAttempts++;
                if (_stepAttempts > 10)
                {
                    Log.WarnFormat("[ComprehensiveFarming] Failed to reach {0} after {1} attempts", 
                        _currentPrimaryTarget.Name, _stepAttempts);
                    Blacklist.Add(target.Id, System.TimeSpan.FromMinutes(5), "Unreachable");
                    _allTargets.Remove(_currentPrimaryTarget);
                    _currentPrimaryTarget = null;
                    _currentStep = FarmingStep.ScanAndPlan;
                }
                return PhaseResult.InProgress("Pathfinding failed, retrying");
            }

            // Check for opportunities while moving
            CheckOpportunitiesWhileMoving(context);

            return PhaseResult.InProgress($"Moving to {_currentPrimaryTarget.Type} (Distance: {distance:F1})");
        }

        /// <summary>
        /// Loot items on the ground
        /// </summary>
        private async Task<PhaseResult> LootItems(GameContext context)
        {
            if (!context.Loot.ValuableItemsNearby)
            {
                _currentStep = FarmingStep.ScanAndPlan;
                return PhaseResult.InProgress("No items to loot");
            }

            var item = context.Loot.ClosestItem;
            var itemName = item?.Item?.Name ?? "Unknown";

            if (item.Distance > 20f)
            {
                PlayerMoverManager.Current.MoveTowards(item.Position);
                return PhaseResult.InProgress($"Moving to item: {itemName}");
            }

            await Coroutines.FinishCurrentAction();
            var pickupResult = await Coroutines.InteractWith(item);

            if (pickupResult)
            {
                Log.DebugFormat("[ComprehensiveFarming] Picked up: {0}", itemName);
                await Coroutine.Sleep(100);
            }
            else
            {
                Blacklist.Add(item.Id, System.TimeSpan.FromMinutes(2), "Failed pickup");
            }

            // Continue looting if more items available
            return PhaseResult.InProgress("Looting items");
        }

        /// <summary>
        /// Explore the map and clear enemies
        /// </summary>
        private async Task<PhaseResult> ExploreAndClear(GameContext context)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            var myPos = context.MyPosition;

            // Check if stuck
            if (_lastPosition != Vector2i.Zero && myPos.Distance(_lastPosition) < 5)
            {
                _stuckCounter++;
                if (_stuckCounter > 10)
                {
                    Log.Warn("[ComprehensiveFarming] Stuck detected, picking new exploration point");
                    _explorationTarget = Vector2i.Zero;
                    _stuckCounter = 0;
                }
            }
            else
            {
                _stuckCounter = 0;
            }
            _lastPosition = myPos;

            // Pick new exploration target if needed
            if (_explorationTarget == Vector2i.Zero || myPos.Distance(_explorationTarget) < 30)
            {
                _explorationTarget = PickExplorationPoint(myPos, context);
                Log.DebugFormat("[ComprehensiveFarming] New exploration target: {0}", _explorationTarget);
            }

            // Move toward exploration point
            var moveSuccess = PlayerMoverManager.Current.MoveTowards(_explorationTarget);
            if (!moveSuccess)
            {
                _explorationTarget = Vector2i.Zero;
            }

            // Always rescan while exploring to catch new targets
            _currentStep = FarmingStep.ScanAndPlan;
            
            return PhaseResult.InProgress($"Exploring toward {_explorationTarget}");
        }

        /// <summary>
        /// Select the optimal action based on current priorities
        /// </summary>
        private FarmingStep SelectOptimalAction(GameContext context)
        {
            // 1. IMMEDIATE COMBAT THREATS (within combat range)
            var immediateThreat = _allTargets
                .Where(t => t.Type == FarmingTargetType.Enemy && 
                           t.Object.Distance < BeastRoutineSettings.Instance.Combat.CombatRange)
                .OrderBy(t => t.Object.Distance)
                .FirstOrDefault();

            if (immediateThreat != null)
            {
                Log.Debug("[ComprehensiveFarming] Immediate threat detected");
                return FarmingStep.Combat;
            }

            // 2. BEAST READY FOR CAPTURE
            var captureReadyBeast = _allTargets
                .Where(t => t.Type == FarmingTargetType.Beast)
                .Cast<FarmingTarget>()
                .Where(t => (t.Object as Monster).HealthPercentTotal <= 
                           BeastRoutineSettings.Instance.BeastCapture.CaptureHealthPercent)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.Object.Distance)
                .FirstOrDefault();

            if (captureReadyBeast != null)
            {
                _currentPrimaryTarget = captureReadyBeast;
                Log.DebugFormat("[ComprehensiveFarming] Beast ready for capture: {0}", captureReadyBeast.Name);
                return captureReadyBeast.Object.Distance <= 20f ? FarmingStep.CaptureBeast : FarmingStep.ApproachTarget;
            }

            // 3. HEIST CACHE WITHIN REASONABLE DISTANCE
            var nearbyCache = _allTargets
                .Where(t => t.Type == FarmingTargetType.HeistCache && t.Object.Distance <= 50f)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.Object.Distance)
                .FirstOrDefault();

            if (nearbyCache != null)
            {
                _currentPrimaryTarget = nearbyCache;
                Log.DebugFormat("[ComprehensiveFarming] Nearby cache selected: {0}", nearbyCache.Name);
                return nearbyCache.Object.Distance <= 10f ? FarmingStep.OpenCache : FarmingStep.ApproachTarget;
            }

            // 4. BEAST HUNTING (damage beasts to capture threshold)
            var huntableBeast = _allTargets
                .Where(t => t.Type == FarmingTargetType.Beast && t.Object.Distance <= 60f)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.Object.Distance)
                .FirstOrDefault();

            if (huntableBeast != null)
            {
                _currentPrimaryTarget = huntableBeast;
                Log.DebugFormat("[ComprehensiveFarming] Beast hunting target: {0}", huntableBeast.Name);
                return huntableBeast.Object.Distance <= 30f ? FarmingStep.CaptureBeast : FarmingStep.ApproachTarget;
            }

            // 5. LOOT NEARBY ITEMS
            if (context.Loot.ValuableItemsNearby)
            {
                Log.Debug("[ComprehensiveFarming] Looting nearby items");
                return FarmingStep.LootItems;
            }

            // 6. DISTANT CACHE (worth traveling for)
            var distantCache = _allTargets
                .Where(t => t.Type == FarmingTargetType.HeistCache)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.Object.Distance)
                .FirstOrDefault();

            if (distantCache != null)
            {
                _currentPrimaryTarget = distantCache;
                Log.DebugFormat("[ComprehensiveFarming] Distant cache selected: {0}", distantCache.Name);
                return FarmingStep.ApproachTarget;
            }

            // 7. GENERAL EXPLORATION AND CLEARING
            Log.Debug("[ComprehensiveFarming] No priority targets, exploring");
            _currentPrimaryTarget = null;
            return FarmingStep.Explore;
        }

        /// <summary>
        /// Update target tracking and cleanup invalid targets
        /// </summary>
        private void UpdateTargetTracking(GameContext context)
        {
            // Remove invalid targets
            _allTargets.RemoveAll(t => !t.Object.IsValid ||
                                     (t.Object as Monster != null && !(t.Object as Monster).IsAliveHostile) ||
                                     (t.Object as Chest)?.IsOpened == true);

            // Update beast health tracking
            foreach (var target in _allTargets.Where(t => t.Type == FarmingTargetType.Beast))
            {
                var beast = target.Object as Monster;
                if (beast != null)
                {
                    _beastHealthTracking[beast.Id] = beast.HealthPercentTotal;
                }
            }

            // Clear current primary target if invalid
            if (_currentPrimaryTarget != null && !_currentPrimaryTarget.Object.IsValid)
            {
                _currentPrimaryTarget = null;
            }
        }

        /// <summary>
        /// Check for opportunities while moving (combat, items, etc.)
        /// </summary>
        private void CheckOpportunitiesWhileMoving(GameContext context)
        {
            // If enemies get too close while traveling, switch to combat
            var nearbyThreat = _allTargets
                .Where(t => t.Type == FarmingTargetType.Enemy && t.Object.Distance < 20f)
                .OrderBy(t => t.Object.Distance)
                .FirstOrDefault();

            if (nearbyThreat != null)
            {
                Log.Debug("[ComprehensiveFarming] Threat approached while traveling, switching to combat");
                _currentStep = FarmingStep.Combat;
            }

            // Pick up valuable items if very close
            if (context.Loot.ValuableItemsNearby && context.Loot.ClosestItem.Distance < 10f)
            {
                _currentStep = FarmingStep.LootItems;
            }
        }

        #region Target Detection and Priority Calculation

        private bool IsHeistCache(Chest chest)
        {
            if (chest.Metadata != null)
            {
                var metadata = chest.Metadata.ToLower();
                if (metadata.Contains("heistchest") || metadata.Contains("smugglercache") ||
                    metadata.Contains("smuggler") || metadata.Contains("heist") ||
                    metadata.Contains("contraband") || metadata.Contains("stash"))
                    return true;
            }

            if (chest.Name != null)
            {
                var name = chest.Name.ToLower();
                if (name.Contains("smuggler") || name.Contains("cache") ||
                    name.Contains("stash") || name.Contains("contraband"))
                    return true;
            }

            return false;
        }

        private bool IsBeast(Monster monster)
        {
            // Check metadata for beast indicators
            if (monster.Metadata != null && monster.Metadata.Contains("Bestiary"))
                return true;

            // Check if monster name contains beast-related keywords
            if (monster.Name != null)
            {
                var lowerName = monster.Name.ToLower();
                if (lowerName.Contains("beast") ||
                    lowerName.Contains("red beast") ||
                    lowerName.Contains("fenumus") ||
                    lowerName.Contains("saqawal") ||
                    lowerName.Contains("farrul") ||
                    lowerName.Contains("craiceann"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldCapture(Monster beast)
        {
            var settings = BeastRoutineSettings.Instance.BeastCapture;
            switch (beast.Rarity)
            {
                case Rarity.Unique: return settings.CaptureUnique;
                case Rarity.Rare: return settings.CaptureRare;
                case Rarity.Magic: return settings.CaptureMagic;
                case Rarity.Normal: return settings.CaptureNormal;
                default: return false;
            }
        }

        private float CalculateCachePriority(Chest cache)
        {
            float priority = 100f; // Base cache priority
            
            // Closer caches have slight priority bonus
            priority += (100f - cache.Distance) * 0.1f;
            
            // TODO: Could analyze cache type for additional priority
            return priority;
        }

        private float CalculateBeastPriority(Monster beast)
        {
            float priority = 0f;

            // Rarity priority
            switch (beast.Rarity)
            {
                case Rarity.Unique: priority += 150f; break;
                case Rarity.Rare: priority += 100f; break;
                case Rarity.Magic: priority += 50f; break;
                case Rarity.Normal: priority += 25f; break;
            }

            // Health-based priority (lower HP = higher priority for capture)
            var healthPercent = beast.HealthPercentTotal;
            var captureThreshold = BeastRoutineSettings.Instance.BeastCapture.CaptureHealthPercent;
            
            if (healthPercent <= captureThreshold)
            {
                priority += 200f; // Ready to capture - very high priority
            }
            else
            {
                // Closer to capture threshold = higher priority
                priority += (100f - healthPercent) * 0.5f;
            }

            // Distance factor
            priority += (100f - beast.Distance) * 0.1f;

            return priority;
        }

        private float CalculateEnemyPriority(Monster enemy)
        {
            float priority = 20f; // Base enemy priority

            // Dangerous enemies get higher priority
            switch (enemy.Rarity)
            {
                case Rarity.Unique: priority += 40f; break;
                case Rarity.Rare: priority += 20f; break;
                case Rarity.Magic: priority += 10f; break;
            }

            // Closer enemies are higher priority
            priority += (50f - enemy.Distance) * 0.5f;

            // Special dangerous mods
            if (enemy.HasAura("monster_aura_cannot_die"))
                priority += 30f;

            return priority;
        }

        #endregion

        #region Utility Methods

        private float GetRequiredDistance(FarmingTargetType type)
        {
            switch (type)
            {
                case FarmingTargetType.HeistCache: return 10f;
                case FarmingTargetType.Beast: return 20f;
                case FarmingTargetType.Enemy: return 25f;
                default: return 15f;
            }
        }

        private FarmingStep GetTargetAction(FarmingTargetType type)
        {
            switch (type)
            {
                case FarmingTargetType.HeistCache: return FarmingStep.OpenCache;
                case FarmingTargetType.Beast: return FarmingStep.CaptureBeast;
                case FarmingTargetType.Enemy: return FarmingStep.Combat;
                default: return FarmingStep.ScanAndPlan;
            }
        }

        private Vector2i PickExplorationPoint(Vector2i currentPos, GameContext context)
        {
            // Try to pick a point that might have uncovered targets
            var range = 100; // Exploration range
            
            // Pick a random direction but favor areas we haven't been to recently
            var randomOffset = new Vector2i(
                (int)LokiPoe.Random.Next(-range, range),
                (int)LokiPoe.Random.Next(-range, range)
            );

            return currentPos + randomOffset;
        }

        private async Task<LogicResult> PerformCombat(GameContext context, Monster target)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            var myPos = context.MyPosition;
            var targetPos = target.Position;
            var dist = myPos.Distance(targetPos);
            var settings = BeastRoutineSettings.Instance.Combat;

            // Move closer if needed
            if (dist > settings.MaxMeleeRange)
            {
                PlayerMoverManager.Current.MoveTowards(targetPos);
                return LogicResult.Provided;
            }

            // Attack
            var slot = settings.PrimarySkillSlot;
            LokiPoe.ProcessHookManager.ClearAllKeyStates();
            
            var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
                slot, settings.AlwaysAttackInPlace, targetPos);

            return LogicResult.Provided;
        }

        #endregion

        public void OnExit()
        {
            Log.InfoFormat("[ComprehensiveFarming] Session complete - Beasts: {0}, Caches: {1}, Enemies: {2}",
                _beastsCaptured, _cachesOpened, _enemiesKilled);
        }
    }

    #region Supporting Classes

    public class FarmingTarget
    {
        public FarmingTargetType Type { get; set; }
        public NetworkObject Object { get; set; }
        public Vector2i Position { get; set; }
        public float Priority { get; set; }
        public string Name { get; set; }
    }

    public enum FarmingTargetType
    {
        Beast,
        HeistCache,
        Enemy
    }

    public enum FarmingStep
    {
        ScanAndPlan,     // Scan for all targets and plan next action
        Combat,          // Fight immediate threats
        CaptureBeast,    // Capture or damage beast
        OpenCache,       // Open heist cache
        ApproachTarget,  // Move toward primary target
        LootItems,       // Pick up valuable items
        Explore          // Explore and clear map
    }

    #endregion
}
