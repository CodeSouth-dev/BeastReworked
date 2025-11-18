using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

namespace BeastCombatRoutine
{
    /// <summary>
    /// Combat routine for BeastRoutine with context-aware combat.
    /// ZOOM MODE (default): Only attacks near beasts/caches, zooms past regular enemies
    /// DISABLED: Fights all enemies (traditional combat)
    /// Features: Auto skill detection, flask management, intelligent targeting
    /// </summary>
    public class BeastCombatRoutine : IRoutine
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private int _leashRange = 40;
        private DateTime _lastSkillUse = DateTime.MinValue;
        private DateTime _lastBuffCheck = DateTime.MinValue;
        private DateTime _lastAuraCheck = DateTime.MinValue;
        private const int MIN_SKILL_DELAY_MS = 50; // Minimum delay between skill uses
        private const int BUFF_CHECK_INTERVAL_MS = 5000; // Check buffs every 5 seconds
        private const int AURA_CHECK_INTERVAL_MS = 2000; // Check auras every 2 seconds

        // Context awareness - track when we should engage in combat
        private bool _shouldEngage = false;
        private Vector2i _lastEngagePosition = Vector2i.Zero;

        // GUI
        private BeastCombatRoutineGui _gui;

        public string Name => "BeastCombatRoutine";
        public string Description => "Zoom Mode: Only fights near beasts/caches. Auto skill detection. Smart combat. Aura activation.";
        public string Author => "YourName";
        public string Version => "1.1.0";
        public JsonSettings Settings => BeastCombatRoutineSettings.Instance;
        public UserControl Control => _gui ?? (_gui = new BeastCombatRoutineGui());

        public void Initialize() { }
        public void Deinitialize() { }
        public void Start()
        {
            // Auto-detect skills if enabled
            var settings = BeastCombatRoutineSettings.Instance;
            if (settings.AutoDetectSkills)
            {
                AutoDetectSkills();
            }
        }
        public void Stop()
        {
            // Clear any held keys when stopping
            LokiPoe.ProcessHookManager.ClearAllKeyStates();
            _shouldEngage = false;
        }
        public void Tick()
        {
            // Check and activate auras periodically
            if (!LokiPoe.IsInGame)
                return;

            var settings = BeastCombatRoutineSettings.Instance;
            if (!settings.EnableAuraActivation)
                return;

            // Only activate auras in combat areas (not town/hideout)
            var cwa = LokiPoe.CurrentWorldArea;
            if (cwa.IsTown || cwa.IsHideoutArea)
                return;

            // Check auras every 2 seconds
            if ((DateTime.UtcNow - _lastAuraCheck).TotalMilliseconds > AURA_CHECK_INTERVAL_MS)
            {
                ActivateAuras();
                _lastAuraCheck = DateTime.UtcNow;
            }
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            // Handle combat hook from bot
            if (logic.Id == "hook_combat")
            {
                return await DoCombat();
            }

            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            // Handle leash range setting
            if (message.Id == "SetLeash")
            {
                _leashRange = message.GetInput<int>();
                return MessageResult.Processed;
            }

            // Handle combat engagement messages from BeastRoutine phases
            if (message.Id == "EnableCombat")
            {
                _shouldEngage = true;
                _lastEngagePosition = message.GetInput<Vector2i>();
                Log.InfoFormat("[BeastCombatRoutine] Combat ENABLED at {0}", _lastEngagePosition);
                return MessageResult.Processed;
            }

            if (message.Id == "DisableCombat")
            {
                _shouldEngage = false;
                Log.Info("[BeastCombatRoutine] Combat DISABLED - zoom mode");
                return MessageResult.Processed;
            }

            return MessageResult.Unprocessed;
        }

        private async Task<LogicResult> DoCombat()
        {
            var settings = BeastCombatRoutineSettings.Instance;
            
            // ZOOM MODE LOGIC:
            // - Enabled (true): Only fight when EnableCombat message received (near beasts/caches) - ZOOM PAST REGULAR MOBS
            // - Disabled (false): Always fight everything (traditional combat)
            if (settings.EnableZoomMode && !_shouldEngage)
            {
                // Zoom mode enabled - only fight near objectives, skip random enemies
                return LogicResult.Unprovided;
            }
            
            // Either zoom mode disabled (fight everything) OR we're near an objective (fight)

            // Auto-manage flasks during combat
            AutoManageFlasks();

            // Check and use buff skills periodically
            if ((DateTime.UtcNow - _lastBuffCheck).TotalMilliseconds > BUFF_CHECK_INTERVAL_MS)
            {
                await UseBuffSkills();
                _lastBuffCheck = DateTime.UtcNow;
            }

            // Respect minimum skill delay
            if ((DateTime.UtcNow - _lastSkillUse).TotalMilliseconds < MIN_SKILL_DELAY_MS)
            {
                return LogicResult.Unprovided;
            }

            // Get best target within leash range of engagement position
            var target = GetBestTarget();
            if (target == null)
                return LogicResult.Unprovided;

            // Get main attack skill slot based on distance and pack size
            var skillSlot = GetMainAttackSkillSlot(target);
            if (skillSlot == -1)
            {
                Log.Warn("[BeastCombatRoutine] No usable attack skill found on skillbar!");
                return LogicResult.Unprovided;
            }

            var skill = LokiPoe.InGameState.SkillBarHud.Slot(skillSlot);
            if (skill == null || !skill.CanUse())
            {
                return LogicResult.Unprovided;
            }

            // Use skill on target
            var targetPos = target.Position;
            var err = LokiPoe.InGameState.SkillBarHud.UseAt(skillSlot, settings.AlwaysAttackInPlace, targetPos);

            if (err == LokiPoe.InGameState.UseResult.None)
            {
                _lastSkillUse = DateTime.UtcNow;
            }
            else
            {
                Log.WarnFormat("[BeastCombatRoutine] Skill use failed: {0}", err);
            }

            return LogicResult.Provided;
        }

        private async Task UseBuffSkills()
        {
            var settings = BeastCombatRoutineSettings.Instance;
            var buffSlots = new[] { settings.BuffSlot1, settings.BuffSlot2, settings.BuffSlot3 };

            foreach (var slot in buffSlots)
            {
                if (slot == -1) continue;

                var skill = LokiPoe.InGameState.SkillBarHud.Slot(slot);
                if (skill == null) continue;

                // Check if buff is already active
                var buffName = skill.Name;
                if (LokiPoe.Me.Auras.Any(a => a.Name == buffName))
                    continue;

                // Try to use buff
                if (skill.CanUse())
                {
                    var err = LokiPoe.InGameState.SkillBarHud.Use(slot, false, false);
                    if (err == LokiPoe.InGameState.UseResult.None)
                    {
                        Log.InfoFormat("[BeastCombatRoutine] Activated buff: {0}", buffName);
                        await Coroutine.Sleep(100); // Brief delay between buffs
                    }
                }
            }
        }

        /// <summary>
        /// Activate auras (toggle skills like Righteous Fire, Aspects)
        /// These need to be reactivated when entering maps
        /// Note: Heralds are permanent in current PoE and don't need reactivation
        /// </summary>
        private void ActivateAuras()
        {
            var settings = BeastCombatRoutineSettings.Instance;
            var auraSlots = new[] { settings.AuraSlot1, settings.AuraSlot2, settings.AuraSlot3 };

            foreach (var slot in auraSlots)
            {
                if (slot == -1) continue;

                var skill = LokiPoe.InGameState.SkillBarHud.Slot(slot);
                if (skill == null) continue;

                // Check if aura is already active
                var auraName = skill.Name;
                if (LokiPoe.Me.Auras.Any(a => a.Name == auraName))
                    continue;

                // Try to activate aura
                if (skill.CanUse())
                {
                    var err = LokiPoe.InGameState.SkillBarHud.Use(slot, false, false);
                    if (err == LokiPoe.InGameState.UseResult.None)
                    {
                        Log.InfoFormat("[BeastCombatRoutine] Activated aura: {0}", auraName);
                    }
                    else
                    {
                        Log.WarnFormat("[BeastCombatRoutine] Failed to activate aura {0}: {1}", auraName, err);
                    }
                }
            }
        }

        private Monster GetBestTarget()
        {
            var myPos = LokiPoe.MyPosition;

            // Only target enemies near the engagement position (beast/cache)
            var searchCenter = _shouldEngage && _lastEngagePosition != Vector2i.Zero
                ? _lastEngagePosition
                : myPos;

            // Find all valid monsters near engagement point
            var monsters = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Where(m => m.IsAliveHostile &&
                           m.Distance <= _leashRange &&
                           m.Position.Distance(searchCenter) <= _leashRange) // Must be near objective
                .OrderBy(m => m.Distance)
                .ToList();

            if (!monsters.Any())
                return null;

            // Prioritize rares and uniques (could be the beast!)
            var rare = monsters.FirstOrDefault(m => m.Rarity >= Rarity.Rare);
            if (rare != null)
                return rare;

            // Then any monster
            return monsters.FirstOrDefault();
        }

        private int GetMainAttackSkillSlot(Monster target)
        {
            var settings = BeastCombatRoutineSettings.Instance;
            var myPos = LokiPoe.MyPosition;
            var targetPos = target.Position;
            var distance = myPos.Distance(targetPos);

            // Count nearby enemies for AOE detection
            var nearbyEnemies = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Count(m => m.IsAliveHostile && m.Position.Distance(targetPos) <= 20);

            bool isPackTarget = nearbyEnemies >= settings.AoePackSize;
            bool isMeleeRange = distance <= settings.MaxMeleeRange;

            // Priority order:
            // 1. AOE skills if pack size met
            // 2. Single target skills
            // 3. Fallback

            int skillSlot = -1;

            if (isPackTarget)
            {
                // Try AOE skills
                if (isMeleeRange && settings.AoeMeleeSlot != -1)
                {
                    skillSlot = settings.AoeMeleeSlot;
                }
                else if (!isMeleeRange && distance <= settings.MaxRangeRange && settings.AoeRangedSlot != -1)
                {
                    skillSlot = settings.AoeRangedSlot;
                }
            }

            if (skillSlot == -1)
            {
                // Try single target skills
                if (isMeleeRange && settings.SingleTargetMeleeSlot != -1)
                {
                    skillSlot = settings.SingleTargetMeleeSlot;
                }
                else if (!isMeleeRange && distance <= settings.MaxRangeRange && settings.SingleTargetRangedSlot != -1)
                {
                    skillSlot = settings.SingleTargetRangedSlot;
                }
            }

            // Fallback
            if (skillSlot == -1 && settings.FallbackSlot != -1)
            {
                skillSlot = settings.FallbackSlot;
            }

            // Validate the skill exists and can be used
            if (skillSlot != -1)
            {
                var skill = LokiPoe.InGameState.SkillBarHud.Slot(skillSlot);
                if (skill != null && skill.CanUse())
                {
                    return skillSlot;
                }
            }

            // Last resort: try to find any usable attack skill
            var movementSkills = new[] { "dash", "flame dash", "frostblink", "lightning warp", "blink arrow",
                                         "leap slam", "whirling blades", "shield charge", "phase run",
                                         "withering step", "bodyswap" };

            foreach (var skill in LokiPoe.Me.AvailableSkills.Where(s => s.IsOnSkillBar))
            {
                var skillLower = skill.Name.ToLower();
                if (skill.CanUse() &&
                    !skillLower.Contains("portal") &&
                    !movementSkills.Any(ms => skillLower.Contains(ms)))
                {
                    return skill.Slot;
                }
            }

            return -1;
        }

        #region Skill Detection and Rotation

        /// <summary>
        /// Automatically detect and assign skills from the skillbar
        /// </summary>
        private void AutoDetectSkills()
        {
            try
            {
                Log.Info("[BeastCombatRoutine] Auto-detecting skills from skillbar...");
                
                var settings = BeastCombatRoutineSettings.Instance;
                var skills = LokiPoe.Me.AvailableSkills.Where(s => s.IsOnSkillBar).ToList();
                
                if (!skills.Any())
                {
                    Log.Warn("[BeastCombatRoutine] No skills found on skillbar!");
                    return;
                }

                // Movement skills to exclude
                var movementSkills = new[] { "dash", "flame dash", "frostblink", "lightning warp", "blink arrow",
                                             "leap slam", "whirling blades", "shield charge", "phase run",
                                             "withering step", "bodyswap" };

                // Aura/buff skills to detect
                var buffKeywords = new[] { "aura", "herald", "banner", "stance", "blood", "sand", "blessing", "rage" };

                // AOE skill indicators
                var aoeKeywords = new[] { "sweep", "cyclone", "cleave", "reave", "blade", "storm", "nova", 
                                          "cascade", "rain", "barrage", "split", "volley", "chain", "pierce" };

                var detectedBuffs = 0;
                var detectedAttacks = 0;

                foreach (var skill in skills)
                {
                    var skillName = skill.Name.ToLower();
                    var slot = skill.Slot;

                    // Skip movement skills
                    if (movementSkills.Any(ms => skillName.Contains(ms)))
                    {
                        Log.DebugFormat("[BeastCombatRoutine] Skipping movement skill: {0}", skill.Name);
                        continue;
                    }

                    // Skip portal and other utility
                    if (skillName.Contains("portal") || skillName.Contains("golem"))
                    {
                        Log.DebugFormat("[BeastCombatRoutine] Skipping utility skill: {0}", skill.Name);
                        continue;
                    }

                    // Detect buffs (assign to first available buff slot)
                    if (buffKeywords.Any(kw => skillName.Contains(kw)))
                    {
                        if (settings.BuffSlot1 == -1)
                        {
                            settings.BuffSlot1 = slot;
                            Log.InfoFormat("[BeastCombatRoutine] Detected BuffSlot1: {0} (Slot {1})", skill.Name, slot);
                            detectedBuffs++;
                        }
                        else if (settings.BuffSlot2 == -1)
                        {
                            settings.BuffSlot2 = slot;
                            Log.InfoFormat("[BeastCombatRoutine] Detected BuffSlot2: {0} (Slot {1})", skill.Name, slot);
                            detectedBuffs++;
                        }
                        else if (settings.BuffSlot3 == -1)
                        {
                            settings.BuffSlot3 = slot;
                            Log.InfoFormat("[BeastCombatRoutine] Detected BuffSlot3: {0} (Slot {1})", skill.Name, slot);
                            detectedBuffs++;
                        }
                        continue;
                    }

                    // Detect attack skills
                    bool isAoe = aoeKeywords.Any(kw => skillName.Contains(kw));
                    
                    // Determine if skill is melee or ranged based on name
                    bool isMelee = skillName.Contains("strike") || skillName.Contains("slam") || 
                                   skillName.Contains("melee") || skillName.Contains("sweep") ||
                                   skillName.Contains("cyclone") || skillName.Contains("cleave") ||
                                   skillName.Contains("reave") || skillName.Contains("lacerate");

                    // Assign to appropriate slot
                    if (isAoe && isMelee && settings.AoeMeleeSlot == -1)
                    {
                        settings.AoeMeleeSlot = slot;
                        Log.InfoFormat("[BeastCombatRoutine] Detected AoeMeleeSlot: {0} (Slot {1})", skill.Name, slot);
                        detectedAttacks++;
                    }
                    else if (isAoe && !isMelee && settings.AoeRangedSlot == -1)
                    {
                        settings.AoeRangedSlot = slot;
                        Log.InfoFormat("[BeastCombatRoutine] Detected AoeRangedSlot: {0} (Slot {1})", skill.Name, slot);
                        detectedAttacks++;
                    }
                    else if (!isAoe && isMelee && settings.SingleTargetMeleeSlot == -1)
                    {
                        settings.SingleTargetMeleeSlot = slot;
                        Log.InfoFormat("[BeastCombatRoutine] Detected SingleTargetMeleeSlot: {0} (Slot {1})", skill.Name, slot);
                        detectedAttacks++;
                    }
                    else if (!isAoe && !isMelee && settings.SingleTargetRangedSlot == -1)
                    {
                        settings.SingleTargetRangedSlot = slot;
                        Log.InfoFormat("[BeastCombatRoutine] Detected SingleTargetRangedSlot: {0} (Slot {1})", skill.Name, slot);
                        detectedAttacks++;
                    }
                    // Fallback - assign first unassigned attack skill
                    else if (settings.FallbackSlot == -1)
                    {
                        settings.FallbackSlot = slot;
                        Log.InfoFormat("[BeastCombatRoutine] Detected FallbackSlot: {0} (Slot {1})", skill.Name, slot);
                        detectedAttacks++;
                    }
                }

                Log.InfoFormat("[BeastCombatRoutine] Skill detection complete: {0} attack skills, {1} buff skills", 
                               detectedAttacks, detectedBuffs);

                // Save settings
                settings.Save();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("[BeastCombatRoutine] Error in AutoDetectSkills: {0}", ex);
            }
        }

        #endregion

        #region Flask Management

        /// <summary>
        /// Auto-manage flasks during combat based on current situation
        /// </summary>
        private void AutoManageFlasks()
        {
            try
            {
                var healthPercent = LokiPoe.Me.HealthPercent;
                var manaPercent = LokiPoe.Me.ManaPercent;
                
                // Count nearby enemies
                var nearbyEnemies = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                    .Count(m => m.IsAliveHostile && m.Distance <= 30);

                // Check if fighting a boss
                var isBoss = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                    .Any(m => m.IsAliveHostile && m.Distance <= 30 && m.Rarity >= Rarity.Rare);

                // Emergency life flask (instant if available)
                if (healthPercent < 35)
                {
                    UseLifeFlask(emergency: true);
                }
                // Regular life flask
                else if (healthPercent < 60)
                {
                    UseLifeFlask(emergency: false);
                }

                // Mana management
                if (manaPercent < 30)
                {
                    UseManaFlask(emergency: true);
                }
                else if (manaPercent < 50)
                {
                    UseManaFlask(emergency: false);
                }

                // Remove debuffs
                if (LokiPoe.Me.HasAura("cursed") || LokiPoe.Me.HasAura("frozen") || LokiPoe.Me.HasAura("shocked"))
                {
                    UseUtilityFlask("Kiara's Determination");
                }

                // Use defensive flasks when facing multiple enemies or boss
                if (nearbyEnemies >= 5 || isBoss)
                {
                    UseDefensiveFlasks();
                }

                // Use offensive flasks during combat
                if (nearbyEnemies > 0)
                {
                    UseOffensiveFlasks();
                }

                // Use quicksilver for movement (when not in heavy combat)
                if (nearbyEnemies < 3 && !LokiPoe.Me.HasAura("flask_utility_sprint"))
                {
                    UseUtilityFlask("Quicksilver Flask");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[BeastCombatRoutine] Error in auto-manage flasks", ex);
            }
        }

        private bool UseLifeFlask(bool emergency = false)
        {
            try
            {
                var flasks = LokiPoe.InGameState.QuickFlaskHud.InventoryControl.Inventory.Items;
                
                // Find best life flask
                Item bestFlask = null;
                if (emergency)
                {
                    // Prefer instant/panicked flasks in emergency
                    bestFlask = flasks.FirstOrDefault(f => f.CanUse && 
                        (f.Class == "Life Flask" || f.Class == "Hybrid Flask") &&
                        (f.IsInstantRecovery || f.FullName.StartsWith("Panicked")));
                }
                
                if (bestFlask == null)
                {
                    // Any life flask
                    bestFlask = flasks.FirstOrDefault(f => f.CanUse && 
                        (f.Class == "Life Flask" || f.Class == "Hybrid Flask"));
                }

                return UseFlask(bestFlask);
            }
            catch (Exception ex)
            {
                Log.Error("[BeastCombatRoutine] Error using life flask", ex);
                return false;
            }
        }

        private bool UseManaFlask(bool emergency = false)
        {
            try
            {
                var flasks = LokiPoe.InGameState.QuickFlaskHud.InventoryControl.Inventory.Items;
                
                // Find best mana flask
                Item bestFlask = null;
                if (emergency)
                {
                    bestFlask = flasks.FirstOrDefault(f => f.CanUse && 
                        (f.Class == "Mana Flask" || f.Class == "Hybrid Flask") &&
                        f.IsInstantRecovery);
                }
                
                if (bestFlask == null)
                {
                    bestFlask = flasks.FirstOrDefault(f => f.CanUse && 
                        (f.Class == "Mana Flask" || f.Class == "Hybrid Flask"));
                }

                return UseFlask(bestFlask);
            }
            catch (Exception ex)
            {
                Log.Error("[BeastCombatRoutine] Error using mana flask", ex);
                return false;
            }
        }

        private bool UseDefensiveFlasks()
        {
            var defensiveFlasks = new[] { "Granite Flask", "Jade Flask", "Basalt Flask", "Amethyst Flask" };
            var used = false;
            foreach (var flaskName in defensiveFlasks)
            {
                if (UseUtilityFlask(flaskName))
                    used = true;
            }
            return used;
        }

        private bool UseOffensiveFlasks()
        {
            var offensiveFlasks = new[] { "Diamond Flask", "Silver Flask", "Sulphur Flask" };
            var used = false;
            foreach (var flaskName in offensiveFlasks)
            {
                if (UseUtilityFlask(flaskName))
                    used = true;
            }
            return used;
        }

        private bool UseUtilityFlask(string flaskName)
        {
            try
            {
                var flask = LokiPoe.InGameState.QuickFlaskHud.InventoryControl.Inventory.Items
                    .FirstOrDefault(f => f.CanUse && (f.Name == flaskName || f.FullName == flaskName));

                return UseFlask(flask);
            }
            catch
            {
                return false;
            }
        }

        private bool UseFlask(Item flask)
        {
            if (flask == null || !flask.CanUse)
                return false;

            try
            {
                var slot = flask.LocationTopLeft.X + 1; // Flask slot is X position + 1
                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                if (LokiPoe.InGameState.QuickFlaskHud.UseFlaskInSlot(slot))
                {
                    Log.DebugFormat("[BeastCombatRoutine] Used flask in slot {0}", slot);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[BeastCombatRoutine] Error using flask", ex);
            }
            return false;
        }

        #endregion

        public override string ToString() => $"{Name}: {Description}";
    }
}
