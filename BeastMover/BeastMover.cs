using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using log4net;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using Logger = DreamPoeBot.Loki.Common.Logger;

namespace BeastMover
{
    /// <summary>
    /// Custom player mover for Beast bot with optimized pathfinding and basic movement
    /// </summary>
    public class BeastMover : IPlayerMover
    {
        public static readonly ILog Log = Logger.GetLoggerInstanceForType();

        #region Implementation of IAuthored
        public string Name => "BeastMover";
        public string Description => "A player mover for Beast bot with movement skill support.";
        public string Author => "Beast Team";
        public string Version => "1.4.0";
        #endregion

        private PathfindingCommand _cmd;
        private readonly Stopwatch _sw = new Stopwatch();
        private Vector2i _lastPosition = Vector2i.Zero;
        private int _stuckCount = 0;
        private readonly Stopwatch _stuckCheckStopwatch = Stopwatch.StartNew();
        private bool _casted = false;
        private static readonly List<Vector2i> BlacklistedLocations = new List<Vector2i>();

        #region Implementation of IBase
        public void Initialize()
        {
            Log.InfoFormat("[BeastMover] Initialized - Version {0}", Version);
        }

        public void Deinitialize()
        {
            Log.InfoFormat("[BeastMover] Deinitialized");
        }

        public void Enable()
        {
            Log.InfoFormat("[BeastMover] Enabled and ready for movement");
        }

        public void Disable()
        {
            Log.InfoFormat("[BeastMover] Disabled");
        }
        #endregion

        #region Implementation of IConfigurable
        public JsonSettings Settings
        {
            get { return BeastMoverSettings.Instance; }
        }

        public UserControl Control
        {
            get { return null; } // GUI disabled - use JSON settings
        }
        #endregion

        #region Implementation of ILogicProvider
        public Task<LogicResult> Logic(string type, params dynamic[] param)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }
        #endregion

        #region Implementation of IStartStopEvents
        public void Start()
        {
        }
        #endregion

        #region Implementation of ITickEvents
        public void Tick()
        {
            if (!LokiPoe.IsInGame)
                return;
        }
        #endregion

        #region Implementation of IMessageHandler
        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
        #endregion

        #region Implementation of IPlayerMover
        /// <summary>
        /// Get the current pathfinding command
        /// </summary>
        public PathfindingCommand CurrentCommand => _cmd;

        /// <summary>
        /// Stop all movement
        /// </summary>
        public void Stop()
        {
            _cmd = null;
            _sw.Stop();
        }

        /// <summary>
        /// Main movement method - moves character towards target position using movement skills when available
        /// </summary>
        public bool MoveTowards(Vector2i position, params dynamic[] user)
        {
            if (position == Vector2i.Zero)
            {
                Log.ErrorFormat("[BeastMover] Received 0,0 as position, returning false");
                return false;
            }

            var myPosition = LokiPoe.MyPosition;
            var distance = myPosition.Distance(position);

            // Already at destination
            if (distance < 5f)
            {
                return false;
            }

            // Check if target is a portal - avoid unless explicitly allowed
            if (IsTargetPortal(position) && !BeastMoverSettings.Instance.AllowPortalMovement)
            {
                Log.InfoFormat("[BeastMover] Avoiding portal movement (portal movement disabled)");
                return false;
            }

            // Check if we're stuck
            CheckStuck(myPosition);

            // Need to generate a new path if:
            // - No command yet
            // - Moving to a different position
            // - Path refresh interval elapsed
            // - Off course from current path
            // - In town (always regenerate)
            if (_casted ||
                _cmd == null ||
                _cmd.Path == null ||
                _cmd.EndPoint != position ||
                LokiPoe.CurrentWorldArea.IsTown ||
                (_sw.IsRunning && _sw.ElapsedMilliseconds > BeastMoverSettings.Instance.PathRefreshRate) ||
                _cmd.Path.Count <= 2 ||
                _cmd.Path.All(p => myPosition.Distance(p) > 10))
            {
                // Generate new path using ExilePather
                _cmd = new PathfindingCommand(myPosition, position, 3, false);

                if (!ExilePather.FindPath(ref _cmd))
                {
                    _sw.Restart();
                    Log.ErrorFormat("[BeastMover] Cannot path from {0} to {1} - PathExistsBetween: {2}, IsWalkable: {3}",
                        myPosition, position,
                        ExilePather.PathExistsBetween(myPosition, position, true),
                        ExilePather.IsWalkable(position));
                    return false;
                }

                _sw.Restart();
            }

            var cwa = LokiPoe.CurrentWorldArea;
            var specialMoveRange = 20;
            if (cwa.IsTown)
                specialMoveRange = 19;

            // Remove points we've already passed
            while (_cmd.Path.Count > 1)
            {
                if (BlacklistedLocations.Contains(_cmd.Path[0]) ||
                    ExilePather.PathDistance(_cmd.Path[0], myPosition) < specialMoveRange)
                {
                    _cmd.Path.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            // Get the next point to move to
            var point = _cmd.Path[0];

            // Add slight randomization to prevent getting stuck on walls
            point += new Vector2i(LokiPoe.Random.Next(-2, 3), LokiPoe.Random.Next(-2, 3));

            // Validate the point is walkable
            if (!ExilePather.IsWalkable(point))
            {
                BlacklistedLocations.Add(point);
                _cmd.Path.RemoveAt(0);
                if (_cmd.Path.Count == 0)
                {
                    _cmd = null; // Force path regeneration
                }
                return true;
            }

            // Check for objects on path
            var pathCheck = ExilePather.Raycast(myPosition, point, out var hitPoint);
            if (!pathCheck)
            {
                BlacklistedLocations.Add(point);
                BlacklistedLocations.Add(hitPoint);
            }

            // PRIORITIZE movement skills over basic movement (when available and appropriate)
            var moveSkillSlot = FindReadyMovementSkill();
            if (moveSkillSlot != -1 &&
                !cwa.IsTown &&
                !cwa.IsHideoutArea &&
                !cwa.IsMapRoom)
            {
                // Try to use the movement skill first (regardless of distance for better pathing)
                var skillUsed = TryUseMovementSkill(moveSkillSlot, point, myPosition);
                if (skillUsed)
                {
                    _casted = true;
                    Log.InfoFormat("[BeastMover] Used movement skill at distance {0:F1}m", myPosition.Distance(point));
                    return true;
                }
            }

            _casted = false;

            // Fall back to basic movement
            Log.InfoFormat("[BeastMover] Using basic movement at distance {0:F1}m", myPosition.Distance(point));
            return BasicMove(myPosition, point);
        }

        /// <summary>
        /// Find a ready movement skill on the skill bar
        /// </summary>
        private int FindReadyMovementSkill()
        {
            // List of movement skills to check (in priority order)
            var movementSkills = new[] {
                "Whirling Blades",
                "Frostblink",
                "Flame Dash",
                "Shield Charge",
                "Leap Slam",
                "Dash",
                "Lightning Warp",
                "Blink Arrow",
                "Phase Run",
                "Bodyswap"
            };

            // Check each skill if it's available and ready
            foreach (var skillName in movementSkills)
            {
                var skill = LokiPoe.Me.AvailableSkills.FirstOrDefault(s => 
                    s.Name == skillName && s.IsOnSkillBar);
                
                if (skill == null) continue;

                var slot = skill.Slot;
                var skillBarSkill = LokiPoe.InGameState.SkillBarHud.Slot(slot);
                
                // Check if skill can be used
                if (skillBarSkill != null &&
                    !skillBarSkill.IsOnCooldown &&
                    skillBarSkill.CanUse() &&
                    LokiPoe.Me.ManaPercent > 10) // Basic mana check
                {
                    return slot;
                }
            }

            return -1; // No movement skill ready
        }

        /// <summary>
        /// Try to use a movement skill
        /// </summary>
        private bool TryUseMovementSkill(int slot, Vector2i target, Vector2i myPosition)
        {
            var skillBarSkill = LokiPoe.InGameState.SkillBarHud.Slot(slot);
            if (skillBarSkill == null) return false;

            var skillName = skillBarSkill.Name;

            // Special handling for Phase Run - just activate it, then basic move
            if (skillName == "Phase Run")
            {
                if (LokiPoe.Me.Auras.All(x => x.Name != "Phase Run"))
                {
                    LokiPoe.InGameState.SkillBarHud.Use(slot, false, false);
                }
                return false; // Let basic move handle the actual movement
            }

            // Special handling for Dash
            if (skillName == "Dash")
            {
                LokiPoe.InGameState.SkillBarHud.Use(slot, false, false);
                return true;
            }

            // For other skills, use them at the target position
            // Check if we can raycast to the target
            if (!ExilePather.Raycast(myPosition, target, out _))
            {
                // Can't see target, don't use skill
                return false;
            }

            // Clear key states and use the skill
            LokiPoe.ProcessHookManager.ClearAllKeyStates();
            LokiPoe.InGameState.SkillBarHud.UseAt(slot, false, target);

            return true;
        }

        /// <summary>
        /// Check if the target position is near a portal object
        /// </summary>
        private bool IsTargetPortal(Vector2i position)
        {
            try
            {
                // Look for Portal objects within 10 units of the target position
                var portals = LokiPoe.ObjectManager.Objects
                    .OfType<Portal>()
                    .Where(p => p != null && p.IsValid && p.Position.Distance(position) < 10)
                    .ToList();

                return portals.Any();
            }
            catch (Exception ex)
            {
                Log.WarnFormat("[BeastMover] Error checking for portal: {0}", ex.Message);
                return false;
            }
        }



        /// <summary>
        /// Basic movement using default move skill
        /// </summary>
        private bool BasicMove(Vector2i myPosition, Vector2i point)
        {
            var move = LokiPoe.InGameState.SkillBarHud.LastBoundMoveSkill;
            if (move == null)
            {
                Log.ErrorFormat("[BeastMover] No move skill found! Please assign the default move skill to your skillbar (q,w,e,r,t)");
                BotManager.Stop();
                return false;
            }

            // Use the bound move skill - check if key is already pressed
            if ((LokiPoe.ProcessHookManager.GetKeyState(move.BoundKeys.Last()) & 0x8000) != 0 &&
                LokiPoe.Me.HasCurrentAction && LokiPoe.Me.CurrentAction.Skill != null && 
                LokiPoe.Me.CurrentAction.Skill.InternalId.Equals("Move"))
            {
                // Key already pressed - for short distances, use single UseAt
                if (myPosition.Distance(point) <= BeastMoverSettings.Instance.SingleUseDistance)
                {
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    LokiPoe.InGameState.SkillBarHud.UseAt(move.Slots.Last(), false, point);
                }
                else
                {
                    // For longer distances, just update mouse position for smooth movement
                    MouseManager.SetMousePos("BeastMover.MoveTowards", point, false);
                }
            }
            else
            {
                // Key not pressed - clear all keys and issue move command
                LokiPoe.ProcessHookManager.ClearAllKeyStates();
                if (myPosition.Distance(point) <= BeastMoverSettings.Instance.SingleUseDistance)
                {
                    // Short distance - single click
                    LokiPoe.InGameState.SkillBarHud.UseAt(move.Slots.Last(), false, point);
                }
                else
                {
                    // Long distance - press and hold
                    LokiPoe.InGameState.SkillBarHud.BeginUseAt(move.Slots.Last(), false, point);
                }
            }
            
            return true;
        }

        /// <summary>
        /// Check if player is stuck and handle it
        /// </summary>
        private void CheckStuck(Vector2i currentPosition)
        {
            // Check every 2 seconds
            if (_stuckCheckStopwatch.ElapsedMilliseconds < 2000)
            {
                return;
            }

            _stuckCheckStopwatch.Restart();

            // If we haven't moved much, increment stuck counter
            var distanceMoved = _lastPosition.Distance(currentPosition);
            if (distanceMoved < BeastMoverSettings.Instance.StuckDistance && _lastPosition != Vector2i.Zero)
            {
                _stuckCount++;
                
                if (BeastMoverSettings.Instance.DebugLogging)
                {
                    Log.WarnFormat("[BeastMover] Stuck check #{0}: Only moved {1:F1}m in 2 seconds", 
                        _stuckCount, distanceMoved);
                }
                
                if (_stuckCount > BeastMoverSettings.Instance.StuckThreshold)
                {
                    Log.WarnFormat("[BeastMover] Player appears stuck at {0}, clearing path and regenerating", 
                        currentPosition);
                    _cmd = null; // Force new path generation
                    _stuckCount = 0;
                    BlacklistedLocations.Clear();
                }
            }
            else
            {
                if (_stuckCount > 0 && BeastMoverSettings.Instance.DebugLogging)
                {
                    Log.InfoFormat("[BeastMover] Movement recovered, moved {0:F1}m", distanceMoved);
                }
                _stuckCount = 0;
            }

            _lastPosition = currentPosition;
        }
        #endregion
    }
}
