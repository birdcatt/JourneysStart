using DressMySlugcat;
using DressMySlugcat.Hooks;
//using LightpupData = JourneysStart.Lightbringer.PlayerStuff.LightpupData;
using Colour = UnityEngine.Color;
using JourneysStart.Shared.PlayerStuff;
using JourneysStart.Slugcats.Outgrowth.PlayerStuff;
//using Debug = UnityEngine.Debug;

namespace JourneysStart.Slugcats;

public class ModCompat
{
    public class DressMySlugcatPatch
    {
        //meant for the version on 28 Apr 2023

        public bool alreadyRestoredStripes = true;
        public bool alreadyRestoredTail = true;
        public bool alreadyRestoredHead = true;

        public void DressMySlugcat_DrawSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!PlayerGraphicsHooks.PlayerGraphicsData.TryGetValue(self, out _))
                return;

            if (!(Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData) && pData.IsModcat))
                return;

            Customization customization = Customization.For(self.player.slugcatStats.name.value, self.player.playerState.playerNumber);

            if (pData.IsSproutcat) //or strawberry
            {
                CheckHeadSprite(self, sLeaser, rCam, customization.CustomSprite("HEAD"));
                var customSprite = customization.CustomSprite("FACE");
                pData.Sproutcat.usingDMSFaceSprite = customSprite?.SpriteSheet != null && customSprite?.SpriteSheet.Name != "Default";
            }

            CheckBodySprite(self, sLeaser, rCam, customization.CustomSprite("BODY"));
            CheckTailSprite(self, sLeaser, customization.CustomSprite("TAIL"));
        }

        #region lightpup (body)
        public void CheckBodySprite(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, CustomSprite customSprite)
        {
            if (!Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData))
                return;

            if (customSprite?.SpriteSheet == null)
                RestoreBodySprites(self, sLeaser, rCam);
            else if (customSprite?.SpriteSheet.Name != "Default")
            {
                alreadyRestoredStripes = false;
                if (pData.IsLightpup)
                    rCam.ReturnFContainer("Midground").RemoveChild(sLeaser.sprites[pData.Lightpup.stripeIndex]);
                else if (pData.IsSproutcat)
                    rCam.ReturnFContainer("Midground").RemoveChild(sLeaser.sprites[pData.Sproutcat.spriteIndexes[OutgrowthData.BODY_SCAR_INDEX]]);
            }
        }
        public void RestoreBodySprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (alreadyRestoredStripes) return; //required because of AddToContainer(), otherwise he keeps multiplying
            alreadyRestoredStripes = true;

            if (!Plugin.PlayerDataCWT.TryGetValue(self.player, out var pData))
                return;

            if (pData.IsLightpup)
            {
                rCam.ReturnFContainer("Foreground").RemoveChild(sLeaser.sprites[pData.Lightpup.stripeIndex]);
                rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[pData.Lightpup.stripeIndex]);
            }
            else if (pData.IsSproutcat)
            {
                rCam.ReturnFContainer("Foreground").RemoveChild(sLeaser.sprites[pData.Sproutcat.spriteIndexes[OutgrowthData.BODY_SCAR_INDEX]]);
                rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[pData.Sproutcat.spriteIndexes[OutgrowthData.BODY_SCAR_INDEX]]);
            }

            //his neck is invisible when restoring body stripes
            //sLeaser.sprites[0].isVisible = true;

            //self.AddToContainer(sLeaser, rCam, null);
            //rCam.ReturnFContainer("Foreground").RemoveChild(sLeaser.sprites[stripeIndex]);
            //rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[stripeIndex]);
        }
        #endregion

        #region tail
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
        #endregion

        #region head
        public void CheckHeadSprite(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, CustomSprite customSprite)
        {
            if (!Plugin.PlayerDataCWT.TryGetValue(self.player, out var pData))
                return;

            if (customSprite?.SpriteSheet == null)
            {
                pData.usingDMSHeadSprite = false;
                if (pData.IsSproutcat)
                    RestoreSproutcatHeadSprite(self, sLeaser, rCam);
            }
            else if (customSprite?.SpriteSheet.Name != "Default")
            {
                alreadyRestoredHead = false;
                pData.usingDMSHeadSprite = true;
                if (pData.IsSproutcat)
                    RemoveSproutcatHeadSprite(self, sLeaser, rCam);
            }
        }
        public void RemoveSproutcatHeadSprite(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!Plugin.PlayerDataCWT.TryGetValue(self.player, out var pData) || !pData.IsSproutcat)
                return;

            for (int i = pData.Sproutcat.cheekFluff.startIndex; i < pData.Sproutcat.cheekFluff.endIndex; i++)
            {
                rCam.ReturnFContainer("Midground").RemoveChild(sLeaser.sprites[i]);
            }
            rCam.ReturnFContainer("Midground").RemoveChild(sLeaser.sprites[pData.Sproutcat.spriteIndexes[OutgrowthData.FACE_SCAR_INDEX]]);
        }
        public void RestoreSproutcatHeadSprite(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (alreadyRestoredHead) return;
            alreadyRestoredHead = true;

            //shoud be fine to restore the cheek fluff and scar only
            //other code from rw or dms should restore the head itself

            if (!Plugin.PlayerDataCWT.TryGetValue(self.player, out var pData) || !pData.IsSproutcat)
                return;

            for (int i = pData.Sproutcat.cheekFluff.startIndex; i < pData.Sproutcat.cheekFluff.endIndex; i++)
            {
                rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[i]);
                sLeaser.sprites[i].MoveBehindOtherNode(sLeaser.sprites[9]); //move behind face
            }
            rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[pData.Sproutcat.spriteIndexes[OutgrowthData.FACE_SCAR_INDEX]]);
            sLeaser.sprites[pData.Sproutcat.spriteIndexes[OutgrowthData.FACE_SCAR_INDEX]].MoveBehindOtherNode(sLeaser.sprites[9]); //move behind face
        }
        #endregion
    }
}
