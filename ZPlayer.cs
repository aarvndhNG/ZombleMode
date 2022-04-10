using Microsoft.Xna.Framework;
using MiniGamesAPI;
using MiniGamesAPI.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TShockAPI;

namespace ZombleMode
{
    public class ZPlayer:MiniPlayer
    {
        [JsonIgnore]
        public ZEnum Character { get; set; }
        [JsonIgnore]
        public bool IsDead { get; set; }
        [JsonIgnore]
        public int CD { get; set; }
        [JsonIgnore]
        public int BulletAmount { get; set; }
        [JsonIgnore]
        public bool BeLoaded { get; set; }
        public List<string> KillNames { get; set; }
        [JsonIgnore]
        public Timer BulletTimer = new Timer(1000);
        public ZPlayer(int id,TSPlayer plr) 
        {
            ID = id;
            Name = plr.Name;
            Player = plr;
            IsReady = false;
            BackUp = null;
            Status = MiniGamesAPI.Enum.PlayerStatus.Waiting;
            Character = ZEnum.Human;
            KillNames = new List<string>();
            CD = 5;
            BulletAmount = 30;
            BeLoaded = false;
            BulletTimer.Elapsed += OnTick;
        }
        public ZPlayer()
        {
            CD = 5;
            BulletAmount = 30;
            BeLoaded = false;
            BulletTimer.Elapsed += OnTick;
            Character = ZEnum.Human;
        }

        private void OnTick(object sender, ElapsedEventArgs e)
        {
            if (IsDead)
            {
                SetBuff(10);
            }
            if (BeLoaded)
            {
                if (CD!=0)
                {
                    CD -= 1;
                }
                else
                {
                    BeLoaded = false;
                    CD = 5;
                    BulletAmount = 30;
                }
            }
            
        }

        public void Join(ZRoom room)
        {
            if (room.Status!=MiniGamesAPI.Enum.RoomStatus.Waiting)
            {
                SendInfoMessage("当前房间状态无法加入游戏");
                return;
            }
            if (room.Players.Count>=room.MaxPlayer)
            {
                SendInfoMessage("该房间满人了");
                return;
            }
            if (CurrentRoomID!=0)
            {
                var originRoom = ConfigUtils.GetRoomByID(CurrentRoomID);
                if (originRoom != null) Leave();
            }
            if (!room.Players.Contains(this))
            {
                room.Broadcast($"玩家 [{Name}] 加入了房间", Color.Orange);
                room.Players.Add(this);
                CurrentRoomID = room.ID;
                SelectPackID = room.HumanPackID;
                BackUp = new MiniPack(Name,ID);
                BackUp.CopyFromPlayer(Player);
                Teleport(room.LobbyPoint) ;
                SendSuccessMessage($"你已加入房间 [{room.ID}][{room.Name}]");
            }
            else 
            {
                SendInfoMessage("你已在此房间中");
            }
        }
        public new void  Leave()
        {
            var room = ConfigUtils.GetRoomByID(CurrentRoomID);
            if (room==null)
            {
                SendInfoMessage("房间不存在或您未加入任何房间");
                return;
            }
            if (room.Status!=MiniGamesAPI.Enum.RoomStatus.Waiting)
            {
                SendInfoMessage("当前房间状态不允许离开");
                return;
            }
            room.Players.Remove(this);
            room.Broadcast($"玩家 {Name} 离开了房间", Color.Crimson);
            CurrentRoomID = 0;
            SelectPackID = 0;
            IsReady = false;
            Status = MiniGamesAPI.Enum.PlayerStatus.Waiting;
            Character = ZEnum.Human;
            if (BackUp != null) BackUp.RestoreCharacter(Player);
            Player.SaveServerCharacter();
            SendSuccessMessage($"你离开了房间 [{room.ID}][{room.Name}]");
            SendBoardMsg("");
            Teleport(new Point(Terraria.Main.spawnTileX,Terraria.Main.spawnTileY));
        }
    }
}
