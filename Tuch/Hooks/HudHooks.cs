using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using RWCustom;

namespace Tuch.Hooks
{
    static class HudHooks
    {
        public static void OnModsInit()
        {
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud; ;
        }

        private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            throw new NotImplementedException();
        }
    }

    public class GameHUD : HudPart
    {
        public GameHUD(HUD.HUD hud) : base(hud)
        {
            name = new FLabel(Custom.GetDisplayFont(), "");
            sprite = new FSprite("Futile_White");
            hud.fContainers[1].AddChild(name);
            hud.fContainers[1].AddChild(sprite);

        }

        public FLabel name;
        public FSprite sprite;
        public override void Update()
        {
            base.Update();
        }
    }
}
