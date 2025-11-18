using System.Collections.Generic;
using System.Linq;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Beasts.Configuration;
using Beasts.Core;
using log4net;

namespace Beasts.Perception
{
    /// <summary>
    /// Detects combat threats and selects targets
    /// </summary>
    public class CombatPerception : IPerceptionModule
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "CombatPerception";

        public List<Monster> NearbyEnemies { get; private set; }
        public Monster BestTarget { get; private set; }

        private readonly string[] _aurasToIgnore = new[]
        {
            "shrine_godmode",           // Divine Shrine
            "bloodlines_invulnerable",  // Phylacteral Link
            "god_mode",                 // Animated Guardian
            "bloodlines_necrovigil"
        };

        public void Update()
        {
            var combatRange = BeastRoutineSettings.Instance.Combat.CombatRange;

            // Find all nearby hostile monsters
            NearbyEnemies = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Where(m => m != null &&
                           m.IsValid &&
                           ShouldInclude(m) &&
                           m.Distance < combatRange)
                .OrderByDescending(m => CalculateWeight(m))
                .ToList();

            BestTarget = NearbyEnemies.FirstOrDefault();
        }

        /// <summary>
        /// Determines if a monster should be considered as a target
        /// </summary>
        private bool ShouldInclude(Monster m)
        {
            // Must be active
            if (!m.IsActive)
                return false;

            // Skip dead/inactive
            if (!m.IsAliveHostile)
                return false;

            // Skip blacklisted
            if (Blacklist.Contains(m))
                return false;

            // Skip cannot die mobs
            if (m.CannotDie)
                return false;

            // Skip mobs with special auras
            if (m.HasAura(_aurasToIgnore))
                return false;

            // TODO: Add more exclusions based on specific mechanics

            return true;
        }

        /// <summary>
        /// Calculates targeting weight for a monster
        /// Higher weight = higher priority
        /// </summary>
        private float CalculateWeight(Monster m)
        {
            float weight = 0;

            // Proximity matters
            weight += (100 - m.Distance);

            // Rarity priority
            switch (m.Rarity)
            {
                case Rarity.Unique:
                    weight += 70;
                    break;
                case Rarity.Rare:
                    weight += 25;
                    break;
                case Rarity.Magic:
                    weight += 15;
                    break;
            }

            // Allies cannot die aura - kill these first!
            if (m.HasAura("monster_aura_cannot_die"))
                weight += 50;

            // Dangerous mobs (necromancers, etc.)
            if (m.ExplicitAffixes.Any(a => a.InternalName.Contains("RaisesUndead")) ||
                m.ImplicitAffixes.Any(a => a.InternalName.Contains("RaisesUndead")))
            {
                weight += 45;
            }

            return weight;
        }

        /// <summary>
        /// Checks if there are enemies near a specific position
        /// </summary>
        public bool AreEnemiesNear(Vector2i position, float range)
        {
            return NearbyEnemies.Any(m => m.Position.Distance(position) < range);
        }
    }
}
