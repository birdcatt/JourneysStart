using UnityEngine;
using Colour = UnityEngine.Color;
using System.IO;
using System; //Exception and WeakReference<>
using Custom = RWCustom.Custom;

//some of it is beecat moment
namespace JourneysStart.Shared.PlayerStuff;

public class PlayerGrafHooks
{
    public static Texture2D LightpupTailTexture;
    public static Texture2D SproutcatTailTexture;
    public static void Hook()
    {
        On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
        On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
    }

    #region not hooks
    public static void TailTextureFilePath(ref Texture2D TailTexture, string fileName)
    {
        TailTexture = new Texture2D(150, 75, TextureFormat.ARGB32, false);
        string path = AssetManager.ResolveFilePath("textures/" +  fileName + ".png");
        if (File.Exists(path))
        {
            byte[] data = File.ReadAllBytes(path);
            TailTexture.LoadImage(data);
        }
    }
    public static void UVMapTail(Player self, RoomCamera.SpriteLeaser sLeaser)
    {
        if (!Plugin.PlayerDataCWT.TryGetValue(self, out PlayerData playerData))
            return;

        SlugTailTexture playerTailTexture = playerData.tailPattern;

        if (!(sLeaser.sprites[2] is TriangleMesh tail && playerTailTexture.TailAtlas.elements?.Count > 0))
            return;

        tail.element = playerTailTexture.TailAtlas.elements[0];
        for (int i = tail.vertices.Length - 1; i >= 0; i--)
        {
            float perc = i / 2 / (float)(tail.vertices.Length / 2);
            Vector2 uv;
            if (i % 2 == 0)
                uv = new Vector2(perc, 0f);
            else if (i < tail.vertices.Length - 1)
                uv = new Vector2(perc, 1f);
            else
                uv = new Vector2(1f, 0f);

            // Map UV values to the element
            uv.x = Mathf.Lerp(tail.element.uvBottomLeft.x, tail.element.uvTopRight.x, uv.x);
            uv.y = Mathf.Lerp(tail.element.uvBottomLeft.y, tail.element.uvTopRight.y, uv.y);

            tail.UVvertices[i] = uv;
        }
    }
    #endregion

    public static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);

        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData) && pData.IsModcat)
        {
            UVMapTail(self.player, sLeaser);

            if (pData.IsLightpup)
            {
                Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
                pData.Lightpup.stripeIndex = sLeaser.sprites.Length - 1;
                sLeaser.sprites[pData.Lightpup.stripeIndex] = new FSprite("lightpup_bodystripes", true);
                self.AddToContainer(sLeaser, rCam, null);
            }
        }
    }
    public static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        bool PlayerIsModcat = Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData playerData) && playerData.IsModcat;

        if (PlayerIsModcat)
        {
            if (playerData.IsLightpup)
            {
                int stripeIndex = playerData.Lightpup.stripeIndex;

                //skinny him
                if (Plugin.SkinnyScale_Index0.TryGet(self.player, out float scale0))
                    sLeaser.sprites[0].scale = scale0;
                if (Plugin.SkinnyScale_Index1.TryGet(self.player, out float scale1))
                    sLeaser.sprites[1].scale = scale1;

                //body stripes
                sLeaser.sprites[stripeIndex].color = playerData.tailPattern.PatternColour;
                if (Plugin.StripeScale.TryGet(self.player, out float stripeScale))
                    sLeaser.sprites[stripeIndex].scale = stripeScale;
                else
                {
                    sLeaser.sprites[stripeIndex].scaleX = sLeaser.sprites[1].scaleX;
                    sLeaser.sprites[stripeIndex].scaleY = sLeaser.sprites[1].scaleY;
                }

                //keep this instead of trying to get value directly from sprites[1], it follows the body better + should be less laggy
                Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
                Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
                sLeaser.sprites[stripeIndex].x = (vector2.x * 2f + vector.x) / 3f - camPos.x;
                sLeaser.sprites[stripeIndex].y = (vector2.y * 2f + vector.y) / 3f - camPos.y - self.player.sleepCurlUp * 3f;
                sLeaser.sprites[stripeIndex].rotation = Custom.AimFromOneVectorToAnother(vector, Vector2.Lerp(self.tail[0].lastPos, self.tail[0].pos, timeStacker));
            }

            playerData.DressMySlugcat_ModCompat_DrawSprites(rCam, sLeaser); //white tail in here
        }

        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (PlayerIsModcat && playerData.IsLightpup)
        {
            //rotund world compatibility so stripes follow the body scale
            int stripeIndex = playerData.Lightpup.stripeIndex;
            if (Plugin.SkinnyScale_Index1.TryGet(self.player, out float scale1) && sLeaser.sprites[1].scale != scale1)
            {
                if (Plugin.StripeScale.TryGet(self.player, out float stripeScale))
                {
                    float scaleRatioX = sLeaser.sprites[1].scaleX / scale1;
                    float scaleRatioY = sLeaser.sprites[1].scaleY / scale1;
                    sLeaser.sprites[stripeIndex].scaleX = stripeScale * scaleRatioX;
                    sLeaser.sprites[stripeIndex].scaleY = stripeScale * scaleRatioY;
                }
            }
        }
    }
    public static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        orig(self, sLeaser, rCam, newContainer);
        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData) && pData.IsLightpup)
        {
            int spriteLen = pData.Lightpup.stripeIndex;

            //makes it so body stripes dont go on top of every creature
            FContainer foregroundContainer = rCam.ReturnFContainer("Foreground");
            FContainer midgroundContainer = rCam.ReturnFContainer("Midground");
            foregroundContainer.RemoveChild(sLeaser.sprites[spriteLen]);
            midgroundContainer.AddChild(sLeaser.sprites[spriteLen]);

            sLeaser.sprites[2].MoveBehindOtherNode(sLeaser.sprites[1]); //tail behind hips
            sLeaser.sprites[spriteLen].MoveToBack(); //so stripes wont go in front when being jolly carried
            sLeaser.sprites[spriteLen].MoveBehindOtherNode(sLeaser.sprites[9]); //stripes behind face
        }
    }
    public static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);

        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData playerData) && playerData.IsModcat)
        {
            Colour stripeColour = GetColour(playerData.tailPattern.OldPatternColour);

            playerData.tailPattern.RecolourTail(sLeaser.sprites[3].color, stripeColour);
            playerData.DressMySlugcat_ModCompat_DrawSprites(rCam, sLeaser);

            if (playerData.IsLightpup)
            {
                sLeaser.sprites[playerData.Lightpup.stripeIndex].color = stripeColour; //body stripe
            }

            Colour GetColour(Colour colour)
            {
                if (self.malnourished > 0f)
                {
                    float num = self.player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
                    colour = Colour.Lerp(colour, Colour.gray, 0.4f * num);
                }
                return self.HypothermiaColorBlend(colour);
            }
        }
    }
} //end of class