using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Tuch.Hooks
{
    static class HudHooks
    {
        public static void OnModsInit()
        {
            //On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
            On.RoomCamera.FireUpSinglePlayerHUD += RoomCamera_FireUpSinglePlayerHUD;
            Debug.Log("HUDHook on");
        }

        private static void RoomCamera_FireUpSinglePlayerHUD(On.RoomCamera.orig_FireUpSinglePlayerHUD orig, RoomCamera self, Player player)
        {
            orig.Invoke(self, player);
            self.hud.AddPart(new GameHUD(self.hud, self, player));
        }
    }

    public class GameHUD : HudPart
    {
        static readonly float textRectSize = 25f;
        RoomCamera cam;
        WeakReference<Player> playerRef;

        FLabel digiTen_1;//1=smaller
        FLabel digiTen_2;//2=bigger

        FLabel digiSingle_1;
        FLabel digiSingle_2;

        int counter;
        int lastCounter;
        int syncGoalCounter;
        float smoothCounter;
        bool synced = true;

        #region display
        bool reval;
        
        int lastIndex = -1;
        bool scrollDigiTen;

        float alpha;
        float lastSoundIndex;

        public GameHUD(HUD.HUD hud, RoomCamera roomCamera,Player player) : base(hud)
        {
            cam = roomCamera;
            playerRef = new WeakReference<Player>(player);

            counter = 40 * 27;
            lastCounter = counter;
            lastSoundIndex = float.MaxValue;

            digiTen_1 = GetNewDigiLabel();
            hud.fContainers[0].AddChild(digiTen_1);

            digiTen_2 = GetNewDigiLabel();
            hud.fContainers[0].AddChild(digiTen_2);

            digiSingle_1 = GetNewDigiLabel();
            hud.fContainers[0].AddChild(digiSingle_1);

            digiSingle_2 = GetNewDigiLabel();
            hud.fContainers[0].AddChild(digiSingle_2);
        }

        #endregion

        public override void Update()
        {
            base.Update();

            if (reval)
            {
                lastCounter = counter;
                counter -= InternalGetTickStep(counter, syncGoalCounter);
                syncGoalCounter--;
            }
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            smoothCounter = Mathf.Lerp(lastCounter / 40f, counter / 40f, timeStacker);
            UpdateLabels(Mathf.Max(0f, smoothCounter), timeStacker);
            if (smoothCounter <= 0f)
                StopTimer();
        }

        public override void ClearSprites()
        {
            digiTen_1.isVisible = false;
            digiTen_1.RemoveFromContainer();

            digiTen_2.isVisible = false;
            digiTen_2.RemoveFromContainer();

            digiSingle_1.isVisible = false;
            digiSingle_1.RemoveFromContainer();

            digiSingle_2.isVisible = false;
            digiSingle_2.RemoveFromContainer();
        }

        void UpdateLabels(float floatIndex,float timeStacker)
        {
            int intIndex = Mathf.FloorToInt(floatIndex);
            if(intIndex != lastIndex)
            {
                lastIndex = intIndex;
                
                InternalUpdateTextAndScrollMode(intIndex, lastIndex);
            }

            float freq = InternalGetFreq(floatIndex);
            if ((lastSoundIndex - floatIndex) >= (1f / freq))
            {
                lastSoundIndex = floatIndex;
                if (playerRef.TryGetTarget(out var player))
                    cam.room.PlaySound(SoundID.Gate_Clamp_Lock, player.mainBodyChunk, false, 1f, 40f + Random.value);
            }

            InternalUpdateScroll(intIndex, lastIndex, floatIndex, timeStacker);
        }

        float InternalGetFreq(float index)
        {
            if (index > 10)
                return 1;
            if (index > 6)
                return 2;
            if (index > 3)
                return 4;
            return 6;
        }

        int InternalGetTickStep(int localCounter,int goalCounter)
        {
            if (localCounter > goalCounter)
                return 2;
            else if(localCounter < goalCounter)
                return 0;
            else
            {
                synced = true;
                return 1;
            }
        }

        void InternalUpdateScroll(int index, int lastIndex, float floatIndex,float timeStacker)
        {
            alpha = Mathf.Lerp(alpha, reval ? 1f : 0f, 0.15f);

            Vector2 anchor = new Vector2(Custom.rainWorld.options.ScreenSize.x / 2f, Custom.rainWorld.options.ScreenSize.y - 140f);
            int uppderIndex = index + 1;
            float t = floatIndex - (float)index;
            float reverseT = 1f - t;
            reverseT = CubicBezier(1f, 0f, 1f, 0f, reverseT);
            
            #region single
            if (index >= 10)
                digiSingle_1.x = anchor.x + (textRectSize * 0.6f);
            else
                digiSingle_1.x = anchor.x;
            digiSingle_1.y = anchor.y + Mathf.Cos(reverseT * Mathf.PI / 2f) * textRectSize;
            digiSingle_1.scaleY = Mathf.Sin(reverseT * Mathf.PI / 2f) * 2;
            digiSingle_1.alpha = Mathf.Sin(reverseT * Mathf.PI / 2f) * alpha;

            if (uppderIndex >= 10)
                digiSingle_2.x = anchor.x + (textRectSize * 0.6f);
            else
                digiSingle_2.x = anchor.x;
            digiSingle_2.y = anchor.y + Mathf.Cos((reverseT + 1) * Mathf.PI / 2f) * textRectSize;
            digiSingle_2.scaleY = Mathf.Sin((reverseT + 1) * Mathf.PI / 2f) * 2;
            digiSingle_2.alpha = Mathf.Sin((reverseT + 1) * Mathf.PI / 2f) * alpha;
            #endregion

            if (!scrollDigiTen)//强制置1来阻止10位数滚动
                reverseT = 1f;

            #region ten
            digiTen_1.x = anchor.x - (textRectSize * 0.6f);
            digiTen_1.y = anchor.y + Mathf.Cos(reverseT * Mathf.PI / 2f) * textRectSize;
            digiTen_1.scaleY = Mathf.Sin(reverseT * Mathf.PI / 2f) * 2;
            digiTen_1.alpha = Mathf.Sin(reverseT * Mathf.PI / 2f) * alpha;

            digiTen_2 .x = anchor.x - (textRectSize * 0.6f);
            digiTen_2.y = anchor.y + Mathf.Cos((reverseT + 1) * Mathf.PI / 2f) * textRectSize;
            digiTen_2.scaleY = Mathf.Sin((reverseT + 1) * Mathf.PI / 2f) * 2;
            digiTen_2.alpha = Mathf.Sin((reverseT + 1) * Mathf.PI / 2f) * alpha;
            #endregion
        }

        void InternalUpdateTextAndScrollMode(int index,int lastIndex)
        {
            string currentText = index.ToString();
            string upperText = (index + 1).ToString();
            string lastText = lastIndex.ToString();

            if (currentText.Length > 1)
                digiTen_1.text = currentText[0].ToString();
            else 
                digiTen_1.text = "";
            digiSingle_1.text = currentText.Last().ToString();


            if (upperText.Length > 1)
                digiTen_2.text = upperText[0].ToString();
            else
                digiTen_2.text = "";
            digiSingle_2.text = upperText.Last().ToString();

            scrollDigiTen = false;
            if (currentText.Length != upperText.Length)
                scrollDigiTen = true;
            else
            {
                if(currentText.Length == 2 && upperText[0] != currentText[0])
                    scrollDigiTen = true;
            }
        }


        internal IEnumerable<PlayerHooks.PlayerModule> GetPlayerInRoom()
        {
            if (cam.room == null)
                yield break;

            foreach(var creature in cam.room.abstractRoom.creatures)
            {
                if (creature.realizedCreature == null || !(creature.realizedCreature is Player))
                    continue;

                Player player = creature.realizedCreature as Player;
                if (PlayerHooks.modules.TryGetValue(player, out var hook))
                    yield return hook;
            }
            yield break;
        }

        FLabel GetNewDigiLabel()
        {
            return new FLabel(Custom.GetDisplayFont(), "")
            {
                isVisible = true,
                scaleX = 2f,
                scaleY = 2f,
                alpha = 0f
            };
        }

        float CubicBezier(float ax,float ay,float bx,float by,float t)
        {
            //see http://yisibl.github.io/cubic-bezier
            Vector2 a = Vector2.zero;
            Vector2 a1 = new Vector2(ax, ay);
            Vector2 b1 = new Vector2(bx, by);
            Vector2 b = Vector2.one;

            Vector2 c1 = Vector2.Lerp(a, a1, t);
            Vector2 c2 = Vector2.Lerp(b1, b, t);

            return Vector2.Lerp(c1, c2, t).y;
        }

        /// <summary>
        /// 重置计数器，并不会自动开启计时器
        /// </summary>
        /// <param name="startCounter"></param>
        public void ResetCounter(int startCounter)
        {
            counter = startCounter;
            lastCounter = counter;
            lastSoundIndex = float.MaxValue;
            syncGoalCounter = startCounter;
        }

        /// <summary>
        /// 同步并校准当前计时器的值
        /// </summary>
        /// <param name="currentCounter"></param>
        public void SyncCounter(int currentCounter)
        {
            if (currentCounter == syncGoalCounter)
                return;
            synced = false;
            syncGoalCounter = currentCounter;
        }

        /// <summary>
        /// 开启计时器，显示计时器的同时开始计时，并不会自动重置计时器的值
        /// </summary>
        public void StartTimer()
        {
            reval = true;
            syncGoalCounter = counter;
        }


        /// <summary>
        /// 关闭计时器，隐藏计时器的同时停止计时，并不会自动重置计时器的值
        /// </summary>
        public void StopTimer()
        {
            reval = false;
        }
    }
}
