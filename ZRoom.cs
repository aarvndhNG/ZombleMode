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
    public class ZRoom : MiniRoom, IRoom
    {
        public int MaxPlayer { get; set; }
        public int MinPlayer { get; set; }
        public int WaitingTime { get; set; }
        public int GamingTime { get; set; }
        public int SelectTime { get; set; }
        public int RespawnTime { get; set; }
        public int RootZombleAmount { get; set; }
        public int HumanPackID { get; set; }
        public int NormalPackID { get; set; }
        public int ViewerPackID { get; set; }
        public int RootPackID { get; set; }
        public bool HumanWin { get; set; }
        public Point LobbyPoint { get; set; }
        public List<Point> SpawnPoints { get; set; }
        [JsonIgnore]
        public List<ZPlayer> Players { get; set; }
        [JsonIgnore]
        public Timer waitingTimer = new Timer(1000);
        [JsonIgnore]
        public Timer gamingTimer = new Timer(1000);
        [JsonIgnore]
        public Timer selectTimer = new Timer(1000);
        public ZRoom(int id,string name) {
            ID = id;
            Name = name;
            Status = MiniGamesAPI.Enum.RoomStatus.Waiting;
            SpawnPoints = new List<Point>();
            Players = new List<ZPlayer>();
            Initialize();
            Start();
        }
        public ZRoom() 
        {
            SpawnPoints = new List<Point>();
            Players = new List<ZPlayer>();
            Initialize();
            Start();
        }
        public void Broadcast(string msg, Color color)
        {
            for (int i = Players.Count-1; i>=0; i--)
            {
                var plr = Players[i];
                plr.SendMessage(msg,color);
            }
        }
        public void Broadcast(string msg, Color color,string name)
        {
            for (int i = Players.Count - 1; i >= 0; i--)
            {
                var plr = Players[i];
                if (plr.Name==name) continue;
                plr.SendMessage(msg, color);
            }
        }
        public void Conclude()
        {
            ShowVictory();
            for (int i = Players.Count-1; i>=0; i--)
            {
                var plr = Players[i];
                plr.IsReady = false;
                plr.IsDead = false;
                if (!plr.Player.Dead) plr.Teleport(LobbyPoint);
                plr.SetTeam(0);
                plr.SetPVP(false);
                plr.BulletTimer.Stop();
                plr.CD = 5;
                plr.BulletAmount = 30;
                plr.BackUp.RestoreCharacter(plr);
                plr.Player.SaveServerCharacter();
                plr.Character = ZEnum.Human;
                plr.Status = MiniGamesAPI.Enum.PlayerStatus.Waiting;
            }
            Status = MiniGamesAPI.Enum.RoomStatus.Restoring;
        }

        public void Dispose()
        {
            for (int i = Players.Count-1; i>=0; i--)
            {
                var plr = Players[i];
                plr.Teleport(Terraria.Main.spawnTileX,Terraria.Main.spawnTileY);
                plr.ClearRecord();
                plr.SendInfoMessage("房间被强制暂停了");
                plr.SetPVP(false);
                plr.SetTeam(0);
                plr.Character = ZEnum.Human;
                plr.Status = MiniGamesAPI.Enum.PlayerStatus.Waiting;
                plr.CurrentRoomID = 0;
                plr.SelectPackID = 0;
                plr.BackUp.RestoreCharacter(plr);
                plr.Player.SaveServerCharacter();
                plr.IsDead = false;
                plr.IsReady = false;
                Players.Clear();
            }
        }

        public int GetPlayerCount()
        {
            return Players.Count;
        }

        public void Initialize()
        {
            waitingTimer.Elapsed += OnWaiting;
            gamingTimer.Elapsed += OnGaming;
            selectTimer.Elapsed += OnSelecting;
            
        }

        private void OnSelecting(object sender, ElapsedEventArgs e)
        {
            ShowRoomMemberInfo();
            if (Status != MiniGamesAPI.Enum.RoomStatus.Gaming) return;
            if (SelectTime==0)
            {
                selectTimer.Stop();
                SelectZomble();
                for (int i = Players.Count-1; i>=0; i--)
                {
                    var plr = Players[i];
                    if (plr.IsDead)
                    {
                        plr.IsDead = false;
                        plr.Character = ZEnum.Zomble;
                        plr.SelectPackID = NormalPackID;
                        var rand = new Random();
                        var pack = ConfigUtils.GetPackByID(plr.SelectPackID);
                        pack.RestoreCharacter(plr);
                        plr.Teleport(SpawnPoints[rand.Next(0,SpawnPoints.Count - 1)]);
                        plr.SendInfoMessage("已重生！");
                    }
                    plr.SetPVP(true);
                    plr.Godmode(false);
                    if (plr.Character==ZEnum.Zomble)
                    {
                        plr.SetTeam(1);//设置红队
                    }
                    else { plr.SetTeam(3); }//设置蓝队
                }
                Broadcast("母体出现了！",Color.Crimson);
                gamingTimer.Start();
            }
            else
            {
                Broadcast($"母体还有 {SelectTime} 秒后出现,请留意你身边的人..",Color.DarkTurquoise);
                SelectTime--;
            }
        }

        private void OnGaming(object sender, ElapsedEventArgs e)
        {
            ShowRoomMemberInfo();
            if (Status != MiniGamesAPI.Enum.RoomStatus.Gaming) return;
            var zombles = Players.Where(p => p.Character == ZEnum.Zomble).ToList();
            var humen = Players.Where(p => p.Character == ZEnum.Human).ToList();
            if (GamingTime==0)
            {
                gamingTimer.Stop();
                if (humen.Count!=0) HumanWin = true;
                Status = MiniGamesAPI.Enum.RoomStatus.Concluding;
                Conclude();
                Restore();
            }
            else 
            {
                if (Players.Count==0)
                {
                    gamingTimer.Stop();
                    Status = MiniGamesAPI.Enum.RoomStatus.Concluding;
                    Conclude();
                    Restore();
                }
                if (humen.Count==0)
                {
                    gamingTimer.Stop();
                    Status = MiniGamesAPI.Enum.RoomStatus.Concluding;
                    Conclude();
                    Restore();
                }
                if (Players.Where(p=>p.Character==ZEnum.Zomble&&p.IsDead).Count()==zombles.Count)
                {
                    HumanWin = true;
                    gamingTimer.Stop();
                    Status = MiniGamesAPI.Enum.RoomStatus.Concluding;
                    Conclude();
                    Restore();
                }
                if (GamingTime==60) Broadcast("游戏还剩 1 分钟..",Color.DarkTurquoise);
                GamingTime--;
            }
        }

        private void OnWaiting(object sender, ElapsedEventArgs e)
        {
            if (Players.Count<=0) return;
            ShowRoomMemberInfo();
            if (Status == MiniGamesAPI.Enum.RoomStatus.Waiting && Players.Where(p => p.IsReady).ToList().Count < MinPlayer) return;
            if (WaitingTime==0)
            {
                waitingTimer.Stop();
                for (int i = Players.Count-1; i>=0; i--)
                {
                    var plr = Players[i];
                    plr.SelectPackID = HumanPackID;
                    plr.IsReady = true;
                    plr.Status = MiniGamesAPI.Enum.PlayerStatus.Gaming;
                    plr.Godmode(true);
                    var pack = ConfigUtils.GetPackByID(plr.SelectPackID);
                    if (pack != null) pack.RestoreCharacter(plr);
                }
                TeleportRandomly();
                selectTimer.Start();
                Status = MiniGamesAPI.Enum.RoomStatus.Gaming;
            }
            else
            {
                Broadcast($"距离游戏开始还有 {WaitingTime} 秒....",Color.MediumAquamarine);
                WaitingTime -= 1;
            }
        }

        public void Restore()
        {
            var room = ConfigUtils.GetRoomFromLocal(ID);
            WaitingTime = room.WaitingTime;
            GamingTime = room.GamingTime;
            LobbyPoint = room.LobbyPoint;
            SpawnPoints = room.SpawnPoints;
            RespawnTime = room.RespawnTime;
            MaxPlayer = room.MaxPlayer;
            MinPlayer = room.MinPlayer;
            RootPackID = room.RootPackID;
            RootZombleAmount = room.RootZombleAmount;
            HumanPackID = room.HumanPackID;
            NormalPackID = room.NormalPackID;
            SelectTime = room.SelectTime;
            HumanWin = false;
            Status = MiniGamesAPI.Enum.RoomStatus.Waiting;
            Start();
            TShock.Utils.Broadcast($"生化模式房间[{ID}][{Name}]已重置完毕，可以加入游戏啦！",Color.DarkTurquoise);
        }

        public void ShowRoomMemberInfo()
        {
            StringBuilder roomInfo = new StringBuilder();
            roomInfo.AppendLine(MiniGamesAPI.Utils.EndLine_10);
            if (Status==MiniGamesAPI.Enum.RoomStatus.Gaming)
            {
                var minutes = GamingTime / 60;
                var seconds = GamingTime % 60;
                roomInfo.AppendLine("————房内信息————");
                roomInfo.AppendLine($"游戏剩余时间[{minutes}:{seconds}]");
                for (int i = Players.Count - 1; i >= 0; i--)
                {
                    var plr = Players[i];
                    roomInfo.AppendLine($"[{plr.Name}] [{(plr.Character==ZEnum.Zomble?"感染体":"泰拉人")}]");
                }
            }
            if (Status==MiniGamesAPI.Enum.RoomStatus.Waiting)
            {
                var minutes = WaitingTime / 60;
                var seconds = WaitingTime % 60;
                roomInfo.AppendLine("————房内信息————");
                roomInfo.AppendLine($"等待倒计时[{minutes}:{seconds}]");
                roomInfo.AppendLine($"房内人数[{Players.Count}/{MaxPlayer}]");
                for (int i = Players.Count - 1; i >= 0; i--)
                {
                    var plr = Players[i];
                    roomInfo.AppendLine($"[{plr.Name}] [{(plr.IsReady ? "已准备" : "未准备")}]");
                }
                roomInfo.AppendLine("输入/zm ready 进行准备");
                roomInfo.AppendLine("输入/zm leave 离开房间");
            }
            roomInfo.AppendLine(MiniGamesAPI.Utils.EndLine_15);
            for (int i = Players.Count - 1; i >= 0; i--)
            {
                var plr = Players[i];
                plr.SendBoardMsg(roomInfo.ToString());
            }
        }

        public void ShowVictory()
        {
            StringBuilder victoryInfo = new StringBuilder();
            if (HumanWin)
            {
                victoryInfo.AppendLine("恭喜泰拉人获得胜利！");
            }
            else
            {
                victoryInfo.AppendLine("变异母体们取得胜利");
            }
            Broadcast(victoryInfo.ToString(),Color.MediumAquamarine);
        }

        public void Start()
        {
            waitingTimer.Start();
        }

        public void Stop()
        {
            gamingTimer.Stop();
            waitingTimer.Stop();
            selectTimer.Stop();
        }
        public void SelectZomble() 
        {
            var zombles = Players.Where(p=>p.Character==ZEnum.Zomble).ToList();
            var rand = new Terraria.Utilities.UnifiedRandom();
            var seed = rand.Next(0,Players.Where(p=>p.Character==ZEnum.Human).Count()-1);
            var plr = Players[seed];
            plr.Character = ZEnum.Zomble;
            plr.SelectPackID = RootPackID;
            var pack = ConfigUtils.GetPackByID(plr.SelectPackID);
            pack.RestoreCharacter(plr);
            plr.SendErrorMessage("你被选择为 母体 !");
            var secondZombles = Players.Where(p => p.Character == ZEnum.Zomble).ToList();
            if (secondZombles.Count<RootZombleAmount) SelectZomble();
        }
        public void TeleportRandomly() 
        {
            for (int i = Players.Count-1; i>=0; i--)
            {
                var plr = Players[i];
                var rand = new Terraria.Utilities.UnifiedRandom();
                var seed = rand.Next(0,SpawnPoints.Count-1);
                plr.Teleport(SpawnPoints[seed]);
                plr.SendInfoMessage("你已被传送到随机出生点");
            }
        }
    }
}
