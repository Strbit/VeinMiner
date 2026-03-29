using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace MyPlugin1
{
    [ApiVersion(2, 1)]
    public class VeinMiner : TerrariaPlugin
    {
        public override string Name => "VeinMiner";
        public override Version Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;    
        public override string Author => "Megghy|YSpoof|strbit"; 
        public override string Description => "VeinMiner by Megghy but with TShock 6 support!";
        internal static Config Config = new();
        public VeinMiner(Main game) : base(game)
        {

        }

        public override void Initialize()
        {
            Config.Load();
            Commands.ChatCommands.Add(new Command(
                permissions: "veinminer",
                cmd: delegate (CommandArgs args)
                {
                    var tsp = args.Player;
                    var result = tsp.GetData<VMStatus>("VeinMiner");
                    if (args.Parameters.Count >= 1)
                    {
                        result.EnableBroadcast = !result.EnableBroadcast;
                        tsp.SendMessage($"[c/95CFA6:<VeinMiner> 采矿提示 {(result.EnableBroadcast ? "已开启" : "已关闭")}.]", Color.White);
                    }
                    else
                    {
                        result.Enable = !result.Enable;
                        tsp.SendMessage($"[c/95CFA6:<VeinMiner> {(result.Enable ? "连锁挖矿 已开启" : "连锁挖矿 已关闭| 要仅关闭挖矿提示，请使用: /vm msg")}.]", Color.White);
                    }
                },
                "veinminer", "chain mining", "vm")
            {
                AllowServer = false
            });
            GetDataHandlers.TileEdit += OnTileEdit;
            TShockAPI.Hooks.GeneralHooks.ReloadEvent += Config.Load;
            ServerApi.Hooks.ServerJoin.Register(this, OnPlayerJoin);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GetDataHandlers.TileEdit -= OnTileEdit;
                TShockAPI.Hooks.GeneralHooks.ReloadEvent -= Config.Load;
                ServerApi.Hooks.ServerJoin.Deregister(this, OnPlayerJoin);
            }
            base.Dispose(disposing);
        }
        class VMStatus
        {
            public bool Enable = true;
            public bool EnableBroadcast = true;
        }
        void OnPlayerJoin(JoinEventArgs args)
        {
            if (TShock.Players[args.Who] is { } plr)
            {
                plr.SetData("VeinMiner", new VMStatus());
            }
        }
        void OnTileEdit(object o, GetDataHandlers.TileEditEventArgs args)
        {
            if (Main.tile[args.X, args.Y] is { } tile && args.Player.HasPermission("veinminer") && Config.Enable && args.Player.GetData<VMStatus>("VeinMiner").Enable && Config.Tile.Contains(tile.type) && args.Action == GetDataHandlers.EditAction.KillTile && args.EditData == 0)
            {
                args.Handled = true;
                Mine(args.Player, args.X, args.Y, tile.type);
            }
        }

        void Mine(TSPlayer plr, int x, int y, int type)
        {
            var list = GetVein(new(), x, y, type).Result;
            var count = list.Count;

            // 👉 汇总整条矿脉掉落
            List<Terraria.Item> allDrops = new();

            foreach (var p in list)
            {
                allDrops.AddRange(MyPlugin1.Utils.GetItemsFromTile(p.X, p.Y));
            }

            // ✅ 合并同类物品（关键修改）
            var grouped = allDrops
                .GroupBy(i => i.type)
                .Select(g => new
                {
                    Name = g.First().Name,
                    Stack = g.Sum(x => x.stack)
                });

            // 👉 生成显示文本（优化后）
            string dropText = grouped.Any()
                ? string.Join(", ", grouped.Select(g => $"{g.Stack} {g.Name}"))
                : "Unknown";

            // 👉 取一个代表物品（用于背包检测等）
            var firstItem = allDrops.FirstOrDefault();

            if (Config.Exchange.Where(e => e.Type == type && count >= e.MinSize).ToList() is { Count: > 0 } exchangeList)
            {
                foreach (var e in exchangeList)
                {
                    if (e.Item.Count <= plr.GetBlankSlot())
                    {
                        foreach (var ex in e.Item)
                            plr.GiveItem(ex.Key, ex.Value);

                        if (e.OnlyGiveItem)
                            KillTileAndSend(list, true);
                        else
                            GiveItem();

                        plr.SendMessage(
                            $"[c/95CFA6:<VeinMiner>] Mined [c/95CFA6:{count} tiles → {dropText}].",
                            Color.White
                        );
                        return;
                    }
                    else
                    {
                        plr.SendInfoMessage(
                            $"[c/95CFA6:<VeinMiner>] Inventory full, space needed: [c/95CFA6:{e.Item.Count}] ."
                        );
                        plr.SendTileSquareCentered(x, y, 1);
                        return;
                    }
                }
            }
            else
            {
                GiveItem();
            }
        

            void GiveItem()
            {
                if (Config.PutInInventory)
                {
                    if (firstItem != null && plr.IsSpaceEnough(firstItem.type, firstItem.stack * count))
                    {
                        // 👉 发放所有掉落（推荐）
                        foreach (var item in allDrops)
                        {
                            plr.GiveItem(item.type, item.stack);
                        }

                        KillTileAndSend(list, true);
                    }
                    else
                    {
                        WorldGen.KillTile(x, y);

                        plr.SendInfoMessage(
                            $"[c/95CFA6:<VeinMiner>] Inventory full, unable to insert mined items."
                        );
                    }
                }
                else
                {
                    KillTileAndSend(list, false);
                }

                if (plr.GetData<VMStatus>("VeinMiner").EnableBroadcast && Config.Broadcast && count > 1)
                {
                    plr.SendMessage(
                        $"[c/95CFA6:<VeinMiner>] 尝试开采 [c/95CFA6:{dropText}].",
                        Color.White
                    );
                }
            }
        }
        
        public static void KillTileAndSend(List<Point> list, bool noItem)
        {
            Task.Run(() =>
            {
                if (!list.Any())
                    return;

                list.ForEach(p =>
                {
                    WorldGen.KillTile(p.X, p.Y, false, false, noItem);
                    NetMessage.SendData(17, -1, -1, null, 4, p.X, p.Y, false.GetHashCode());
                });
            });
        }

        public static Task<List<Point>> GetVein(List<Point> list, int x, int y, int type)
        {
            return Task.Run(() =>
            {
                if (!list.Any(p => p.Equals(new Point(x, y))) && Main.tile[x, y] is { } tile && tile.active() && tile.type == type)
                {
                    if (list.Count > 5000) return list;
                    list.Add(new(x, y));
                    list = GetVein(list, x + 1, y, type).Result;
                    list = GetVein(list, x - 1, y, type).Result;
                    list = GetVein(list, x, y + 1, type).Result;
                    list = GetVein(list, x, y - 1, type).Result;
                    list = GetVein(list, x + 1, y + 1, type).Result;
                    list = GetVein(list, x + 1, y - 1, type).Result;
                    list = GetVein(list, x - 1, y + 1, type).Result;
                    list = GetVein(list, x - 1, y - 1, type).Result;
                }
                return list;
            });
        }
    }
}
