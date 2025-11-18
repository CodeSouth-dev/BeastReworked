using System.Linq;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using Beasts.Core;
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
        public int PortalScrollCount { get; private set; }
        public int InventorySpaceUsed { get; private set; }
        public int InventorySpaceTotal { get; private set; }

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