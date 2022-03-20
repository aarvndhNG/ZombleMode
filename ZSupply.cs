using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;

namespace ZombleMode
{
    public class ZSupply
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool Completed { get; set; }
        public List<NetItem> Items { get; set; }
        public ZSupply(int x,int y,bool state)
        {
            X = x;
            Y = y;
            Completed = state;
            Items = new List<NetItem>(40);
        }
        public void Generate() 
        {
            Place();
            PlaceItem();
            Completed = true;
        }
        public void Place() 
        {
            WorldGen.PlaceChest(X, Y, 21, true, 2);
        }
        public void Kill() 
        {
            var id = Chest.FindChest(X,Y);
            if (id!=-1) Chest.DestroyChestDirect(X, Y, id);
            Completed = false;
        }
        public void PlaceItem() 
        {
            var id = Chest.FindChest(X, Y);
            if (id != -1)
            {
                var chest = Terraria.Main.chest[id];
                for (int i = 0; i < Items.Count; i++)
                {
                    chest.item[i].netDefaults(Items[i].NetId);
                    chest.item[i].stack = Items[i].Stack;
                    chest.item[i].prefix = Items[i].PrefixId;
                    TSPlayer.All.SendData(PacketTypes.ChestItem, "", id, i);
                }  
            }
        }
    }
}
