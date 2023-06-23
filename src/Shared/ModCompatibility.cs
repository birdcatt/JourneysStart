using DressMySlugcat;
using DressMySlugcat.Hooks;
//using LightpupData = JourneysStart.Lightbringer.PlayerStuff.LightpupData;
using Colour = UnityEngine.Color;
using JourneysStart.Shared.PlayerStuff;
//using Debug = UnityEngine.Debug;

namespace JourneysStart.Shared;

public class ModCompatibility
{
    public class DressMySlugcatPatch
    {
        //meant for the version on 28 Apr 2023

        public bool alreadyRestoredStripes = true;
        public bool alreadyRestoredTail = true;
        public void DressMySlugcat_DrawSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!PlayerGraphicsHooks.PlayerGraphicsData.TryGetValue(self, out _))
                return;

            if (!(Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData) && pData.IsModcat))
                return;

            SlugcatStats.Name name = self.player.slugcatStats.name;

            Customization customization = Customization.For(self.player.slugcatStats.name.value, self.player.playerState.playerNumber);
            CustomSprite customSprite;

            if (Plugin.lghtbrpup == name)
            {
                customSprite = customization.CustomSprite("BODY");
                CheckBodySprite(self, sLeaser, rCam, pData.Lightpup.stripeIndex, customSprite);
            }

            customSprite = customization.CustomSprite("TAIL");
            CheckTailSprite(self, sLeaser, customSprite);
        }

        #region lightpup
        public void CheckBodySprite(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, int stripeIndex, CustomSprite customSprite)
        {
            if (customSprite?.SpriteSheet == null)
                RestoreBodyStripes(self, sLeaser, rCam);
            else if (customSprite?.SpriteSheet.Name != "Default")
            {
                alreadyRestoredStripes = false;
                rCam.ReturnFContainer("Midground").RemoveChild(sLeaser.sprites[stripeIndex]);
            }
        }
        public void RestoreBodyStripes(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (alreadyRestoredStripes) return; //required because of AddToContainer(), otherwise he keeps multiplying
            alreadyRestoredStripes = true;

            if (!Plugin.PlayerDataCWT.TryGetValue(self.player, out _))
                return;

            //his neck is invisible when restoring body stripes
            //sLeaser.sprites[0].isVisible = true;

            self.AddToContainer(sLeaser, rCam, null);
            //rCam.ReturnFContainer("Foreground").RemoveChild(sLeaser.sprites[stripeIndex]);
            //rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[stripeIndex]);
        }
        #endregion

        public void CheckTailSprite(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, CustomSprite customSprite)
        {
            if (customSprite?.SpriteSheet == null)
                RestoreTail(self.player, sLeaser);
            else if (customSprite?.SpriteSheet.Name != "Default" && Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData playerData))
            {
                alreadyRestoredTail = false;
                playerData.tailPattern.usingDMSTailSprite = true;
            }
        }
        public void RestoreTail(Player player, RoomCamera.SpriteLeaser sLeaser)
        {
            if (alreadyRestoredTail) return;
            alreadyRestoredTail = true;

            if (!Plugin.PlayerDataCWT.TryGetValue(player, out PlayerData playerData))
                return;

            playerData.tailPattern.usingDMSTailSprite = false;

            //restore tail
            sLeaser.sprites[2].color = Colour.white;
            playerData.tailPattern.RecolourUVMapTail();
        }
    }
}
