using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using SimpleMapBot.Configuration;
using SimpleMapBot.GUI;
using Beasts.Services;
using log4net;

namespace SimpleMapBot.Core
{
    /// <summary>
    /// Simple map running bot - handles map workflow, looting, and stashing
    /// Requires BeastMover for movement and BeastCombatRoutine for combat
    /// </summary>
    public class SimpleMapBot : IBot
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private SimpleMapBotGui _gui;

        // Coroutine for bot loop
        private Coroutine _coroutine;

        // State tracking
        private MapBotState _currentState = MapBotState.Idle;
        private int _tickCount = 0;
        private int _stateAttempts = 0;

        // Map timer
        private readonly Stopwatch _mapTimer = new Stopwatch();
        private bool _mapTimerStarted = false;

        #region IAuthored
        public string Name => "SimpleMapBot";
        public string Description => "Simple map running bot - stash to map device workflow, looting, stashing, boss seeking";
        public string Author => "BeastReworked";
        public string Version => "1.1.0";
        #endregion

        #region IBase
        public void Initialize()
        {
            Log.Info("[SimpleMapBot] Initialize called");
        }

        public void Deinitialize()
        {
            Log.Info("[SimpleMapBot] Deinitialize called");
        }

        public void Enable()
        {
            Log.Info("[SimpleMapBot] Enable called");
        }

        public void Disable()
        {
            Log.Info("[SimpleMapBot] Disable called");
        }
        #endregion

        #region IConfigurable
        public JsonSettings Settings => SimpleMapBotSettings.Instance;
        public UserControl Control => _gui ?? (_gui = new SimpleMapBotGui());
        #endregion

        #region ILogicProvider
        public async Task<LogicResult> Logic(Logic logic)
        {
            await Task.CompletedTask;
            return LogicResult.Unprovided;
        }
        #endregion

        #region IMessageHandler
        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
        #endregion

        #region IStartStopEvents
        public void Start()
        {
            Log.Info("=====================================================");
            Log.Info("[SimpleMapBot] Starting SimpleMapBot v1.1.0");
            Log.Info("=====================================================");
            Log.InfoFormat("[SimpleMapBot] Boss-seeking threshold: {0} monsters remaining",
                SimpleMapBotSettings.Instance.MonstersRemainingThreshold);
            Log.InfoFormat("[SimpleMapBot] Max time per map: {0} seconds ({1} minutes)",
                SimpleMapBotSettings.Instance.MaxMapTimeSeconds,
                SimpleMapBotSettings.Instance.MaxMapTimeSeconds / 60);

            // Enable ProcessHookManager for client actions (CRITICAL!)
            LokiPoe.ProcessHookManager.Enable();
            Log.Info("[SimpleMapBot] ProcessHookManager enabled");

            // Cache all bound keys
            LokiPoe.Input.Binding.Update();
            Log.InfoFormat("[SimpleMapBot] KeyPickup: {0}", LokiPoe.ConfigManager.KeyPickup);

            // Check required components
            var currentMover = PlayerMoverManager.Current;
            var currentRoutine = RoutineManager.Current;

            Log.InfoFormat("[SimpleMapBot] PlayerMover: {0}", currentMover?.Name ?? "NONE - WARNING!");
            Log.InfoFormat("[SimpleMapBot] Routine: {0}", currentRoutine?.Name ?? "NONE - WARNING!");

            if (currentMover == null)
            {
                Log.Error("[SimpleMapBot] ===== NO PLAYERMOVER SELECTED =====");
                Log.Error("[SimpleMapBot] Please select BeastMover in the Bot Settings!");
            }
            else if (currentMover.Name != "BeastMover")
            {
                Log.WarnFormat("[SimpleMapBot] PlayerMover is '{0}' - BeastMover is recommended", currentMover.Name);
            }

            if (currentRoutine == null)
            {
                Log.Error("[SimpleMapBot] ===== NO ROUTINE SELECTED =====");
                Log.Error("[SimpleMapBot] Please select BeastCombatRoutine in the Bot Settings!");
            }
            else if (currentRoutine.Name != "BeastCombatRoutine")
            {
                Log.WarnFormat("[SimpleMapBot] Routine is '{0}' - BeastCombatRoutine is recommended", currentRoutine.Name);
            }

            // Log enabled maps
            var enabledMaps = SimpleMapBotSettings.Instance.GetEnabledMaps();
            if (enabledMaps.Count > 0)
            {
                Log.InfoFormat("[SimpleMapBot] Enabled maps ({0}): {1}", enabledMaps.Count, string.Join(", ", enabledMaps));
            }
            else
            {
                Log.Error("[SimpleMapBot] ===== NO MAPS ENABLED =====");
                Log.Error("[SimpleMapBot] Please configure map selection in the GUI!");
            }

            // Log selected scarabs
            var selectedScarabs = SimpleMapBotSettings.Instance.GetSelectedScarabs();
            if (selectedScarabs.Count > 0)
            {
                Log.InfoFormat("[SimpleMapBot] Selected scarabs ({0}): {1}", selectedScarabs.Count, string.Join(", ", selectedScarabs));
            }
            else
            {
                Log.Info("[SimpleMapBot] Running with 0 scarabs (none configured)");
            }

            // Initialize PoeNinja service if filtering is enabled
            if (SimpleMapBotSettings.Instance.UsePoeNinjaFiltering)
            {
                var league = SimpleMapBotSettings.Instance.PoeNinjaLeague;
                Log.InfoFormat("[SimpleMapBot] Initializing poe.ninja price service for league: {0}", league);
                SimpleMapBot.Services.PoeNinjaService.Initialize(league);
            }
            else
            {
                Log.Info("[SimpleMapBot] poe.ninja filtering disabled");
            }

            // Reset coroutine
            _coroutine = null;

            Log.Info("=====================================================");
            Log.Info("[SimpleMapBot] Startup complete - ready to run maps!");
            Log.Info("[SimpleMapBot] Workflow: Stash \u2192 Map Device \u2192 Portal \u2192 Loot \u2192 Stash");
            Log.Info("=====================================================");
        }

        public void Stop()
        {
            Log.Info("[SimpleMapBot] Bot stopped!");

            // Disable ProcessHookManager
            LokiPoe.ProcessHookManager.Disable();
            Log.Info("[SimpleMapBot] ProcessHookManager disabled");

            _currentState = MapBotState.Idle;
            _stateAttempts = 0;

            // Cleanup coroutine
            if (_coroutine != null)
            {
                _coroutine.Dispose();
                _coroutine = null;
            }
        }
        #endregion

        #region ITickEvents
        public void Tick()
        {
            // Initialize coroutine if needed
            if (_coroutine == null)
            {
                _coroutine = new Coroutine(() => MainCoroutine());
            }

            // Check if coroutine finished
            if (_coroutine.IsFinished)
            {
                Log.Debug("[SimpleMapBot] Coroutine finished");
                BotManager.Stop();
                return;
            }

            // Execute coroutine
            try
            {
                _coroutine.Resume();
            }
            catch
            {
                var c = _coroutine;
                _coroutine = null;
                c.Dispose();
                throw;
            }
        }
        #endregion

        #region Main Coroutine
        private async Task MainCoroutine()
        {
            Log.Info("[SimpleMapBot] *** MainCoroutine started - bot is running! ***");

            while (true)
            {
                _tickCount++;

                // Wait for game
                if (!LokiPoe.IsInGame)
                {
                    if (_tickCount % 200 == 0)
                    {
                        Log.Info("[SimpleMapBot] Waiting for game...");
                    }
                    await Coroutine.Sleep(100);
                    continue;
                }

                var cwa = LokiPoe.CurrentWorldArea;

                // Log status every 3 seconds
                if (_tickCount % 100 == 0)
                {
                    Log.InfoFormat("[SimpleMapBot] State: {0}, Area: {1}, Tick: {2}",
                        _currentState, cwa?.Name ?? "Unknown", _tickCount);
                }

                // State machine
                if (cwa != null && cwa.IsHideoutArea)
                {
                    await HandleHideoutState();
                }
                else if (cwa != null && !cwa.IsTown)
                {
                    await HandleMapState();
                }
                else if (cwa != null && cwa.IsTown)
                {
                    if (_tickCount % 100 == 0)
                    {
                        Log.Info("[SimpleMapBot] In town - go to hideout to run maps");
                    }
                }

                await Coroutine.Sleep(30); // Small delay between iterations
            }
        }

        private async Task HandleHideoutState()
        {
            switch (_currentState)
            {
                case MapBotState.Idle:
                case MapBotState.NeedMap:
                    await WithdrawMapFromStash();
                    break;

                case MapBotState.HasMap:
                    await PlaceMapInDevice();
                    break;

                case MapBotState.MapInDevice:
                    await PlaceScarabsInDevice();
                    break;

                case MapBotState.ReadyToActivate:
                    await ActivateMapDevice();
                    break;

                case MapBotState.WaitingForPortal:
                    await EnterPortal();
                    break;

                case MapBotState.NeedToStash:
                    await StashItems();
                    break;
            }
        }

        private async Task HandleMapState()
        {
            // Start map timer on first entry
            if (!_mapTimerStarted)
            {
                _mapTimer.Restart();
                _mapTimerStarted = true;
                Log.InfoFormat("[SimpleMapBot] Map timer started - {0} second limit",
                    SimpleMapBotSettings.Instance.MaxMapTimeSeconds);
            }

            // Check time limit
            if (_mapTimer.Elapsed.TotalSeconds >= SimpleMapBotSettings.Instance.MaxMapTimeSeconds)
            {
                Log.InfoFormat("[SimpleMapBot] ===== TIME LIMIT REACHED ({0:F0}s / {1}s) =====",
                    _mapTimer.Elapsed.TotalSeconds,
                    SimpleMapBotSettings.Instance.MaxMapTimeSeconds);
                Log.Info("[SimpleMapBot] Exiting map due to time limit");
                await ReturnToHideout();
                _currentState = MapBotState.NeedToStash;
                _mapTimer.Reset();
                _mapTimerStarted = false;
                return;
            }

            // Check map completion
            int monstersRemaining = LokiPoe.InstanceInfo.MonstersRemaining;

            // Log monsters remaining and time periodically
            if (_tickCount % 100 == 0 && monstersRemaining >= 0)
            {
                Log.InfoFormat("[SimpleMapBot] Monsters remaining: {0} | Time: {1:F0}s / {2}s",
                    monstersRemaining,
                    _mapTimer.Elapsed.TotalSeconds,
                    SimpleMapBotSettings.Instance.MaxMapTimeSeconds);
            }

            // Map complete - boss killed (MonstersRemaining = 0)
            if (monstersRemaining == 0)
            {
                Log.InfoFormat("[SimpleMapBot] ===== MAP COMPLETE (Boss Killed) - Time: {0:F0}s =====",
                    _mapTimer.Elapsed.TotalSeconds);
                Log.Info("[SimpleMapBot] Returning to hideout for next map");
                await ReturnToHideout();
                _currentState = MapBotState.NeedToStash;
                _mapTimer.Reset();
                _mapTimerStarted = false;
                return;
            }

            // Few monsters left - seek boss
            if (monstersRemaining > 0 && monstersRemaining <= SimpleMapBotSettings.Instance.MonstersRemainingThreshold)
            {
                Log.InfoFormat("[SimpleMapBot] Only {0} monsters left - seeking boss", monstersRemaining);
                await SeekBoss();
                return;
            }

            // Inventory full - return to stash
            if (IsInventoryFull())
            {
                Log.InfoFormat("[SimpleMapBot] Inventory full, returning to hideout - Time: {0:F0}s",
                    _mapTimer.Elapsed.TotalSeconds);
                await ReturnToHideout();
                _currentState = MapBotState.NeedToStash;
                _mapTimer.Reset();
                _mapTimerStarted = false;
                return;
            }

            // Normal mapping - loot items
            await TryLootNearbyItems();
        }

        private async Task SeekBoss()
        {
            // Look for boss monster
            var boss = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Where(m => m != null && m.IsValid && m.IsAliveHostile)
                .Where(m => m.Rarity == Rarity.Unique || m.IsBoss)
                .OrderBy(m => m.Distance)
                .FirstOrDefault();

            if (boss != null)
            {
                if (_tickCount % 50 == 0)
                {
                    Log.InfoFormat("[SimpleMapBot] Boss found: {0} at {1:F1}m", boss.Name, boss.Distance);
                }

                // Move towards boss (let BeastCombatRoutine handle the fighting)
                if (boss.Distance > 40f)
                {
                    if (!SafeMove(boss.Position))
                        return;
                    await Coroutine.Sleep(100);
                }
                return;
            }

            // No boss visible - look for boss arena entrance or unexplored areas
            var bossArena = FindBossArenaEntrance();
            if (bossArena != null)
            {
                if (_tickCount % 50 == 0)
                {
                    Log.InfoFormat("[SimpleMapBot] Boss arena detected: {0} at {1:F1}m", bossArena.Name, bossArena.Distance);
                }

                if (bossArena.Distance > 20f)
                {
                    if (!SafeMove(bossArena.Position))
                        return;
                    await Coroutine.Sleep(100);
                }
                else
                {
                    // Try to interact with arena entrance
                    await Coroutines.InteractWith(bossArena);
                    await Coroutine.Sleep(500);
                }
                return;
            }

            // No boss or arena found - explore map to find boss
            if (_tickCount % 100 == 0)
            {
                Log.Debug("[SimpleMapBot] Boss not found, exploring to find boss area");
            }

            // Pick random exploration direction
            var myPos = LokiPoe.MyPosition;
            var angle = LokiPoe.Random.Next(0, 360);
            var distance = 40;
            var x = (int)(distance * Math.Cos(angle * Math.PI / 180));
            var y = (int)(distance * Math.Sin(angle * Math.PI / 180));
            var target = myPos + new Vector2i(x, y);

            if (!SafeMove(target))
                return;
            await Coroutine.Sleep(100);
        }

        private NetworkObject FindBossArenaEntrance()
        {
            // Look for common boss arena indicators
            var candidates = LokiPoe.ObjectManager.Objects
                .Where(obj => obj != null && obj.IsValid && obj.Distance < 150f)
                .Where(obj =>
                {
                    if (obj.Name == null && obj.Metadata == null)
                        return false;

                    string name = obj.Name?.ToLower() ?? "";
                    string metadata = obj.Metadata?.ToLower() ?? "";

                    // Common boss arena patterns
                    return name.Contains("arena") ||
                           name.Contains("boss") ||
                           (name.Contains("portal") && name.Contains("boss")) ||
                           metadata.Contains("bossportal") ||
                           metadata.Contains("bossarena") ||
                           metadata.Contains("questportal") ||
                           name.Contains("aspirant") ||
                           name.Contains("atziri") ||
                           name.Contains("shaper") ||
                           name.Contains("elder");
                })
                .OrderBy(obj => obj.Distance)
                .FirstOrDefault();

            return candidates;
        }

        private async Task WithdrawMapFromStash()
        {
            // Check if we already have an enabled map
            var mapInInventory = GetEnabledMapFromInventory();
            if (mapInInventory != null)
            {
                Log.InfoFormat("[SimpleMapBot] Already have map: {0}", mapInInventory.Name);
                _currentState = MapBotState.HasMap;
                _stateAttempts = 0;
                return;
            }

            // Find stash
            var stash = LokiPoe.ObjectManager.Stash;
            if (stash == null)
            {
                if (_tickCount % 100 == 0)
                {
                    Log.Warn("[SimpleMapBot] Stash not found in hideout");
                }
                return;
            }

            // Move to stash
            if (stash.Distance > 15f)
            {
                if (_tickCount % 50 == 0)
                {
                    Log.InfoFormat("[SimpleMapBot] Moving to stash ({0:F1}m)", stash.Distance);
                }
                if (!SafeMove(stash.Position))
                    return;
                await Coroutine.Sleep(100);
                return;
            }

            // Open stash
            if (!LokiPoe.InGameState.StashUi.IsOpened)
            {
                Log.Info("[SimpleMapBot] Opening stash to withdraw map");
                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                if (!await Coroutines.InteractWith(stash))
                {
                    _stateAttempts++;
                    if (_stateAttempts > 5)
                    {
                        Log.Error("[SimpleMapBot] Failed to open stash after 5 attempts");
                        _stateAttempts = 0;
                    }
                    return;
                }

                await Coroutine.Sleep(500);
                return;
            }

            // Search for map in stash
            var stashMap = await FindAndWithdrawMapFromStash();
            if (stashMap)
            {
                Log.Info("[SimpleMapBot] Map withdrawn from stash");

                // Close stash
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
                await Coroutine.Sleep(300);

                _currentState = MapBotState.HasMap;
                _stateAttempts = 0;
            }
            else
            {
                _stateAttempts++;
                if (_stateAttempts > 3)
                {
                    Log.Error("[SimpleMapBot] ===== OUT OF MAPS =====");
                    Log.Error("[SimpleMapBot] No enabled maps found in stash. Please restock!");
                    _stateAttempts = 0;
                }
            }
        }

        private async Task<bool> FindAndWithdrawMapFromStash()
        {
            var settings = SimpleMapBotSettings.Instance;
            var tabControl = LokiPoe.InGameState.StashUi.TabControl;

            if (tabControl == null)
                return false;

            // Search through stash tabs
            for (int i = 0; i < 50; i++) // Check up to 50 tabs
            {
                if (tabControl.CurrentTabIndex != i)
                {
                    tabControl.SwitchToTabMouse(i);
                    await Coroutine.Sleep(200);
                }

                var stashInventory = LokiPoe.InGameState.StashUi.InventoryControl?.Inventory;
                if (stashInventory == null)
                    continue;

                // Find enabled maps in this tab
                var maps = stashInventory.Items
                    .Where(item => item != null && item.IsValid &&
                                  item.Class == "Maps" &&
                                  settings.IsMapEnabled(item.Name))
                    .ToList();

                if (maps.Any())
                {
                    var map = maps.First();
                    Log.InfoFormat("[SimpleMapBot] Found map in stash tab {0}: {1}", i, map.Name);

                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    var result = LokiPoe.InGameState.StashUi.InventoryControl.FastMove(map.LocalId);

                    if (result == FastMoveResult.None)
                    {
                        await Coroutine.Sleep(200);
                        return true;
                    }
                }
            }

            return false;
        }

        private async Task PlaceMapInDevice()
        {
            // Check if map already in device
            if (LokiPoe.InGameState.MapDeviceUi.IsOpened)
            {
                var deviceInv = LokiPoe.InGameState.MapDeviceUi.InventoryControl?.Inventory;
                if (deviceInv?.Items?.Any(i => i != null && i.Class == "Maps") == true)
                {
                    Log.Info("[SimpleMapBot] Map already in device");
                    _currentState = MapBotState.MapInDevice;
                    _stateAttempts = 0;
                    return;
                }
            }

            // Open map device
            if (!await MapDeviceService.OpenMapDevice())
            {
                if (_tickCount % 50 == 0)
                {
                    Log.Debug("[SimpleMapBot] Opening map device...");
                }
                return;
            }

            // Get map from inventory
            var map = GetEnabledMapFromInventory();
            if (map == null)
            {
                Log.Warn("[SimpleMapBot] No map in inventory");
                _currentState = MapBotState.NeedMap;
                return;
            }

            // Place map
            Log.InfoFormat("[SimpleMapBot] Placing map: {0}", map.Name);
            if (await MapDeviceService.PlaceItemInDevice(map))
            {
                _currentState = MapBotState.MapInDevice;
                _stateAttempts = 0;
                await Coroutine.Sleep(300);
            }
            else
            {
                _stateAttempts++;
                if (_stateAttempts > 5)
                {
                    Log.Error("[SimpleMapBot] Failed to place map after 5 attempts");
                    _stateAttempts = 0;
                }
            }
        }

        private async Task PlaceScarabsInDevice()
        {
            var selectedScarabs = SimpleMapBotSettings.Instance.GetSelectedScarabs();

            if (selectedScarabs.Count == 0)
            {
                // No scarabs configured, skip to activation
                _currentState = MapBotState.ReadyToActivate;
                return;
            }

            // Find scarabs in inventory
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            var scarabsToPlace = new System.Collections.Generic.List<Item>();

            foreach (var scarabName in selectedScarabs)
            {
                var scarab = inventory?.Items.FirstOrDefault(i =>
                    i != null && i.Name != null && i.Name.Equals(scarabName, System.StringComparison.OrdinalIgnoreCase));

                if (scarab != null)
                    scarabsToPlace.Add(scarab);
            }

            if (scarabsToPlace.Count == 0)
            {
                Log.Warn("[SimpleMapBot] No scarabs found in inventory, activating without scarabs");
                _currentState = MapBotState.ReadyToActivate;
                return;
            }

            // Place scarabs
            Log.InfoFormat("[SimpleMapBot] Placing {0} scarabs", scarabsToPlace.Count);
            foreach (var scarab in scarabsToPlace)
            {
                if (await MapDeviceService.PlaceItemInDevice(scarab))
                {
                    Log.DebugFormat("[SimpleMapBot] Placed scarab: {0}", scarab.Name);
                    await Coroutine.Sleep(150);
                }
            }

            _currentState = MapBotState.ReadyToActivate;
            _stateAttempts = 0;
        }

        private async Task ActivateMapDevice()
        {
            Log.Info("[SimpleMapBot] Activating map device");

            if (await MapDeviceService.ActivateMapDevice())
            {
                _currentState = MapBotState.WaitingForPortal;
                _stateAttempts = 0;
                await Coroutine.Sleep(500);
            }
            else
            {
                _stateAttempts++;
                if (_stateAttempts > 5)
                {
                    Log.Error("[SimpleMapBot] Failed to activate device after 5 attempts");
                    _stateAttempts = 0;
                    _currentState = MapBotState.Idle;
                }
            }
        }

        private async Task EnterPortal()
        {
            var portal = MapDeviceService.FindMapPortal();

            if (portal == null)
            {
                _stateAttempts++;
                if (_stateAttempts > 30)
                {
                    Log.Error("[SimpleMapBot] Portal did not appear");
                    _stateAttempts = 0;
                    _currentState = MapBotState.Idle;
                }
                return;
            }

            // Move to portal
            if (portal.Distance > 30f)
            {
                if (!SafeMove(portal.Position))
                    return;
                await Coroutine.Sleep(100);
                return;
            }

            // Enter portal
            Log.Info("[SimpleMapBot] Entering map portal");
            if (await PortalService.EnterPortal(portal))
            {
                _currentState = MapBotState.InMap;
                _stateAttempts = 0;
                await Coroutine.Sleep(2000);
            }
        }

        private async Task StashItems()
        {
            var stash = LokiPoe.ObjectManager.Stash;
            if (stash == null)
                return;

            // Move to stash
            if (stash.Distance > 15f)
            {
                if (!SafeMove(stash.Position))
                    return;
                await Coroutine.Sleep(100);
                return;
            }

            // Open stash
            if (!LokiPoe.InGameState.StashUi.IsOpened)
            {
                Log.Info("[SimpleMapBot] Opening stash to deposit items");
                await Coroutines.InteractWith(stash);
                await Coroutine.Sleep(500);
                return;
            }

            // Stash all items except maps
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            var itemsToStash = inventory?.Items.Where(i => i != null && i.Class != "Maps").ToList();

            if (itemsToStash != null && itemsToStash.Any())
            {
                foreach (var item in itemsToStash)
                {
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    var result = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.FastMove(item.LocalId);

                    if (result == FastMoveResult.None)
                    {
                        Log.InfoFormat("[SimpleMapBot] Stashed: {0}", item.Name);
                        await Coroutine.Sleep(50);
                    }
                }
            }

            // Close stash
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.close_panels, true, false, false);
            await Coroutine.Sleep(200);

            Log.Info("[SimpleMapBot] Finished stashing, returning to map");
            _currentState = MapBotState.Idle; // Will start new map cycle
        }

        private async Task TryLootNearbyItems()
        {
            var loot = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
                .Where(wi => wi != null && wi.IsValid &&
                           wi.Distance < 50 &&
                           IsItemValuable(wi.Item))
                .OrderBy(wi => wi.Distance)
                .FirstOrDefault();

            if (loot == null)
                return;

            // Move to loot
            if (loot.Distance > 10)
            {
                if (!SafeMove(loot.Position))
                    return;
                await Coroutine.Sleep(50);
                return;
            }

            // Pick up
            if (await Coroutines.InteractWith(loot))
            {
                Log.InfoFormat("[SimpleMapBot] Looted: {0}", loot.Item?.Name ?? "Unknown");
                await Coroutine.Sleep(100);
            }
        }

        private bool IsItemValuable(Item item)
        {
            if (item == null)
                return false;

            // Always pick up currency, maps, and divination cards
            if (item.Class == "Currency" || item.Class == "Maps" || item.Class == "Divination Card")
                return true;

            // Use poe.ninja filtering if enabled
            var settings = SimpleMapBotSettings.Instance;
            if (settings.UsePoeNinjaFiltering && settings.MinItemValueChaos > 0)
            {
                var value = SimpleMapBot.Services.PoeNinjaService.GetItemValue(item.Name, item.Class);
                if (value.HasValue && value.Value >= settings.MinItemValueChaos)
                {
                    Log.DebugFormat("[SimpleMapBot] Valuable item detected: {0} ({1}c)", item.Name, value.Value);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Safely moves to a position, checking for null PlayerMover
        /// </summary>
        private bool SafeMove(DreamPoeBot.Loki.Common.Vector2i position)
        {
            var mover = PlayerMoverManager.Current;
            if (mover == null)
            {
                if (_tickCount % 100 == 0)
                {
                    Log.Error("[SimpleMapBot] Cannot move - no PlayerMover selected! Please select BeastMover in Bot Settings.");
                }
                return false;
            }

            mover.MoveTowards(position);
            return true;
        }

        private Item GetEnabledMapFromInventory()
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            var settings = SimpleMapBotSettings.Instance;

            return inventory?.Items.FirstOrDefault(item =>
                item != null && item.Class == "Maps" &&
                settings.IsMapEnabled(item.Name));
        }

        private bool IsInventoryFull()
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
                return false;

            const int INVENTORY_COLS = 12;
            const int INVENTORY_ROWS = 5;
            bool[,] occupiedSlots = new bool[INVENTORY_COLS, INVENTORY_ROWS];

            foreach (var item in inventory.Items)
            {
                if (item == null || !item.IsValid)
                    continue;

                int x = (int)item.LocationTopLeft.X;
                int y = (int)item.LocationTopLeft.Y;
                int width = (int)item.Size.X;
                int height = (int)item.Size.Y;

                for (int dx = 0; dx < width; dx++)
                {
                    for (int dy = 0; dy < height; dy++)
                    {
                        int slotX = x + dx;
                        int slotY = y + dy;

                        if (slotX < INVENTORY_COLS && slotY < INVENTORY_ROWS)
                        {
                            occupiedSlots[slotX, slotY] = true;
                        }
                    }
                }
            }

            int freeSlots = 0;
            for (int x = 0; x < INVENTORY_COLS; x++)
            {
                for (int y = 0; y < INVENTORY_ROWS; y++)
                {
                    if (!occupiedSlots[x, y])
                        freeSlots++;
                }
            }

            return freeSlots <= 5;
        }

        private async Task ReturnToHideout()
        {
            var portalSkill = LokiPoe.Me.AvailableSkills.FirstOrDefault(s => s.Name == "Portal");
            if (portalSkill != null && portalSkill.CanUse())
            {
                Log.Info("[SimpleMapBot] Using portal to return to hideout");
                LokiPoe.InGameState.SkillBarHud.Use(portalSkill.Slot, false, false);
                await Coroutine.Sleep(500);

                for (int i = 0; i < 30; i++)
                {
                    var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                        .FirstOrDefault(p => p.Distance < 20);

                    if (portal != null)
                    {
                        if (await Coroutines.InteractWith(portal))
                        {
                            await Coroutine.Sleep(2000);
                            return;
                        }
                    }
                    await Coroutine.Sleep(100);
                }
            }
        }
        #endregion

        private enum MapBotState
        {
            Idle,
            NeedMap,
            HasMap,
            MapInDevice,
            ReadyToActivate,
            WaitingForPortal,
            InMap,
            NeedToStash
        }
    }
}
