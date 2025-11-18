using System;
using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using SimpleMapBot.Configuration;
using SimpleMapBot.Core;
using log4net;

namespace SimpleMapBot.Tasks
{
    public class ReturnToHideoutTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public string Name => "ReturnToHideoutTask";
        public string Description => "Returns to hideout when needed";
        public bool IsEnabled => true;

        public async Task<bool> Run()
        {
            var settings = SimpleMapBotSettings.Instance;
            var cwa = LokiPoe.CurrentWorldArea;

            // Don't run if already in hideout or town
            if (cwa.IsHideoutArea || cwa.IsTown)
            {
                // Reset map state if we're in hideout and map is complete
                if (MapState.MapComplete)
                {
                    MapState.Reset();
                }
                return false;
            }

            // Check return conditions
            bool shouldReturn = false;
            string reason = "";

            // Inventory full
            if (settings.ReturnWhenInventoryFull && IsInventoryFull())
            {
                shouldReturn = true;
                reason = "inventory full";
            }

            // Low health
            if (LokiPoe.Me.HealthPercent < settings.ReturnHealthPercent)
            {
                shouldReturn = true;
                reason = $"low health ({LokiPoe.Me.HealthPercent}%)";
            }

            if (!shouldReturn)
                return false;

            // Check if map is incomplete and we need to create a portal to return
            bool needsReturnPortal = MapState.MapInProgress && !MapState.MapComplete;

            if (needsReturnPortal)
            {
                Log.InfoFormat("[ReturnToHideoutTask] Map incomplete ({0}% explored) - creating return portal", MapState.ExplorationPercent);

                // Create portal for return
                if (await CreateReturnPortal())
                {
                    MapState.PortalCreated = true;
                    Log.Info("[ReturnToHideoutTask] Return portal created - will resume map after banking");
                }
                else
                {
                    Log.Warn("[ReturnToHideoutTask] Failed to create return portal - map will not be resumed");
                    MapState.MapComplete = true; // Mark complete so we don't try again
                }
            }
            else
            {
                Log.InfoFormat("[ReturnToHideoutTask] Returning to hideout - {0} (map complete)", reason);
            }

            // Use portal to return
            if (await UsePortal())
            {
                // Wait for zone transition
                await Coroutine.Sleep(2000);
                return true;
            }

            return false;
        }

        private bool IsInventoryFull()
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
                return false;

            // Count free slots
            int totalSlots = inventory.Width * inventory.Height;
            int usedSlots = inventory.AvailableItems.Count;
            int freeSlots = totalSlots - usedSlots;

            return freeSlots <= 5; // Return if 5 or fewer slots available
        }

        private async Task<bool> UsePortal()
        {
            // First try to use portal skill
            var portalSkill = LokiPoe.Me.AvailableSkills.Find(s => s.Name == "Portal");
            if (portalSkill != null && portalSkill.CanUse())
            {
                var slot = portalSkill.Slot;
                LokiPoe.InGameState.SkillBarHud.Use(slot, false, false);
                await Coroutine.Sleep(500);

                // Wait for portal to appear
                for (int i = 0; i < 30; i++)
                {
                    var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                        .Find(p => p.Distance < 20);
                    if (portal != null)
                    {
                        await Coroutines.InteractWith(portal);
                        return true;
                    }
                    await Coroutine.Sleep(100);
                }
            }

            // Fallback: Try to use portal scroll from inventory
            var portalScroll = LokiPoe.InstanceInfo.GetPlayerInventoryItemsByName("Portal Scroll").FirstOrDefault();
            if (portalScroll != null)
            {
                int invId = portalScroll.LocalId;
                LokiPoe.InGameState.InventoryUi.InventoryControl_Main.UseItem(invId);
                await Coroutine.Sleep(2000);
                return true;
            }

            Log.Warn("[ReturnToHideoutTask] No portal skill or portal scrolls available!");
            return false;
        }

        private async Task<bool> CreateReturnPortal()
        {
            // Try to use portal skill to create return portal (but don't enter it)
            var portalSkill = LokiPoe.Me.AvailableSkills.Find(s => s.Name == "Portal");
            if (portalSkill != null && portalSkill.CanUse())
            {
                var slot = portalSkill.Slot;
                LokiPoe.InGameState.SkillBarHud.Use(slot, false, false);
                await Coroutine.Sleep(500);

                // Wait for portal to appear and verify it was created
                for (int i = 0; i < 30; i++)
                {
                    var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                        .Find(p => p.Distance < 20);
                    if (portal != null)
                    {
                        Log.Info("[ReturnToHideoutTask] Return portal created successfully");
                        return true;
                    }
                    await Coroutine.Sleep(100);
                }

                Log.Warn("[ReturnToHideoutTask] Portal skill used but portal didn't appear");
                return false;
            }

            // Fallback: Try to use portal scroll
            var portalScroll = LokiPoe.InstanceInfo.GetPlayerInventoryItemsByName("Portal Scroll").FirstOrDefault();
            if (portalScroll != null)
            {
                int invId = portalScroll.LocalId;
                LokiPoe.InGameState.InventoryUi.InventoryControl_Main.UseItem(invId);
                await Coroutine.Sleep(1000);

                // Verify portal was created
                var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                    .Find(p => p.Distance < 20);
                if (portal != null)
                {
                    Log.Info("[ReturnToHideoutTask] Return portal created with scroll");
                    return true;
                }

                Log.Warn("[ReturnToHideoutTask] Portal scroll used but portal didn't appear");
                return false;
            }

            Log.Warn("[ReturnToHideoutTask] No portal skill or portal scrolls available for return portal!");
            return false;
        }
    }
}
