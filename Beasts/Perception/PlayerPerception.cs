using System.Linq;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using Beasts.Core;
using Beasts.Models;
using log4net;

namespace Beasts.Perception
{
    /// <summary>
    /// Monitors player state (health, mana, resources, etc.)
    /// </summary>
    public class PlayerPerception : IPerceptionModule
    {
        private static readonly ILog Log = Beasts.Core.Logger.GetLoggerInstanceForType();

        public string Name => "PlayerPerception";

        public int HealthPercent { get; private set; }
        public int ManaPercent { get; private set; }
        public int EnergyShieldPercent { get; private set; }
        public bool InventoryFull { get; private set; }
        public int AvailableFlasks { get; private set; }
        public int PortalScrollCount { get; private set; }
        public int InventorySpaceUsed { get; private set; }
        public int InventorySpaceTotal { get; private set; }

        // Detailed flask tracking
        public FlaskCounts FlaskCounts { get; private set; }
        public bool HasLifeFlasks => FlaskCounts?.LifeFlasks > 0;
        public bool HasManaFlasks => FlaskCounts?.ManaFlasks > 0;
        public bool HasUtilityFlasks => FlaskCounts?.UtilityFlasks > 0;

        public void Update()
        {
            if (!LokiPoe.IsInGame || LokiPoe.Me == null)
                return;

            var me = LokiPoe.Me;

            // Health/Mana/ES
            HealthPercent = (int)me.HealthPercent;
            ManaPercent = (int)me.ManaPercent;
            EnergyShieldPercent = (int)me.EnergyShieldPercent;

            // Inventory
            CalculateInventoryFullness();
            InventoryFull = CheckInventoryFull();

            // Flasks
            AvailableFlasks = CountAvailableFlasks();

            // Portal scrolls
            PortalScrollCount = CheckPortalScrollCount();
        }

        /// <summary>
        /// Checks if inventory is full (no free slots)
        /// </summary>
        private bool CheckInventoryFull()
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
                return false;

            // Standard PoE inventory: 12 columns x 5 rows = 60 slots
            const int INVENTORY_COLS = 12;
            const int INVENTORY_ROWS = 5;
            bool[,] occupiedSlots = new bool[INVENTORY_COLS, INVENTORY_ROWS];

            // Mark occupied slots
            foreach (var item in inventory.Items)
            {
                if (item == null || !item.IsValid)
                    continue;

                int x = (int)item.LocationTopLeft.X;
                int y = (int)item.LocationTopLeft.Y;
                int width = (int)item.Size.X;
                int height = (int)item.Size.Y;

                // Mark all slots this item occupies
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

            // Count free slots
            int freeSlots = 0;
            for (int x = 0; x < INVENTORY_COLS; x++)
            {
                for (int y = 0; y < INVENTORY_ROWS; y++)
                {
                    if (!occupiedSlots[x, y])
                        freeSlots++;
                }
            }

            // Consider inventory full if we have less than 4 free slots (safety margin)
            bool isFull = freeSlots < 4;

            Log.DebugFormat("[PlayerPerception] Free inventory slots: {0}, Full: {1}", freeSlots, isFull);
            return isFull;
        }

        /// <summary>
        /// Counts how many flasks are available (not on cooldown)
        /// </summary>
        private int CountAvailableFlasks()
        {
            try
            {
                // Count flasks directly from inventory
                FlaskCounts = GetFlaskCounts();

                Log.DebugFormat("[PlayerPerception] Flasks - Total: {0}, Life: {1}, Mana: {2}, Utility: {3}, Instant: {4}",
                    FlaskCounts.TotalFlasks,
                    FlaskCounts.LifeFlasks,
                    FlaskCounts.ManaFlasks,
                    FlaskCounts.UtilityFlasks,
                    FlaskCounts.InstantFlasks);

                return (int)FlaskCounts.TotalFlasks;
            }
            catch (System.Exception ex)
            {
                Log.Error("[PlayerPerception] Error counting flasks", ex);
                FlaskCounts = new FlaskCounts();
                return 0;
            }
        }

        /// <summary>
        /// Get detailed flask counts from inventory
        /// </summary>
        private FlaskCounts GetFlaskCounts()
        {
            var counts = new FlaskCounts();

            try
            {
                var flaskInventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Flask);
                if (flaskInventory == null)
                    return counts;

                foreach (var item in flaskInventory.Items)
                {
                    if (item == null || !item.IsValid)
                        continue;

                    var itemClass = item.Class?.ToLower() ?? "";

                    // Count by type
                    if (itemClass.Contains("life"))
                    {
                        counts.LifeFlasks++;
                        counts.TotalFlasks++;
                        
                        // Check if instant
                        if (item.Components?.FlaskComponent?.InstantRecovery ?? false)
                            counts.InstantFlasks++;
                    }
                    else if (itemClass.Contains("mana") || itemClass.Contains("hybrid"))
                    {
                        counts.ManaFlasks++;
                        counts.TotalFlasks++;
                    }
                    else if (itemClass.Contains("flask"))
                    {
                        counts.UtilityFlasks++;
                        counts.TotalFlasks++;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("[PlayerPerception] Error in GetFlaskCounts", ex);
            }

            return counts;
        }

        /// <summary>
        /// Checks portal scroll count in inventory
        /// </summary>
        private int CheckPortalScrollCount()
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
                return 0;

            // Count portal scrolls
            var scrollCount = inventory.Items
                .Where(item => item != null &&
                              item.IsValid &&
                              item.Metadata != null &&
                              (item.Metadata.ToLower().Contains("portalscroll") ||
                               item.Name.ToLower().Contains("portal scroll")))
                .Sum(item => item.StackCount);

            Log.DebugFormat("[PlayerPerception] Portal Scrolls: {0}", scrollCount);
            return (int)scrollCount;
        }

        /// <summary>
        /// Calculate inventory fullness percentage
        /// </summary>
        private void CalculateInventoryFullness()
        {
            var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
            if (inventory == null)
            {
                InventorySpaceUsed = 0;
                InventorySpaceTotal = 0;
                return;
            }

            // Count used slots
            var items = inventory.Items.Where(i => i != null && i.IsValid).ToList();
            InventorySpaceUsed = (int)items.Count;

            // Total slots (typical PoE inventory is 60 slots: 12 columns x 5 rows)
            InventorySpaceTotal = 60; // Could make this dynamic if inventory size changes

            Log.DebugFormat("[PlayerPerception] Inventory: {0}/{1} ({2:F1}% full)",
                InventorySpaceUsed, InventorySpaceTotal,
                InventorySpaceTotal > 0 ? (float)InventorySpaceUsed / InventorySpaceTotal * 100f : 0f);
        }
    }
}
