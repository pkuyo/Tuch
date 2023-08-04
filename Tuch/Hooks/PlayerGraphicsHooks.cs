using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuch.Hooks
{
    static class PlayerGraphicsHooks
    {
        public static void OnModsInit()
        {
            //On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig(self,sLeaser, rCam, timeStacker, camPos);
            if (PlayerHooks.gameStart && PlayerHooks.inGamePlayer != null)
            {
                if (!PlayerHooks.inGamePlayer.Contains(self.player))
                {
                    foreach (var sprite in sLeaser.sprites)
                        sprite.isVisible = false;
                    return;
                }
            }
            foreach (var sprite in sLeaser.sprites)
                if(!sprite.isVisible)
                    sprite.isVisible = true;
        }
    }
}
