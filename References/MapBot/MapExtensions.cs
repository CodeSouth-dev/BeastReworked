using System.Collections.Generic;
using System.Linq;
using SimpleBot.Extensions;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;

namespace SimpleBot.MapBot
{
    public static class MapExtensions
    {
        private static readonly GeneralSettings GeneralSettings = GeneralSettings.Instance;
        private static readonly Dictionary<string, MapData> MapDict = MapSettings.Instance.MapDict;
        private static readonly Dictionary<string, AffixData> AffixDict = AffixSettings.Instance.AffixDict;

        public static bool IsMap(this Item item)
        {
            return item.Class == ItemClasses.Map;
        }

        public static string CleanName(this Item map)
        {
            return map.FullName;
        }

        public static bool BelowTierLimit(this Item map)
        {
            return map.MapTier <= GeneralSettings.MaxMapTier;
        }

        public static int Priority(this Item map)
        {
            var cleanName = map.CleanName();

            if (!MapDict.TryGetValue(cleanName, out var data))
                return int.MinValue;

            var priority = data.Priority;

            return priority;
        }

        public static bool Ignored(this Item map)
        {
            return !MapDict.TryGetValue(map.CleanName(), out MapData data) || data.Ignored;
        }

        public static string GetBannedAffix(this Item map)
        {
            var rarity = map.RarityLite();

            if (rarity != Rarity.Magic && rarity != Rarity.Rare)
                return null;

            //var checkDoubleBoss = map.CleanName() == MapNames.Peninsula;

            foreach (var affix in map.ExplicitAffixes)
            {
                string affixName = affix.DisplayName;

                //if (checkDoubleBoss && affixName == "Twinned")
                //    return affixName;

                if (AffixDict.TryGetValue(affixName, out var data))
                {
                    if (rarity == Rarity.Magic)
                    {
                        if (data.RerollMagic)
                            return affixName;
                    }
                    else
                    {
                        if (data.RerollRare)
                            return affixName;
                    }
                }
                else
                {
                    GlobalLog.Debug($"[GetBannedAffix] Unknown map affix \"{affixName}\".");
                }
            }
            return null;
        }

        public static bool HasBannedAffix(this Item map)
        {
            return map.GetBannedAffix() != null;
        }

        public static bool CanAugment(this Item map)
        {
            return map.ExplicitAffixes.Count() < 2;
        }

        public static bool ShouldUpgrade(this Item map, Upgrade upgrade)
        {
            var tier = map.MapTier;

            if (upgrade.TierEnabled && tier >= upgrade.Tier)
                return true;

            if (upgrade.PriorityEnabled && map.Priority() >= upgrade.Priority)
                return true;

            return false;
        }

        public static bool ShouldSell(this Item map)
        {
            if (map.RarityLite() == Rarity.Unique)
                return false;

            if (GeneralSettings.SellIgnoredMaps && map.Ignored())
                return true;

            if (map.MapTier > GeneralSettings.MaxSellTier)
                return false;

            if (map.Priority() > GeneralSettings.MaxSellPriority)
                return false;

            return true;
        }

        public static bool IsSacrificeFragment(this Item item)
        {
            return item.Metadata.Contains("CurrencyVaalFragment1");
        }

        internal class AtlasData
        {
            private static readonly HashSet<string> BonusCompletedAreas = new HashSet<string>();
            private static readonly HashSet<string> ShaperInfluencedAreas = new HashSet<string>();
            private static readonly HashSet<string> ElderInfluencedAreas = new HashSet<string>();

            internal static bool IsCompleted(string name)
            {
                return BonusCompletedAreas.Contains(name);
            }

            internal static bool IsShaperInfluenced(string name)
            {
                return ShaperInfluencedAreas.Contains(name);
            }

            internal static bool IsElderInfluenced(string name)
            {
                return ElderInfluencedAreas.Contains(name);
            }

            internal static void Update()
            {
                BonusCompletedAreas.Clear();
                ShaperInfluencedAreas.Clear();
                ElderInfluencedAreas.Clear();

                //foreach (var area in LokiPoe.InstanceInfo.Atlas.BonusCompletedAreas)
                //{
                //    BonusCompletedAreas.Add(area.Name);
                //}
                //foreach (var area in LokiPoe.InstanceInfo.Atlas.ShaperInfluencedAreas)
                //{
                //    ShaperInfluencedAreas.Add(area.Name);
                //}
                //foreach (var area in LokiPoe.InstanceInfo.Atlas.ElderInfluencedAreas)
                //{
                //    ElderInfluencedAreas.Add(area.Name);
                //}
            }
        }
    }
}