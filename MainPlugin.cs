using Microsoft.Xna.Framework;
using MiniGamesAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace ZombleMode
{
    [ApiVersion(2, 1)]
    public class MainPlugin : TerrariaPlugin
    {
        public MainPlugin(Main game) : base(game){}
        public override string Name => "ZombleMode";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Author => "豆沙";
        public override string Description => "生化模式";
        public override void Initialize()
        {
            ServerApi.Hooks.NetGreetPlayer.Register(this,OnJoin);
            ServerApi.Hooks.ServerLeave.Register(this,OnLeave);
            ServerApi.Hooks.ServerChat.Register(this,OnChat);
            ServerApi.Hooks.GamePostInitialize.Register(this,OnPostInitialize);
            GetDataHandlers.KillMe += OnKillMe;
            GetDataHandlers.ChestOpen += OnOpenChest;
            GetDataHandlers.PlayerSpawn += OnPlayerSpawn;
            GetDataHandlers.TogglePvp += OnChangePVP;
            GetDataHandlers.PlayerTeam += OnChangeTeam;
            GetDataHandlers.NewProjectile += OnNewProjectile;
            ConfigUtils.LoadConfig();
        }
        private void OnPostInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("zm.user", ZM,"zm","生化模式"));
            Commands.ChatCommands.Add(new Command("zm.admin", ZMA, "zma", "生化管理"));
        }
        private void ZMA(CommandArgs args)
        {
            var plr = args.Player;
            ZRoom room = null;
            int id,count;
            StringBuilder board = new StringBuilder();
            if (args.Parameters.Count < 1)
            {
                args.Player.SendInfoMessage("请输入/zma help 查看帮助");
                return;
            }
            switch (args.Parameters[0])
            {
                case "list":
                    foreach (var tempRoom in ConfigUtils.rooms)
                    {
                        board.AppendLine($"[{tempRoom.ID}] [{tempRoom.Name}] [{tempRoom.GetPlayerCount()}/{tempRoom.MaxPlayer}] [{tempRoom.Status.ToString()}]");
                    }
                    plr.SendInfoMessage(board.ToString());
                    break;
                case "create":
                    if (args.Parameters.Count != 2)
                    {
                        plr.SendInfoMessage("正确指令：/zma create [房间名]");
                        return;
                    }
                    room = new ZRoom(ConfigUtils.rooms.Count + 1,args.Parameters[1]);
                    ConfigUtils.rooms.Add(room);
                    ConfigUtils.AddSingleRoom(room);
                    plr.SendInfoMessage("房间创建成功 [{0}][{1}]",room.ID,room.Name);
                    break;
                case "newpack":
                    if (args.Parameters.Count!=3)
                    {
                        plr.SendInfoMessage("指令错误");
                        return;
                    }
                    string name, plrName;
                    name = args.Parameters[1];
                    plrName = args.Parameters[2];
                    if (TSPlayer.FindByNameOrID(plrName).Count!=0)
                    {
                        var target = TSPlayer.FindByNameOrID(plrName)[0];
                        var pack = new MiniPack(name, ConfigUtils.packs.Count + 1);
                        pack.CopyFromPlayer(target);
                        ConfigUtils.packs.Add(pack);
                        ConfigUtils.UpdatePacks();
                        plr.SendInfoMessage($"成功创建以玩家{target.Name}的背包为基础的背包");
                    }
                    break;
                case "smp"://设置最大玩家数
                    if (args.Parameters.Count!=3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1],out id)&&int.TryParse(args.Parameters[2],out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room!=null)
                        {
                            room.MaxPlayer = count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的最大玩家数为{room.MaxPlayer}");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "sdp"://设置最小玩家数
                    if (args.Parameters.Count != 3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) && int.TryParse(args.Parameters[2], out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.MinPlayer = count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的最小玩家数为{room.MinPlayer}");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "sgt":
                    if (args.Parameters.Count != 3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) && int.TryParse(args.Parameters[2], out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.GamingTime = count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的游戏时长为{room.GamingTime}秒");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "sst":
                    if (args.Parameters.Count != 3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) && int.TryParse(args.Parameters[2], out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.SelectTime = count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的选择母体时长为{room.SelectTime}秒");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "swt":
                    if (args.Parameters.Count != 3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) && int.TryParse(args.Parameters[2], out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.WaitingTime = count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的等待时长为{room.WaitingTime}秒");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "src"://设置母体数量
                    if (args.Parameters.Count != 3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) && int.TryParse(args.Parameters[2], out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.RootZombleAmount = count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的母体数量为{room.RootZombleAmount}");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "snp"://普通僵尸背包
                    if (args.Parameters.Count != 3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) && int.TryParse(args.Parameters[2], out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.NormalPackID = count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的普通僵尸背包ID为{room.NormalPackID}");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "srp"://母体僵尸背包
                    if (args.Parameters.Count != 3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) && int.TryParse(args.Parameters[2], out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.RootPackID = count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的母体背包ID为{room.RootPackID}");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "shp"://人类背包
                    if (args.Parameters.Count != 3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) && int.TryParse(args.Parameters[2], out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.HumanPackID = count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的人类背包ID为{room.HumanPackID}");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "svp"://观战者背包
                    if (args.Parameters.Count != 3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) && int.TryParse(args.Parameters[2], out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.ViewerPackID= count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的观察者背包ID为{room.ViewerPackID}");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "sp":
                    if (!plr.RealPlayer)
                    {
                        plr.SendInfoMessage("请在游戏中使用此命令");
                        return;
                    }
                    plr.AwaitingTempPoint = 1;
                    plr.SendInfoMessage("请选择一个点");
                    break;
                case "swp":
                    if (!plr.RealPlayer)
                    {
                        plr.SendInfoMessage("请在游戏中使用此命令");
                        return;
                    }
                    if (args.Parameters.Count != 2)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) )
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.LobbyPoint = plr.TempPoints[0];
                            plr.TempPoints[0] = Point.Zero;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的等待大厅");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "addp":
                    if (!plr.RealPlayer)
                    {
                        plr.SendInfoMessage("请在游戏中使用此命令");
                        return;
                    }
                    if (args.Parameters.Count != 2)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.SpawnPoints.Add(plr.TempPoints[0]);
                            plr.TempPoints[0] = Point.Zero;
                            plr.SendInfoMessage($"成功添加房间(ID：{room.ID})的一个出生点");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "respawntime":
                    if (args.Parameters.Count != 3)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id) && int.TryParse(args.Parameters[2], out count))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.RespawnTime = count;
                            plr.SendInfoMessage($"成功设置房间(ID：{room.ID})的重生秒数为{room.RespawnTime}");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "remove":
                    if (args.Parameters.Count != 2)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.Stop();
                            room.Conclude();
                            room.Status = MiniGamesAPI.Enum.RoomStatus.Waiting;
                            for (int i = room.Players.Count; i>=0; i--)
                            {
                                var rplr = room.Players[i];
                                rplr.Leave();
                                rplr.SendInfoMessage("房间被强制删除");
                            }
                            ConfigUtils.rooms.Remove(room);
                            ConfigUtils.RemoveSingleRoom(room);
                            plr.SendInfoMessage($"成功删除房间(ID：{room.ID})");
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "start":
                    if (args.Parameters.Count != 2)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.Start();
                            room.Status = MiniGamesAPI.Enum.RoomStatus.Waiting;
                            plr.SendInfoMessage($"成功开启房间(ID：{room.ID})");
                            ConfigUtils.UpdateSingleRoom(room);
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "stop":
                    if (args.Parameters.Count != 2)
                    {
                        plr.SendInfoMessage("指令不正确");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1], out id))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room != null)
                        {
                            room.Stop();
                            room.Conclude();
                            room.Restore();
                            room.Status = MiniGamesAPI.Enum.RoomStatus.Waiting;
                            for (int i = room.Players.Count-1; i >= 0; i--)
                            {
                                var rplr = room.Players[i];
                                rplr.Leave();
                                rplr.SendInfoMessage("房间被强制停止");
                            }
                            room.Status = MiniGamesAPI.Enum.RoomStatus.Stopped;
                            ConfigUtils.UpdateSingleRoom(room);
                            plr.SendInfoMessage($"成功停止房间(ID：{room.ID})");
                        }
                        else
                        {
                            plr.SendInfoMessage("房间不存在");
                        }
                    }
                    else
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "help":
                default:
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;
                    List<string> lines = new List<string>();
                    lines.Add("/zma create 房间名 创建房间");
                    lines.Add("/zma newpack [背包名] [玩家名] 创建背包");
                    lines.Add("/zma remove [房间ID] 移除房间");
                    lines.Add("/zma start [房间ID] 开启房间");
                    lines.Add("/zma stop [房间ID] 关闭房间");
                    lines.Add("/zma sgt [房间ID] [时间] 设置游戏时间(单位:秒)");
                    lines.Add("/zma swt [房间ID] [时间] 设置等待时间(单位:秒)");
                    lines.Add("/zma sst [房间ID] [时间] 设置选择时间(单位:秒)");
                    lines.Add("/zma srp [房间ID] [背包ID] 设置母体背包");
                    lines.Add("/zma snp [房间ID] [背包ID] 设置普通僵尸背包");
                    lines.Add("/zma shp [房间ID] [背包ID] 设置人类背包");
                    lines.Add("/zma svp [房间ID] [背包ID] 设置观战者背包");
                    lines.Add("/zma smp [房间ID] [玩家数] 设置最大玩家数");
                    lines.Add("/zma sdp [房间ID] [玩家数] 设置最小玩家数");
                    lines.Add("/zma respawntime [房间ID] [时间] 设置重生时间(单位:秒)");
                    lines.Add("/zma reloadpacks 重载背包数据");
                    lines.Add("/zma src [房间ID] [母体个数] 设置母体个数");
                    lines.Add("/zma sp 选取临时点");
                    lines.Add("/zma swp [房间ID] 设置房间的等待点");
                    lines.Add("/zma addp [房间ID] 添加出生点");
                    PaginationTools.Settings settings= new PaginationTools.Settings();
                    settings.FooterFormat = $"输入/zma help {pageNumber+1} 查看更多指令";
                    settings.HeaderFormat = "生化模式管理员指令";
                    PaginationTools.SendPage(plr,pageNumber,lines,settings);
                    break;
                case "reloadpacks":
                    ConfigUtils.ReloadPacks();
                    args.Player.SendInfoMessage("背包重载成功");
                    break;
            }
        }
        private void ZM(CommandArgs args)
        {
            var plr = ConfigUtils.GetPlayerByName(args.Player.Name);
            ZRoom room = null;
            int id;
            StringBuilder board = new StringBuilder();
            if (plr==null)
            {
                args.Player.SendInfoMessage("数据出错，请尝试重新进入服务器");
                return;
            }
            if (args.Parameters.Count<1)
            {
                args.Player.SendInfoMessage("请输入/zm help 查看帮助");
                return;
            }
            switch (args.Parameters[0])
            {
                case "join":
                    if (args.Parameters.Count!=2)
                    {
                        plr.SendInfoMessage("正确指令：/zm join [房间号]");
                        return;
                    }
                    if (int.TryParse(args.Parameters[1],out id))
                    {
                        room = ConfigUtils.GetRoomByID(id);
                        if (room!=null)
                            plr.Join(room);
                        else
                            plr.SendInfoMessage("房间不存在");
                    }
                    else 
                    {
                        plr.SendInfoMessage("请输入数字");
                    }
                    break;
                case "leave":
                    plr.Leave();
                    break;
                case "ready":
                    room = ConfigUtils.GetRoomByID(plr.CurrentRoomID);
                    if (room!=null&&plr.Status==MiniGamesAPI.Enum.PlayerStatus.Waiting)
                    {
                        plr.Ready();
                        room.Broadcast($"玩家[{plr.Name}]{(plr.IsReady?"已准备":"未准备")}",Color.Gold);
                    }
                    else
                    {
                        plr.SendInfoMessage("房间不存在或当前状态不允许准备");
                    }
                    break;
                case "list":
                    foreach (var tempRoom in ConfigUtils.rooms)
                    {
                        board.AppendLine($"[{tempRoom.ID}] [{tempRoom.Name}] [{tempRoom.GetPlayerCount()}/{tempRoom.MaxPlayer}] [{tempRoom.Status.ToString()}]");
                    }
                    plr.SendInfoMessage(board.ToString());
                    break;
                case "help":
                default:    
                    board.AppendLine("/zm join [房间号] 加入房间");
                    board.AppendLine("/zm leave 离开当前房间");
                    board.AppendLine("/zm ready 准备/未准备");
                    board.AppendLine("/zm list 查看房间列表");
                    plr.SendInfoMessage(board.ToString());
                    break;
            }
        }
        private void OnChat(ServerChatEventArgs args)
        {
            if (args.Text.StartsWith(TShock.Config.Settings.CommandSilentSpecifier)||args.Text.StartsWith(TShock.Config.Settings.CommandSpecifier)) return;
            var tsplr = TShock.Players[args.Who];
            var plr = ConfigUtils.GetPlayerByName(tsplr.Name);
            if (plr!=null&&plr.CurrentRoomID!=0)
            {
                var room = ConfigUtils.GetRoomByID(plr.CurrentRoomID);
                if (room != null) 
                {
                    room.Broadcast($"[房内聊天][{(plr.Character==ZEnum.Zomble?"丧尸":"人类")}][{(plr.IsDead?"阵亡":"存活")}]{plr.Name}:{args.Text}",Color.LightPink);
                    args.Handled = true;
                }
            }

        }
        private void OnNewProjectile(object sender, GetDataHandlers.NewProjectileEventArgs args)
        {
            var plr = ConfigUtils.GetPlayerByName(args.Player.Name);
            ZRoom room = null;
            if (plr != null)
            {
                room = ConfigUtils.GetRoomByID(plr.CurrentRoomID);
                if (room != null && room.Status == MiniGamesAPI.Enum.RoomStatus.Gaming)
                {
                    if (plr.BeLoaded && args.Player.TPlayer.HeldItem.ranged && plr.Character == ZEnum.Human)
                    {
                        if (plr.BulletAmount == 0)
                        {
                            plr.BeLoaded = true;
                            Terraria.Main.projectile[args.Index].active = false;
                            TSPlayer.All.SendData(PacketTypes.ProjectileDestroy,"",args.Index);
                            plr.SendErrorMessage("上膛中...");
                            if (!plr.BulletTimer.Enabled)
                            {
                                plr.BulletTimer.Start();
                            }
                        }
                        else
                        {
                            plr.BulletAmount -= 1;
                        }
                    }
                }
            }
        }
        private void OnChangePVP(object sender, GetDataHandlers.TogglePvpEventArgs args)
        {
            var plr = ConfigUtils.GetPlayerByName(args.Player.Name);
            if (plr!=null&&plr.Status==MiniGamesAPI.Enum.PlayerStatus.Gaming)
            {
                if (plr.IsDead)
                    plr.SetPVP(false);
                else
                    plr.SetPVP(true) ;
                args.Handled = true;
            }
        }
        private void OnChangeTeam(object sender, GetDataHandlers.PlayerTeamEventArgs args)
        {
            var plr = ConfigUtils.GetPlayerByName(args.Player.Name);
            if (plr!=null&&plr.Status==MiniGamesAPI.Enum.PlayerStatus.Gaming)
            {
                if (plr.Character==ZEnum.Human)
                {
                    plr.SetTeam(3);
                }
                else
                {
                    if (plr.IsDead)
                        plr.SetTeam(0);
                    else 
                        plr.SetTeam(1);
                    
                }
                args.Handled = true;
            }
        }
        private void OnPlayerSpawn(object sender, GetDataHandlers.SpawnEventArgs args)
        {
            var plr = ConfigUtils.GetPlayerByName(args.Player.Name);
            ZRoom room = null;
            if (plr!=null)
            {
                //plr.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld,1);
                room = ConfigUtils.GetRoomByID(plr.CurrentRoomID);
                if (room!=null)
                {
                    if (room.Status==MiniGamesAPI.Enum.RoomStatus.Gaming)
                    {
                        var rand = new Terraria.Utilities.UnifiedRandom();
                        var pack = ConfigUtils.GetPackByID(plr.SelectPackID);
                        plr.Teleport(room.SpawnPoints[rand.Next(0,room.SpawnPoints.Count-1)]);
                        plr.SendInfoMessage("已重生！");
                        plr.SetTeam(1);
                        if (pack != null) pack.RestoreCharacter(plr);
                        if (plr.IsDead)
                        {
                            plr.SetPVP(false);
                            plr.SetTeam(0);
                            plr.BulletTimer.Start();
                            plr.SendInfoMessage("已进入观战模式");
                        }
                        
                    }
                    else if(room.Status==MiniGamesAPI.Enum.RoomStatus.Selecting)
                    {
                        plr.Teleport(room.LobbyPoint);
                        plr.SetTeam(0);
                        plr.BackUp.RestoreCharacter(plr);
                        plr.SendInfoMessage("已将你送回等待房间");

                    }
                    else if(room.Status==MiniGamesAPI.Enum.RoomStatus.Waiting)
                    {
                        plr.Teleport(room.LobbyPoint);
                        plr.SetPVP(false);
                        plr.SetTeam(0);
                        plr.BackUp.RestoreCharacter(plr);
                        plr.SendInfoMessage("已将你送回等待房间");
                    }
                    args.Handled = true;
                }
            }
        }
        private void OnOpenChest(object sender, GetDataHandlers.ChestOpenEventArgs args)
        {
            var plr = ConfigUtils.GetPlayerByName(args.Player.Name);
            if (plr!=null&&plr.CurrentRoomID!=0)
            {
                plr.SendInfoMessage("游戏中不可打开箱子");
                args.Handled = true;
            }

        }
        private void OnKillMe(object sender,GetDataHandlers.KillMeEventArgs args)
        {
            TSPlayer other=null;
            if (args.PlayerDeathReason._sourcePlayerIndex!=-1) other = TShock.Players[args.PlayerDeathReason._sourcePlayerIndex];
            var zother = ConfigUtils.GetPlayerByName(other.Name);
            var plr = ConfigUtils.GetPlayerByName(args.Player.Name);
            ZRoom room = ConfigUtils.GetRoomByID(plr.CurrentRoomID);
            if (room == null) return;
            if (args.Pvp&&room.Status==MiniGamesAPI.Enum.RoomStatus.Gaming)
            {
                zother = ConfigUtils.GetPlayerByName(other.Name);
                plr.Player.RespawnTimer = room.RespawnTime;
                if (zother.Character == ZEnum.Zomble && plr.Character == ZEnum.Human)//己人敌尸
                {
                    plr.Character = ZEnum.Zomble;
                    plr.SelectPackID = room.NormalPackID;
                    room.Broadcast($"{plr.Name} 被 {zother.Name} 给挠了,变成了僵尸！", Color.Crimson);
                }
                if (zother.Character == ZEnum.Human && plr.Character == ZEnum.Zomble)//己尸敌人
                {
                    if (zother.Player.ItemInHand.ranged)
                    {
                        plr.IsDead = true;
                        plr.SelectPackID = room.ViewerPackID;
                        room.Broadcast($"{zother.Name} 把 {plr.Name} 给刀了！勇气可嘉", Color.MediumAquamarine);
                    }
                    else
                    {
                        room.Broadcast($"{zother.Name} 把 {plr.Name} 枪毙了！", Color.MediumAquamarine);
                    }
                }
                args.Handled = true;
            }else if (!args.Pvp && room.Status == MiniGamesAPI.Enum.RoomStatus.Waiting) 
            {
                plr.Player.RespawnTimer = room.RespawnTime;
                plr.Character = ZEnum.Human;
                plr.BackUp.RestoreCharacter(plr);
                args.Handled = true;
            }
            else if(!args.Pvp&&room.Status==MiniGamesAPI.Enum.RoomStatus.Selecting)
            {
                plr.Player.RespawnTimer = room.RespawnTime;
                plr.IsDead = true;
                plr.SelectPackID = room.ViewerPackID;
                args.Handled = true;
            }
            else if (!args.Pvp && room.Status == MiniGamesAPI.Enum.RoomStatus.Gaming)
            {
                plr.Player.RespawnTimer = room.RespawnTime;
                //plr.IsDead = true;
                plr.SelectPackID = room.NormalPackID;
                plr.Character = ZEnum.Zomble;
                plr.SendInfoMessage("未知原因死亡，重生后将变成丧尸");
                args.Handled = true;
            }

            /*if (plr!=null&&zother!=null&&plr.CurrentRoomID!=0&&plr.CurrentRoomID==zother.CurrentRoomID)
            {
                room = ConfigUtils.GetRoomByID(plr.CurrentRoomID);
                if (room!=null&&room.Status==MiniGamesAPI.Enum.RoomStatus.Gaming)
                {
                    plr.Player.RespawnTimer = room.RespawnTime;
                    if (zother.Character==ZEnum.Zomble&&plr.Character==ZEnum.Human)//己人敌尸
                    {
                        plr.Character = ZEnum.Zomble;
                        plr.SelectPackID = room.NormalPackID;
                        room.Broadcast($"{plr.Name} 被 {zother.Name} 给挠了,变成了僵尸！",Color.Crimson);
                    }
                    if (zother.Character == ZEnum.Human && plr.Character == ZEnum.Zomble)//己尸敌人
                    {
                        if (!zother.Player.ItemInHand.ranged)
                        {
                            plr.IsDead = true;
                            plr.SelectPackID = room.ViewerPackID;
                            room.Broadcast($"{zother.Name} 把 {plr.Name} 给刀了！勇气可嘉",Color.MediumAquamarine);
                        }
                        else 
                        {
                            room.Broadcast($"{zother.Name} 把 {plr.Name} 枪毙了！", Color.MediumAquamarine);
                        }
                    }
                    args.Handled = true;
                }
            } */
        }
        private void OnLeave(LeaveEventArgs args)
        {
            var tsplr = TShock.Players[args.Who];
            try
            {
                var plr = ConfigUtils.GetPlayerByName(tsplr.Name);
                if (plr != null)
                {
                    var room = ConfigUtils.GetRoomByID(plr.CurrentRoomID);
                    if (room != null)
                    {
                        room.Players.Remove(plr);
                        room.Broadcast($"玩家[{tsplr.Name}]强制退出了房间", Color.DarkTurquoise);
                    }
                    plr.BackUp.RestoreCharacter(plr);
                    plr.BackUp = null;
                    plr.Player = null;
                }
            }
            catch (Exception)
            {
                TShock.Log.ConsoleInfo($"玩家 [{tsplr.Name}] 退出服务器时出错");
            }
            

        }
        private void OnJoin(GreetPlayerEventArgs args)
        {
            var tsplr = TShock.Players[args.Who];
            var plr = ConfigUtils.GetPlayerByName(tsplr.Name);
            if (plr==null)
            {
                plr = new ZPlayer(ConfigUtils.players.Count+1,tsplr);
                ConfigUtils.players.Add(plr);
                ConfigUtils.UpdatePlayers();
            }
            plr.Player = tsplr;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
                GetDataHandlers.KillMe -= OnKillMe;
                GetDataHandlers.ChestOpen -= OnOpenChest;
                GetDataHandlers.NewProjectile -= OnNewProjectile;
                GetDataHandlers.PlayerSpawn -= OnPlayerSpawn;
                GetDataHandlers.TogglePvp -= OnChangePVP;
                GetDataHandlers.PlayerTeam -= OnChangeTeam;
            }
            base.Dispose(disposing);
        }
    }
}
