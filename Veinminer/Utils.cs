using Terraria;
using TShockAPI;

namespace MyPlugin1
{
    class Utils
    {
        public static List<Item> GetItemsFromTile(int x, int y)
        {
            List<Item> items = new();

            if (!WorldGen.InWorld(x, y))
                return items;

            WorldGen.KillTile_GetItemDrops(
                x,
                y,
                Main.tile[x, y],
                out int itemId,
                out int stack,
                out int secondaryItemId,
                out int secondaryStack,
                out bool noPrefix,
                false
            );

            if (itemId > 0 && stack > 0)
            {
                Item item = new();
                item.SetDefaults(itemId);
                item.stack = stack;
                items.Add(item);
            }

            if (secondaryItemId > 0 && secondaryStack > 0)
            {
                Item item2 = new();
                item2.SetDefaults(secondaryItemId);
                item2.stack = secondaryStack;
                items.Add(item2);
            }

            return items;
        }
    }

    public static class Expansion
    {
        public static int GetBlankSlot(this TSPlayer tsp)
        {
            int num = 0;
            tsp.TPlayer.inventory.ForEach(s => { if (s.type == 0) num++; });
            return num;
        }

        public static bool IsSpaceEnough(this TSPlayer tsp, int id, int stack)
        {
            int available = 0;
            Item item = new Item();
            item.SetDefaults(id);
            Item s;
            for (int i = 0; i < 50; i++)
            {
                s = tsp.TPlayer.inventory[i];
                if (available < stack)
                {
                    if (s.type == id) available += (s.maxStack - s.stack);
                    else if (s.type == 0) available += item.maxStack;
                }
                else
                {
                    break;
                }
            }
            return available >= stack;
        }
    }
}