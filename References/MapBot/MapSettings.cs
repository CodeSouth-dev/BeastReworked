using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimpleBot.Extensions;
using DreamPoeBot.Loki;
using Newtonsoft.Json;

namespace SimpleBot.MapBot
{
    public class MapSettings
    {
        private static readonly string SettingsPath = Path.Combine(Configuration.Instance.Path, "MapBot", "MapSettings.json");

        private static MapSettings _instance;
        public static MapSettings Instance => _instance ?? (_instance = new MapSettings());

        private MapSettings()
        {
            InitList();
            Load();
            InitDict();
            Configuration.OnSaveAll += (sender, args) => { Save(); };

            MapList = MapList.OrderByDescending(m => m.Priority).ToList();
        }

        public List<MapData> MapList { get; } = new List<MapData>();
        public Dictionary<string, MapData> MapDict { get; } = new Dictionary<string, MapData>();

        private void InitList()
        {
            MapList.Add(new MapData(MapNames.Confluence, 1, MapType.Bossroom));
        }

        private void InitDict()
        {
            foreach (var data in MapList)
            {
                MapDict.Add(data.Name, data);
            }
        }

        private void Load()
        {
            if (!File.Exists(SettingsPath))
                return;

            var json = File.ReadAllText(SettingsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                GlobalLog.Error("[MapBot] Fail to load \"MapSettings.json\". File is empty.");
                return;
            }
            var parts = JsonConvert.DeserializeObject<Dictionary<string, EditablePart>>(json);
            if (parts == null)
            {
                GlobalLog.Error("[MapBot] Fail to load \"MapSettings.json\". Json deserealizer returned null.");
                return;
            }
            foreach (var data in MapList)
            {
                if (parts.TryGetValue(data.Name, out var part))
                {
                    data.Priority = part.Priority;
                    data.Ignored = part.Ignore;
                    data.IgnoredBossroom = part.IgnoreBossroom;
                    data.MobRemaining = part.MobRemaining;
                    data.StrictMobRemaining = part.StrictMobRemaining;
                    data.ExplorationPercent = part.ExplorationPercent;
                    data.StrictExplorationPercent = part.StrictExplorationPercent;
                    data.TrackMob = part.TrackMob;
                    data.FastTransition = part.FastTransition;
                }
            }
        }

        private void Save()
        {
            var parts = new Dictionary<string, EditablePart>(MapList.Count);

            foreach (var map in MapList)
            {
                var part = new EditablePart
                {
                    Priority = map.Priority,
                    Ignore = map.Ignored,
                    IgnoreBossroom = map.IgnoredBossroom,
                    MobRemaining = map.MobRemaining,
                    StrictMobRemaining = map.StrictMobRemaining,
                    ExplorationPercent = map.ExplorationPercent,
                    StrictExplorationPercent = map.StrictExplorationPercent,
                    TrackMob = map.TrackMob,
                    FastTransition = map.FastTransition
                };
                parts.Add(map.Name, part);
            }
            var json = JsonConvert.SerializeObject(parts, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }

        private class EditablePart
        {
            public int Priority;
            public bool Ignore;
            public bool IgnoreBossroom;
            public bool Sextant;
            public int ZanaMod;
            public int MobRemaining;
            public bool StrictMobRemaining;
            public int ExplorationPercent;
            public bool StrictExplorationPercent;
            public bool? TrackMob;
            public bool? FastTransition;
        }
    }
}