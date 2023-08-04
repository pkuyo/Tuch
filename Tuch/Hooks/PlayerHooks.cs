using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using RWCustom;
using Smoke;
using UnityEngine;
using Random = UnityEngine.Random;
using static Tuch.GhostPlayerImports;

namespace Tuch.Hooks
{
    static class PlayerHooks
    {
        public static void OnModsInit()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.RoomCamera.ChangeRoom += RoomCamera_ChangeRoom;
            On.RoomCamera.Update += RoomCamera_Update;
        }

        private static void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            orig(self);
            if (voidChains != null)
            {
                chainsActiveTime += 1;
                for (int i = 0; i < voidChains.Length; i++)
                {
                    voidChains[i].proximityAlpha = 0.8f;
                    
                    voidChains[i].colorFlash =
                        Mathf.Sin(3.1415927f * (chainsActiveTime / 400f) - 3.1415927f) * 0.25f + 0.25f +
                        0.5f * Mathf.Min(1f, chainsActiveTime / 80f);

                }
            }
        }

        private static void RoomCamera_ChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
        {
            if (gameStart)
            {
                Plugin.Log("Change room in game");
                OnGameStop(self.room);
            }
            orig(self, newRoom, cameraPosition);
        }

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            gameStart = false;
            inGamePlayer = null;
        }


        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self,abstractCreature,world);
            if(!modules.TryGetValue(self,out _) && !self.isNPC)
                modules.Add(self,new PlayerModule(self));
        }

        public static void StartGameCommand(string[] str)
        {
            if (str[0] == "/xgame" && !gameStart && (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                && game.Players[0].realizedCreature is Player player && modules.TryGetValue(player,out var module))
            {
                module.LocalStartGame();
            }
                
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if(modules.TryGetValue(self,out var module))
                module.Update();
        }

        public static void OnGameStart(Room room)
        {
            if (!gameStart)
            {
                gameStart = true;
                int num = 0;
                for (int j = 0; j < room.shortcuts.Length; j++)
                    if (room.shortcuts[j].shortCutType == ShortcutData.Type.RoomExit && room.abstractRoom.connections[room.shortcuts[j].destNode] >= 0)
                        num++;


                voidChains = new VoidChain[num - 1];
                int index = 0;
                Vector2? spawnPosB = null;
                inGamePlayer = room.abstractRoom.creatures.Where(k => k.realizedCreature is Player player &&
                                                                      !player.isNPC).Select(i=> i.realizedCreature as Player).ToList();
                for (int k = 0; k < room.shortcuts.Length; k++)
                {
                    if (room.shortcuts[k].shortCutType == ShortcutData.Type.RoomExit && room.abstractRoom.connections[room.shortcuts[k].destNode] >= 0)
                    {
                        Vector2 spawnPosA = room.MiddleOfTile(room.shortcuts[k].StartTile) + IntVector2.ToVector2(room.ShorcutEntranceHoleDirection(room.shortcuts[k].StartTile)) * 15f;
                        room.lockedShortcuts.Add(room.shortcuts[k].StartTile);
                        if (spawnPosB.HasValue)
                        {
                            VoidChain voidChain = new VoidChain(room, spawnPosA, spawnPosB.Value);
                            room.AddObject(voidChain);
                            voidChains[index] = voidChain;
                            index++;
                        }
                        spawnPosB = spawnPosA;
                    }
                }
                room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Lock, 0f, 1f, Random.value * 0.5f + 0.8f);
                room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Lock, 0f, 1f, Random.value * 0.5f + 0.8f);
            }

        }

        public static void OnGameStop(Room room)
        {
            if (gameStart)
            {
                gameStart = false;
                room.lockedShortcuts.Clear();
                foreach (var chain in voidChains)
                {
                    chain.Destroy();
                    chain.RemoveFromRoom();
                    room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Break, 0f, 1f, Random.value * 0.5f + 0.95f);
                    room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Break, 0f, 1f, Random.value * 0.5f + 0.95f);
                }

                voidChains = null;
                inGamePlayer = null;
            }
        }

        private static VoidChain[] voidChains;
        public static bool gameStart;
        public static List<Player> inGamePlayer;
        public static int chainsActiveTime;
        internal static ConditionalWeakTable<Player, PlayerModule> modules = new ConditionalWeakTable<Player, PlayerModule>();
        public class PlayerModule
        {
            public WeakReference<Player> playerRef;
            public bool isGhost;
            public int counter;
            FireSmoke bugSmoke;
            private const int maxCounter = 20 * 40;

          
            public PlayerModule(Player player)
            {
                playerRef = new WeakReference<Player>(player);
            }

            public void RemoteUpdate(Player player)
            {
                //网络同步下来的玩家只进行显示

                //尝试获取网络数据 如果存在则更新
                if (TryGetValueForPlayer(player, typeof(TuchData), out var data))
                {
                    isGhost = (data as TuchData).isGhost;
                    counter = (data as TuchData).counter * 40;
                }

                //接收到事件 山芋炸了x
                if (TryGetImportantValue(typeof(TCPTuchData), out var rawData) && rawData is TCPTuchData tcpData)
                    if (tcpData.state == 2 && tcpData.nextGhost == GetPlayerNetID(player))
                        Burst();
            }

            public void LocalUpdate(Player player)
            {
                if (player.room != null)
                {
                    if (isGhost && player.Consious)
                    {
                        //本地玩家 而且有山芋（？）的情况下判断是否抓到人
                        foreach (var creature in inGamePlayer)
                        {
                            if(creature == player) continue;
                            //抓到人
                            if (Custom.DistLess(player.DangerPos, creature.DangerPos, 30))
                            {
                                //传送 传递山芋事件 下一个抓山芋的玩家的玩家ID
                                //TrySendImportantValue的信息只会发送给全体玩家一次
                                TrySendImportantValue(new TCPTuchData()
                                { nextGhost = GetPlayerNetID(creature) });

                                //更改 玩家的状态数据
                                //TrySetValueForPlayer的状态数据如果在不更改情况下会保持原状态发送
                                isGhost = false;
                                TrySetValueForPlayer(player, new TuchData() { isGhost = isGhost });
                                SendMessage(player, $"Catch : {GetPlayerNetName(creature)}");

                                break;
                            }
                            TrySetValueForPlayer(player, new TuchData() { isGhost = isGhost,counter = (byte)(counter/40)});
                        }
                        counter--;

                        if (counter == 0)
                        {
                            //传送 传递你菜到爆炸了的事件.jpg 下一个抓山芋的玩家的玩家ID
                            //TrySendImportantValue的信息只会发送给全体玩家一次
                            TrySendImportantValue(new TCPTuchData()
                            { nextGhost = GetPlayerNetID(player), state = 2 });
                            TrySetValueForPlayer(player, new TuchData() { isGhost = false, counter = (byte)(counter / 40) });
                            SendMessage(player, $"Boom : {GetPlayerNetName(player)}");
                            Burst();
                            OnGameStop(player.room);
                        }
                    }

                    //接收到事件 
                    if (TryGetImportantValue(typeof(TCPTuchData), out var rawData) && rawData is TCPTuchData tcpData)
                    {
                        //很不幸你被抓了----
                        if (tcpData.nextGhost == GetPlayerNetID(player))
                        {
                            isGhost = true;
                            TrySetValueForPlayer(player, new TuchData() { isGhost = isGhost ,counter = maxCounter/40});
                            counter = maxCounter;
                            player.Stun(100);
                            Plugin.Log("LOL you have been caught");
                        }
                        //游戏开始
                        if (tcpData.state == 1)
                            OnGameStart(player.room);

                        //游戏结束
                        else if (tcpData.state == 2)
                            OnGameStop(player.room);

                    }

                }
            }

            public void RenderUpdate(Player player)
            {
                if (player.room != null && player.room.ViewedByAnyCamera(player.firstChunk.pos, 300f) && bugSmoke != null)
                {
                    bugSmoke.Update(false);
                    bugSmoke.EmitSmoke(player.firstChunk.pos, Custom.RNV(), Custom.HSL2RGB(
                            Custom.LerpMap(counter, 20 * 40, 0, 144, 0) / 360f, 1f, 0.5f),
                        (int)Custom.LerpMap(counter, 20 * 40, 0, 25f, 40f));
                }
                if (isGhost && player.room != null &&
                    (player.room != bugSmoke?.room))
                {
                    bugSmoke?.Destroy();
                    player.room.AddObject(bugSmoke = new FireSmoke(player.room));
                }

                if (!isGhost)
                {
                    bugSmoke?.Destroy();
                    bugSmoke = null;
                }
            }
            public void Update()
            {
                if (!playerRef.TryGetTarget(out var player) || !IsConnected())
                    return;
           
                if (IsNetworkPlayer(player))
                {
                    RemoteUpdate(player);
                }
                else
                {
                    LocalUpdate(player);
                }
                RenderUpdate(player);
            }

            public void LocalStartGame()
            {
                if (!playerRef.TryGetTarget(out var player))
                    return;
                isGhost = true;
                TrySetValueForPlayer(player, new TuchData() { isGhost = isGhost });
                counter = maxCounter;
                TrySendImportantValue(new TCPTuchData() { nextGhost = GetPlayerNetID(player), state = 1 });
                OnGameStart(player.room);
                SendMessage(player, $"Game Start :{GetPlayerNetName(player)}");
            }

            public void Burst()
            {
                if (playerRef.TryGetTarget(out var self))
                {
                    self.room.AddObject(new SootMark(self.room, self.DangerPos, 80f, true));
                    self.room.AddObject(new Explosion(self.room, self, self.DangerPos, 7, 250f, 2.2f, 0f, 100f, 0.15f, null, 0.7f, 40f, 1f));
                    self.room.AddObject(new Explosion.ExplosionLight(self.DangerPos, 280f, 1f, 7, Color.white));
                    self.room.AddObject(new Explosion.ExplosionLight(self.DangerPos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                    self.room.AddObject(new ExplosionSpikes(self.room, self.DangerPos, 14, 30f, 9f, 7f, 170f, Color.white));
                    self.room.AddObject(new ShockWave(self.DangerPos, 330f, 0.045f, 5, false));

                    for (int j = 0; j < 70; j++)
                        self.room.AddObject(new SporeCloud(self.DangerPos, Custom.RNV() * Random.value * 10f,
                        Color.Lerp(Color.red, Color.yellow, Random.Range(0.3f, 0.7f)), 1f, null, j % 20, null));

                    if(!IsNetworkPlayer(self))
                        self.Die();
                }
                  
            }

        }


    }
}
