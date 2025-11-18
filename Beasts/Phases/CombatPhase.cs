using System;
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
using Beasts.Services;
using log4net;

namespace Beasts.Phases
{
    /// <summary>
    /// Combat phase: Handles normal combat encounters while exploring
    /// OPTIMIZED: High-performance combat for fast attack speed and movement speed builds
    /// </summary>
    public class CombatPhase : IPhase
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "Combat";

        // Performance tracking
        private int _attackCounter = 0;
        private DateTime _lastAttackTime = DateTime.MinValue;
        private DateTime _lastMovementSkillTime = DateTime.MinValue;
        private Vector2i _lastRepositionTarget = Vector2i.Zero;

        public bool CanExecute(GameContext context)
        {
            // ZOOM MODE: Only fight when near mechanics (beasts/caches)
            // Random encounters during exploration are ignored
            return context.Combat.AnyEnemiesNearby && context.Mechanics.HasTarget;
        }

        public async Task<PhaseResult> Execute(GameContext context)
        {
            var target = context.Combat.BestTarget;
            if (target == null)
            {
                _attackCounter = 0;
                return PhaseResult.InProgress("No combat target");
            }

            // Enable combat around current mechanic
            var mechanicPos = context.Mechanics.BestBeast?.Position ?? context.Mechanics.BestCache?.Position ?? context.MyPosition;
            var msg = new Message("EnableCombat", this, mechanicPos);
            RoutineManager.Current.Message(msg);

            var myPos = context.MyPosition;
            var targetPos = target.Position;
            var dist = myPos.Distance(targetPos);

            var settings = BeastRoutineSettings.Instance.Combat;

            // OPTIMIZATION 1: Fast Combat Mode - Check if we should reposition
            bool shouldReposition = settings.AttackWhileMoving &&
                                   _attackCounter >= settings.MaxAttacksBeforeMove;

            if (shouldReposition)
            {
                await RepositionForCombat(context, targetPos, settings);
                _attackCounter = 0;
                return PhaseResult.InProgress($"Repositioning around {target.Name}");
            }

            // OPTIMIZATION 2: Movement Skill Integration - Use dash skill for long distances
            if (settings.UseMovementSkill &&
                dist > settings.MinMovementSkillDistance &&
                dist > settings.MaxMeleeRange)
            {
                var moveSkillUsed = await UseMovementSkill(targetPos, settings);
                if (moveSkillUsed)
                {
                    Log.DebugFormat("[Combat] Used movement skill to approach target (distance: {0:F1})", dist);
                    await Coroutine.Sleep(100); // Brief pause after dash
                    return PhaseResult.InProgress($"Dashing to {target.Name}");
                }
            }

            // Standard movement if out of range
            if (dist > settings.MaxMeleeRange)
            {
                PlayerMoverManager.Current.MoveTowards(targetPos);
                return PhaseResult.InProgress($"Approaching {target.Name} (Distance: {dist:F1})");
            }

            // OPTIMIZATION 3: Flask Usage in Combat
            if (settings.UseFlasksInCombat)
            {
                await UseFlasksIfNeeded(context, settings);
            }

            // OPTIMIZATION 4: Fast Attack Loop with minimal overhead
            if (settings.EnableFastCombatMode)
            {
                await ExecuteFastCombat(target, targetPos, settings);
            }
            else
            {
                // Legacy single-attack mode
                await ExecuteStandardCombat(target, targetPos, settings);
            }

            _attackCounter++;
            return PhaseResult.InProgress($"Fighting {target.Name} ({_attackCounter} attacks)");
        }

        /// <summary>
        /// OPTIMIZATION: Fast combat mode with minimal overhead and attack queueing
        /// </summary>
        private async Task ExecuteFastCombat(Monster target, Vector2i targetPos, CombatSettings settings)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            var now = DateTime.UtcNow;

            // Respect minimum attack delay to avoid spamming
            if (settings.MinAttackDelayMs > 0)
            {
                var timeSinceLastAttack = (now - _lastAttackTime).TotalMilliseconds;
                if (timeSinceLastAttack < settings.MinAttackDelayMs)
                {
                    return; // Skip this attack to maintain timing
                }
            }

            _lastAttackTime = now;

            var slot = settings.PrimarySkillSlot;

            // OPTIMIZATION 5: Hold Attack Button mode for continuous high-APS builds
            if (settings.HoldAttackButton)
            {
                // Use BeginUseAt which holds the skill down
                var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
                    slot,
                    settings.AlwaysAttackInPlace,
                    targetPos);

                if (useResult != LokiPoe.InGameState.UseResult.None)
                {
                    Log.WarnFormat("[Combat] Fast attack failed: {0}", useResult);
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                }
            }
            else
            {
                // Standard attack mode (release after each attack)
                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
                    slot,
                    settings.AlwaysAttackInPlace,
                    targetPos);

                if (useResult != LokiPoe.InGameState.UseResult.None)
                {
                    Log.WarnFormat("[Combat] Attack failed: {0}", useResult);
                }
            }
        }

        /// <summary>
        /// Legacy combat mode (single attack per tick)
        /// </summary>
        private async Task ExecuteStandardCombat(Monster target, Vector2i targetPos, CombatSettings settings)
        {
            var slot = settings.PrimarySkillSlot;
            var skill = LokiPoe.InGameState.SkillBarHud.Slot(slot);

            if (skill == null)
            {
                Log.Error("[Combat] No skill configured in primary slot");
                return;
            }

            // Clear any stuck keys
            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            // Attack
            var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
                slot,
                settings.AlwaysAttackInPlace,
                targetPos);

            if (useResult != LokiPoe.InGameState.UseResult.None)
            {
                Log.WarnFormat("[Combat] Skill use failed: {0}", useResult);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// OPTIMIZATION: Use movement skill (Whirling Blades, Shield Charge, etc.) for repositioning
        /// Supports both primary and secondary movement skills with delay tracking
        /// </summary>
        private async Task<bool> UseMovementSkill(Vector2i targetPos, CombatSettings settings)
        {
            await Task.CompletedTask; // Method needs to be async for consistency
            var now = DateTime.UtcNow;

            // Check movement skill delay
            if (settings.MinMovementSkillDelayMs > 0)
            {
                var timeSinceLastMovement = (now - _lastMovementSkillTime).TotalMilliseconds;
                if (timeSinceLastMovement < settings.MinMovementSkillDelayMs)
                {
                    return false; // Skip to avoid spamming movement skills
                }
            }

            // Try primary movement skill first
            var primarySlot = settings.MovementSkillSlot;
            var primarySkill = LokiPoe.InGameState.SkillBarHud.Slot(primarySlot);

            if (primarySkill != null && primarySkill.CanUse())
            {
                var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
                    primarySlot,
                    false, // Don't attack in place for movement skills
                    targetPos);

                if (useResult == LokiPoe.InGameState.UseResult.None)
                {
                    _lastMovementSkillTime = now;
                    Log.DebugFormat("[Combat] Used primary movement skill (slot {0})", primarySlot);
                    return true;
                }
            }

            // Try secondary movement skill if primary failed or unavailable
            if (settings.SecondaryMovementSkillSlot > 0)
            {
                var secondarySlot = settings.SecondaryMovementSkillSlot;
                var secondarySkill = LokiPoe.InGameState.SkillBarHud.Slot(secondarySlot);

                if (secondarySkill != null && secondarySkill.CanUse())
                {
                    var useResult = LokiPoe.InGameState.SkillBarHud.BeginUseAt(
                        secondarySlot,
                        false,
                        targetPos);

                    if (useResult == LokiPoe.InGameState.UseResult.None)
                    {
                        _lastMovementSkillTime = now;
                        Log.DebugFormat("[Combat] Used secondary movement skill (slot {0})", secondarySlot);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// OPTIMIZATION: Attack-move pattern for mobile combat
        /// </summary>
        private async Task RepositionForCombat(GameContext context, Vector2i targetPos, CombatSettings settings)
        {
            // Calculate a position slightly offset from target for kiting/repositioning
            var myPos = context.MyPosition;
            var angle = Math.Atan2(targetPos.Y - myPos.Y, targetPos.X - myPos.X);

            // Move perpendicular to target for circle-strafing effect
            var perpendicularAngle = angle + (Math.PI / 2);
            var repositionDist = 15; // Move 15 units perpendicular

            var newX = (int)(myPos.X + Math.Cos(perpendicularAngle) * repositionDist);
            var newY = (int)(myPos.Y + Math.Sin(perpendicularAngle) * repositionDist);
            var repositionPos = new Vector2i(newX, newY);

            // Try movement skill first
            if (settings.UseMovementSkill)
            {
                var moved = await UseMovementSkill(repositionPos, settings);
                if (moved)
                {
                    Log.Debug("[Combat] Repositioned using movement skill");
                    _lastRepositionTarget = repositionPos;
                    return;
                }
            }

            // Fallback to walking
            PlayerMoverManager.Current.MoveTowards(repositionPos);
            await Coroutine.Sleep(200); // Brief pause to allow movement
            _lastRepositionTarget = repositionPos;
        }

        /// <summary>
        /// Flask management is now handled by BeastCombatRoutine automatically
        /// This method is kept for compatibility but does nothing
        /// </summary>
        private async Task UseFlasksIfNeeded(GameContext context, CombatSettings settings)
        {
            // Note: Flask management is handled by BeastCombatRoutine's AutoManageFlasks()
            // No need to manually manage flasks here
            await Task.CompletedTask;
        }

        public void OnExit()
        {
            // Cleanup
            LokiPoe.ProcessHookManager.ClearAllKeyStates();
            _attackCounter = 0;
            _lastAttackTime = DateTime.MinValue;
            _lastMovementSkillTime = DateTime.MinValue;
            _lastRepositionTarget = Vector2i.Zero;

            // Disable combat when exiting combat phase
            var msg = new Message("DisableCombat", this);
            RoutineManager.Current.Message(msg);
        }
    }
}
