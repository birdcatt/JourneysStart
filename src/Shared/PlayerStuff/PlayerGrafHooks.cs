using UnityEngine;
using Colour = UnityEngine.Color;
using System.IO;
using System; //Exception and WeakReference<>
using Custom = RWCustom.Custom;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using static JourneysStart.Outgrowth.PlayerStuff.OutgrowthData;

namespace JourneysStart.Shared.PlayerStuff;

public class PlayerGrafHooks
{
    public static Texture2D LightpupTailTexture;
    public static Texture2D SproutcatTailTexture;
    public static void Hook()
    {
        On.PlayerGraphics.MSCUpdate += PlayerGraphics_MSCUpdate;
        On.PlayerGraphics.DefaultFaceSprite += PlayerGraphics_DefaultFaceSprite;
        On.PlayerGraphics.ctor += PlayerGraphics_ctor;

        IL.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
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

        if (!(sLeaser.sprites[2] is TriangleMesh tail && playerData.tailPattern.TailAtlas.elements?.Count > 0))
            return;

        tail.element = playerData.tailPattern.TailAtlas.elements[0];
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

    public static void PlayerGraphics_MSCUpdate(On.PlayerGraphics.orig_MSCUpdate orig, PlayerGraphics self)
    {
        orig(self);

        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData))
        {
            pData.Sproutcat?.cheekFluff?.Update();
        }
    }
    public static string PlayerGraphics_DefaultFaceSprite(On.PlayerGraphics.orig_DefaultFaceSprite orig, PlayerGraphics self, float eyeScale)
    {
        string val = orig(self, eyeScale);
        if (ModManager.MSC && Plugin.sproutcat == self.player.SlugCatClass)
        {
            //arti face
            if (self.blink > 0)
                return "FaceB";

            if (eyeScale < 0f)
                return "FaceD";

            return "FaceC";
        }
        return val;
    }

    public static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
    {
        orig(self, ow);

        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData) && pData.IsSproutcat)
        {
            pData.Sproutcat.cheekFluff = new(self, 13);
        }
    }
    #region sleaser stuff
    public static void PlayerGraphics_InitiateSprites(ILContext il)
    {
        ILCursor c = new(il);

        c.GotoNext(MoveType.Before, i => i.MatchLdarg(1), i => i.MatchLdcI4(2), i => i.Match(OpCodes.Newarr), i => i.MatchStfld<RoomCamera.SpriteLeaser>("sprites"));

        c.GotoPrev(MoveType.Before, i => i.MatchLdarg(0), i => i.MatchLdarg(1), i => i.MatchLdarg(2), i => i.MatchLdnull(), i => i.MatchCallOrCallvirt<GraphicsModule>("AddToContainer"));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.EmitDelegate((PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser) =>
        {
            if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData) && pData.IsModcat && !pData.SpritesInited)
            {
                //!SpritesInited because InitiateSprites runs twice in a row, for some reason
                UVMapTail(self.player, sLeaser);

                if (pData.IsLightpup)
                {
                    Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
                    pData.Lightpup.stripeIndex = sLeaser.sprites.Length - 1;
                    sLeaser.sprites[pData.Lightpup.stripeIndex] = new FSprite("lightpup_bodystripes", true);
                }
                else if (pData.IsSproutcat)
                {
                    pData.Sproutcat.spriteIndexes = new int[1]; //so far only body scar rn, change this later to fit the other stuff

                    int num = sLeaser.sprites.Length + pData.Sproutcat.cheekFluff.scalePos.Length;
                    pData.Sproutcat.spriteIndexes[0] = num++; //++ so Array.Resize gets correct length

                    Array.Resize(ref sLeaser.sprites, num);

                    sLeaser.sprites[pData.Sproutcat.spriteIndexes[0]] = new FSprite("sproutcat_bodyscar", true);
                    pData.Sproutcat.cheekFluff.InitiateSprites(sLeaser);
                }

                pData.SpritesInited = true;

                Debug.Log($"{Plugin.MOD_NAME}: {self.player.SlugCatClass}'s sLeaser.sprites.Length is {sLeaser.sprites.Length}");
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (null == sLeaser.sprites[i])
                        Debug.Log($"\tsLeaser.sprites[{i}] is NULL");
                    else
                        Debug.Log($"\tsLeaser.sprites[{i}].element.name is {sLeaser.sprites[i].element.name}");
                }
                //no need to call self.AddToContainer(sLeaser, rCam, null); since i IL'd to before that
            }
        });
    }
    public static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        bool PlayerIsModcat = Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData playerData) && playerData.IsModcat;

        if (PlayerIsModcat)
        {
            //maths for lightpup's body stripes / sproutcat's body scar
            Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
            float xPos = (vector2.x * 2f + vector.x) / 3f - camPos.x;
            float yPos = (vector2.y * 2f + vector.y) / 3f - camPos.y - self.player.sleepCurlUp * 3f;

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
                sLeaser.sprites[stripeIndex].x = xPos;
                sLeaser.sprites[stripeIndex].y = yPos;
                sLeaser.sprites[stripeIndex].rotation = Custom.AimFromOneVectorToAnother(vector, Vector2.Lerp(self.tail[0].lastPos, self.tail[0].pos, timeStacker));
                //index 1 rotation, turns the sprite upside down
            }
            else if (playerData.IsSproutcat)
            {
                int bodyScarIndex = playerData.Sproutcat.spriteIndexes[BodyScarIndex];
                sLeaser.sprites[bodyScarIndex].color = playerData.tailPattern.PatternColour;
                //scaleX and scaleY done after orig

                sLeaser.sprites[bodyScarIndex].x = xPos;
                sLeaser.sprites[bodyScarIndex].y = yPos;
                sLeaser.sprites[bodyScarIndex].rotation = Custom.AimFromOneVectorToAnother(vector2, vector); //index 0 rotation

                playerData.Sproutcat.cheekFluff.DrawSprites(sLeaser, timeStacker, camPos);
            }

            playerData.ModCompat_DressMySlugcat_DrawSprites(rCam, sLeaser); //white tail in here
        }

        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (PlayerIsModcat)
        {
            //rotund world compatibility so stripes follow the body scale
            if (playerData.IsLightpup)
            {
                if (Plugin.SkinnyScale_Index1.TryGet(self.player, out float scale1) && sLeaser.sprites[1].scale != scale1)
                {
                    if (Plugin.StripeScale.TryGet(self.player, out float stripeScale))
                    {
                        int stripeIndex = playerData.Lightpup.stripeIndex;
                        float scaleRatioX = sLeaser.sprites[1].scaleX / scale1;
                        float scaleRatioY = sLeaser.sprites[1].scaleY / scale1;
                        sLeaser.sprites[stripeIndex].scaleX = stripeScale * scaleRatioX;
                        sLeaser.sprites[stripeIndex].scaleY = stripeScale * scaleRatioY;
                    }
                }
            }
            else if (playerData.IsSproutcat)
            {
                int bodyScarIndex = playerData.Sproutcat.spriteIndexes[BodyScarIndex];
                sLeaser.sprites[bodyScarIndex].scaleX = sLeaser.sprites[0].scaleX /*+ sLeaser.sprites[0].scaleX / 10f*/;
                sLeaser.sprites[bodyScarIndex].scaleY = sLeaser.sprites[0].scaleY + 0.5f;
            }
        }
    }
    public static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        orig(self, sLeaser, rCam, newContainer);
        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData) && pData.SpritesInited)
        {
            if (pData.IsLightpup)
            {
                int spriteLen = pData.Lightpup.stripeIndex;

                //makes it so body stripes dont go on top of every creature
                rCam.ReturnFContainer("Foreground").RemoveChild(sLeaser.sprites[spriteLen]);
                rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[spriteLen]);

                sLeaser.sprites[2].MoveBehindOtherNode(sLeaser.sprites[1]); //tail behind hips
                sLeaser.sprites[spriteLen].MoveToBack(); //so stripes wont go in front when being jolly carried
                sLeaser.sprites[spriteLen].MoveBehindOtherNode(sLeaser.sprites[9]); //stripes behind face
            }
            else if (pData.IsSproutcat)
            {
                foreach (int i in pData.Sproutcat.spriteIndexes)
                {
                    rCam.ReturnFContainer("Foreground").RemoveChild(sLeaser.sprites[i]);
                    rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[i]);
                    sLeaser.sprites[i].MoveToBack();
                    sLeaser.sprites[i].MoveBehindOtherNode(sLeaser.sprites[9]);
                }
                pData.Sproutcat.cheekFluff.AddToContainer(sLeaser, rCam.ReturnFContainer("Midground"));
            }
            pData.SpritesInited = false; //for next time
        }
    }
    public static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);

        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData playerData) && playerData.IsModcat)
        {
            Colour stripeColour = playerData.tailPattern.OldPatternColour; //the unique colour
            if (self.malnourished > 0f)
            {
                float num = self.player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
                stripeColour = Colour.Lerp(stripeColour, Colour.gray, 0.4f * num);
            }
            stripeColour = self.HypothermiaColorBlend(stripeColour);

            playerData.tailPattern.RecolourTail(sLeaser.sprites[3].color, stripeColour);
            playerData.ModCompat_DressMySlugcat_DrawSprites(rCam, sLeaser);

            if (playerData.IsLightpup)
            {
                sLeaser.sprites[playerData.Lightpup.stripeIndex].color = stripeColour; //body stripe
            }
            else if (playerData.IsSproutcat)
            {
                int bodyScarIndex = playerData.Sproutcat.spriteIndexes[BodyScarIndex];
                sLeaser.sprites[bodyScarIndex].color = stripeColour;

                playerData.Sproutcat.cheekFluff.ApplyPalette(sLeaser);
            }
        }
    }
    #endregion
} //end of class