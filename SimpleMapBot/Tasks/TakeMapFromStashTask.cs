using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using SimpleMapBot.Configuration;
using SimpleMapBot.Utilities;
using log4net;

namespace SimpleMapBot.Tasks
{
    public class TakeMapFromStashTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private bool _hasMap = false;

        public string Name => "TakeMapFromStashTask";
        public string Description => "Takes a suitable map from stash";
        public string Author => "SimpleMapBot";
        public string Version => "1.0.0";
        public bool IsEnabled => true;

        public async Task<bool> Run()
        {
            var settings = SimpleMapBotSettings.Instance;
            var cwa = LokiPoe.CurrentWorldArea;

            // Only run in hideout/town
            if (!cwa.IsHideoutArea && !cwa.IsTown)
                return false;

            // Check if we already have a map in inventory
            if (_hasMap || HasMapInInventory())
            {
                _hasMap = true;
                return false;
            }

            Log.Info("[TakeMapFromStashTask] Need to get a map from stash");

            // Open stash
            if (!await OpenStash())
            {
                ErrorManager.ReportError();
                return false;
            }

            // Find and take map
            if (await TakeMapFromStash())
            {
                _hasMap = true;

                // Close stash
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(200);

                ErrorManager.Reset(); // Reset on success
                return true;
            }

            Log.Warn("[TakeMapFromStashTask] No suitable maps found in stash!");
            ErrorManager.ReportError();
            return false;
        }

        private bool HasMapInInventory()
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
                return false;

            // Check for any map item
            return inventory.Items.Any(item =>
                item != null &&
                item.Class == "Maps");
        }

        private async Task<bool> OpenStash()
        {
            if (LokiPoe.InGameState.StashUi.IsOpened)
                return true;

            var stash = LokiPoe.ObjectManager.Stash;
            if (stash == null)
            {
                Log.Warn("[TakeMapFromStashTask] Stash not found");
                return false;
            }

            // Move to stash if needed
            if (stash.Distance > 30)
            {
                PlayerMoverManager.Current.MoveTowards(stash.Position);
                await Coroutine.Sleep(100);
                return false;
            }

            // Interact with stash
            if (!await Coroutines.InteractWith(stash))
            {
                Log.Warn("[TakeMapFromStashTask] Failed to interact with stash");
                return false;
            }

            // Wait for stash UI to open
            for (int i = 0; i < 30; i++)
            {
                if (LokiPoe.InGameState.StashUi.IsOpened)
                    return true;
                await Coroutine.Sleep(100);
            }

            Log.Warn("[TakeMapFromStashTask] Stash UI did not open");
            return false;
        }

        private async Task<bool> TakeMapFromStash()
        {
            var settings = SimpleMapBotSettings.Instance;

            // Switch to map stash tab
            if (!await SwitchToStashTab(settings.MapStashTab))
            {
                Log.WarnFormat("[TakeMapFromStashTask] Could not find stash tab: {0}", settings.MapStashTab);
                return false;
            }

            // Get current stash page items
            var stashItems = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Stash);
            if (stashItems == null)
            {
                Log.Warn("[TakeMapFromStashTask] Could not access stash items");
                return false;
            }

            // Find suitable map
            var suitableMap = stashItems.Items
                .Where(item => item != null && item.Class == "Maps")
                .Where(item => IsMapSuitable(item, settings))
                .OrderBy(item => item.MapTier)
                .FirstOrDefault();

            if (suitableMap == null)
            {
                Log.Warn("[TakeMapFromStashTask] No suitable maps found in stash tab");
                return false;
            }

            Log.InfoFormat("[TakeMapFromStashTask] Taking map: {0} (T{1}, {2})",
                suitableMap.Name, suitableMap.MapTier, suitableMap.Rarity);

            // Fast move from stash to inventory
            var result = LokiPoe.InGameState.StashUi.InventoryControl.FastMove(suitableMap.LocalId);
            if (result != FastMoveResult.None)
            {
                Log.WarnFormat("[TakeMapFromStashTask] Fast move failed: {0}", result);
                return false;
            }

            await Coroutine.Sleep(200);
            return true;
        }

        private async Task<bool> SwitchToStashTab(string tabName)
        {
            var stashUi = LokiPoe.InGameState.StashUi;
            if (!stashUi.IsOpened)
                return false;

            // Find tab by name
            int targetTabIndex = -1;
            for (int i = 0; i < stashUi.TabCount; i++)
            {
                var tab = stashUi.GetTabInfo(i);
                if (tab != null && tab.Name.Equals(tabName, StringComparison.OrdinalIgnoreCase))
                {
                    targetTabIndex = i;
                    break;
                }
            }

            if (targetTabIndex == -1)
                return false;

            // Switch to tab
            if (stashUi.CurrentTabIndex != targetTabIndex)
            {
                stashUi.SwitchToTab(targetTabIndex);
                await Coroutine.Sleep(300);
            }

            return stashUi.CurrentTabIndex == targetTabIndex;
        }

        private bool IsMapSuitable(Item map, SimpleMapBotSettings settings)
        {
            // Check tier
            if (map.MapTier < settings.MinMapTier || map.MapTier > settings.MaxMapTier)
                return false;

            // Check rarity
            switch (map.Rarity)
            {
                case Rarity.Normal:
                    return settings.RunWhiteMaps;
                case Rarity.Magic:
                    return settings.RunMagicMaps;
                case Rarity.Rare:
                    return settings.RunRareMaps;
                case Rarity.Unique:
                    return settings.RunUniqueMaps;
                default:
                    return false;
            }
        }

        #region Unused interface members
        public async Task<LogicResult> Logic(Logic logic) => LogicResult.Unprovided;
        public MessageResult Message(Message message) => MessageResult.Unprocessed;
        public void Start() { }
        public void Stop() { }
        public void Tick() { }
        #endregion
    }
}
