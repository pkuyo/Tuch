using BepInEx;
using System;
using System.Security.Permissions;
using MonoMod.ModInterop;
using Tuch.Hooks;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace Tuch
{
    [BepInPlugin("tuch", "Tuch", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        void OnEnable()
        {
            try
            {
                typeof(GhostPlayerImports).ModInterop();
               
                

                if (enableGhostPlayer)
                {
                    GhostPlayerImports.Register(typeof(TuchData));
                    GhostPlayerImports.Register(typeof(TCPTuchData));
                    GhostPlayerImports.RegisterCommandEvent(PlayerHooks.StartGameCommand);   
                }

                On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            }
            catch (Exception e)
            {
                Logger.LogFatal(e);
            }
      

        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            enableGhostPlayer = GhostPlayerImports.Register != null;
            if (enableGhostPlayer)
            {
                PlayerHooks.OnModsInit();
                PlayerGraphicsHooks.OnModsInit();
            }

            HudHooks.OnModsInit();
        }

        public static void Log(string m)
        {
            Debug.Log("[Tuch] " + m);
        }

        public static void Log(string f, params object[] args)
        {
            Debug.Log("[Tuch] " + string.Format(f, args));
        }

        private static bool enableGhostPlayer = false;
    }


    [ModImportName("GhostPlayerExtension")]
    public static class GhostPlayerImports
    {
        public delegate bool TryGetImportantValueDel(Type type, out object obj);
        public delegate bool TryGetValueForPlayerDel(Player player,Type type, out object obj);

        public static Func<Type, bool> Register;

        public static TryGetValueForPlayerDel TryGetValueForPlayer;
        public static Func<Player, object, bool> TrySetValueForPlayer;

        public static TryGetImportantValueDel TryGetImportantValue;
        public static Func<object,bool , bool> TrySendImportantValue;

        public static Func<Player, string, bool> SendMessage;
        public static Action<Action<string[]>> RegisterCommandEvent;

        public static Func<Player, int> GetPlayerNetID;
        public static Func<Player, string> GetPlayerNetName;
        public static Func<Player, bool> IsNetworkPlayer;
        public static Func<bool> IsConnected;

    }

    public class TuchData
    {
        public bool isGhost;
        public byte counter;
     

    }
    public class TCPTuchData
    {
        public int nextGhost;
        public byte state; // burst : 2 startgame : 1
        public byte maxCount;
        public byte countDown;
    }
}
