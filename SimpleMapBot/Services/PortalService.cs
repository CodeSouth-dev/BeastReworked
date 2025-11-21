using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

namespace SimpleMapBot.Services
{
    /// <summary>
    /// Service for portal creation and interaction
    /// </summary>
    public static class PortalService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PortalService));

        /// <summary>
        /// Creates a portal using the Portal gem skill
        /// </summary>
        /// <returns>True if portal was created successfully</returns>
        public static async Task<bool> CreatePortalUsingGem()
        {
            try
            {
                // Find portal gem skill on skill bar
                var portalSkill = LokiPoe.InGameState.SkillBarHud.Skills
                    .FirstOrDefault(s => s != null &&
                                       s.Name != null &&
                                       s.Name.ToLower().Contains("portal"));

                if (portalSkill == null)
                {
                    Log.Warn("[PortalService] No portal gem found on skill bar");
                    return false;
                }

                if (!portalSkill.CanUse())
                {
                    Log.Warn("[PortalService] Portal gem cannot be used (cooldown/mana/charges)");
                    return false;
                }

                Log.Info("[PortalService] Using portal gem to create portal");

                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                var useResult = LokiPoe.InGameState.SkillBarHud.Use(portalSkill.Slot, false);

                if (useResult != LokiPoe.InGameState.UseResult.None)
                {
                    Log.ErrorFormat("[PortalService] Failed to use portal gem: {0}", useResult);
                    return false;
                }

                // Wait for portal to spawn
                await Coroutine.Sleep(1000);

                // Verify portal was created
                var portal = FindNearestPortal();
                if (portal != null)
                {
                    Log.Info("[PortalService] Portal created successfully");
                    return true;
                }

                Log.Warn("[PortalService] Portal gem used but no portal found nearby");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[PortalService] Error creating portal", ex);
                return false;
            }
        }

        /// <summary>
        /// Finds the nearest portal object
        /// </summary>
        /// <returns>Nearest Portal object or null</returns>
        public static Portal FindNearestPortal()
        {
            try
            {
                return LokiPoe.ObjectManager.Objects
                    .OfType<Portal>()
                    .Where(p => p != null && p.IsValid && p.IsTargetable)
                    .OrderBy(p => p.Distance)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log.Error("[PortalService] Error finding portal", ex);
                return null;
            }
        }

        /// <summary>
        /// Enters a portal
        /// </summary>
        /// <param name="portal">Portal to enter</param>
        /// <returns>True if successfully initiated portal entry</returns>
        public static async Task<bool> EnterPortal(Portal portal)
        {
            if (portal == null || !portal.IsValid)
            {
                Log.Error("[PortalService] Invalid portal");
                return false;
            }

            Log.InfoFormat("[PortalService] Entering portal at distance {0:F1}", portal.Distance);

            // Move closer if needed
            if (portal.Distance > 30)
            {
                PlayerMoverManager.Current.MoveTowards(portal.Position);
                await Coroutine.Sleep(100);
                return false; // Still moving
            }

            // Interact with portal
            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            if (!await Coroutines.InteractWith(portal))
            {
                Log.Error("[PortalService] Failed to interact with portal");
                return false;
            }

            // Wait for area transition
            await Coroutine.Sleep(500);

            Log.Info("[PortalService] Portal entry initiated");
            return true;
        }

        /// <summary>
        /// Creates a portal using portal scroll (press 'B' keybind)
        /// </summary>
        /// <returns>True if portal scroll was used</returns>
        public static async Task<bool> CreatePortalUsingScroll()
        {
            try
            {
                Log.Info("[PortalService] Using portal scroll (pressing 'B' key)");

                LokiPoe.ProcessHookManager.ClearAllKeyStates();

                // Press 'B' key to use portal scroll
                LokiPoe.Input.SimulateKeyEvent(System.Windows.Forms.Keys.B, true, false, false);

                // Wait for portal to spawn
                await Coroutine.Sleep(1000);

                // Verify portal was created
                var portal = FindNearestPortal();
                if (portal != null)
                {
                    Log.Info("[PortalService] Portal scroll used successfully");
                    return true;
                }

                Log.Warn("[PortalService] Portal scroll used but no portal found nearby");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[PortalService] Error using portal scroll", ex);
                return false;
            }
        }

        /// <summary>
        /// Creates a portal using either gem or scroll (tries gem first, falls back to scroll)
        /// </summary>
        /// <returns>True if portal was created successfully</returns>
        public static async Task<bool> CreatePortal()
        {
            // Try portal gem first
            if (await CreatePortalUsingGem())
            {
                return true;
            }

            Log.Info("[PortalService] Portal gem failed or unavailable, trying portal scroll");

            // Fallback to portal scroll
            return await CreatePortalUsingScroll();
        }

        /// <summary>
        /// Creates a portal and waits for it to be ready
        /// </summary>
        /// <returns>The created portal or null if failed</returns>
        public static async Task<Portal> CreateAndWaitForPortal()
        {
            if (!await CreatePortal())
                return null;

            // Wait for portal to be targetable
            Portal portal = null;
            for (int i = 0; i < 20; i++) // 2 seconds max
            {
                portal = FindNearestPortal();
                if (portal != null && portal.IsTargetable)
                    break;

                await Coroutine.Sleep(100);
            }

            if (portal == null)
            {
                Log.Error("[PortalService] Portal created but not found after waiting");
                return null;
            }

            return portal;
        }

        /// <summary>
        /// Attempts to travel to hideout using portal gem
        /// </summary>
        /// <returns>True if travel was initiated successfully</returns>
        public static async Task<bool> TravelToHideoutViaPortal()
        {
            var portal = await CreateAndWaitForPortal();
            if (portal == null)
                return false;

            return await EnterPortal(portal);
        }
    }
}
