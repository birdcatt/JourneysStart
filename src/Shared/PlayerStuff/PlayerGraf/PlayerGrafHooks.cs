using UnityEngine;
using Colour = UnityEngine.Color;
using System; //Exception and WeakReference<>
using Custom = RWCustom.Custom;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using static JourneysStart.Slugcats.Outgrowth.PlayerStuff.OutgrowthData;
using Vector2 = UnityEngine.Vector2;
using static JourneysStart.Shared.PlayerStuff.PlayerGraf.PlayerGrafMethods;

namespace JourneysStart.Shared.PlayerStuff.PlayerGraf;

public class PlayerGrafHooks
{
    public static void Hook()
    {
        On.PlayerGraphics.MSCUpdate += PlayerGraphics_MSCUpdate;
        On.PlayerGraphics.DefaultFaceSprite += PlayerGraphics_DefaultFaceSprite;
        On.PlayerGraphics.Reset += PlayerGraphics_Reset;

        On.PlayerGraphics.ctor += PlayerGraphics_ctor;

        IL.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
        On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
    }

    #region sproutcat
    public static void PlayerGraphics_MSCUpdate(On.PlayerGraphics.orig_MSCUpdate orig, PlayerGraphics self)
    {
        orig(self);

        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData) && pData.IsSproutcat)
        {
            pData.Sproutcat.cheekFluff.Update();
            RopeMethods.MSCUpdate(self);
        }
    }
    public static string PlayerGraphics_DefaultFaceSprite(On.PlayerGraphics.orig_DefaultFaceSprite orig, PlayerGraphics self, float eyeScale)
    {
        string val = orig(self, eyeScale);
        //if (ModManager.MSC && Plugin.sproutcat == self.player.SlugCatClass)
        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out var pData) && pData.IsSproutcat && !pData.Sproutcat.usingDMSFaceSprite)
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
    public static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
    {
        orig(self);
        if (ModManager.MSC && Plugin.sproutcat == self.player.SlugCatClass)
        {
            for (int k = 0; k < self.ropeSegments.Length; k++)
            {
                self.ropeSegments[k].pos = self.player.mainBodyChunk.pos;
                self.ropeSegments[k].lastPos = self.player.mainBodyChunk.pos;
                self.ropeSegments[k].vel *= 0f;
            }
        }
    }
    #endregion

    public static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
    {
        orig(self, ow);

        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData))
        {
            if (pData.IsSproutcat)
            {
                pData.Sproutcat.cheekFluff = new(self, 13);
                pData.Sproutcat.backScales = new(self, 13 + pData.Sproutcat.cheekFluff.scales.Length);

                self.ropeSegments = new PlayerGraphics.RopeSegment[20];
                for (int i = 0; i < self.ropeSegments.Length; i++)
                {
                    self.ropeSegments[i] = new PlayerGraphics.RopeSegment(i, self);
                }
            }
            else if (pData.IsStrawberry)
            {
                if (self.player.playerState.isPup)
                {
                    self.tail[0] = new TailSegment(self, 8f, 2f, null, 0.85f, 1f, 1f, true);
                    self.tail[1] = new TailSegment(self, 6f, 3.5f, self.tail[0], 0.85f, 1f, 0.5f, true);
                    self.tail[2] = new TailSegment(self, 4f, 3.5f, self.tail[1], 0.85f, 1f, 0.5f, true);
                    self.tail[3] = new TailSegment(self, 2f, 3.5f, self.tail[2], 0.85f, 1f, 0.5f, true);
                }
                else
                {
                    self.tail[0] = new TailSegment(self, 8f, 4f, null, 0.85f, 1f, 1f, true);
                    self.tail[1] = new TailSegment(self, 6f, 7f, self.tail[0], 0.85f, 1f, 0.5f, true);
                    self.tail[2] = new TailSegment(self, 4f, 7f, self.tail[1], 0.85f, 1f, 0.5f, true);
                    self.tail[3] = new TailSegment(self, 2f, 7f, self.tail[2], 0.85f, 1f, 0.5f, true);
                }
            }
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
                pData.SpritesInited = true;

                UVMapTail(self.player, sLeaser);

                if (pData.IsLightpup)
                {
                    pData.Lightpup.stripeIndex = sLeaser.sprites.Length;
                    Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
                    sLeaser.sprites[pData.Lightpup.stripeIndex] = new FSprite("lightpup_bodystripes", true);
                }
                else if (pData.IsSproutcat)
                {
                    pData.Sproutcat.spriteIndexes = new int[3]; //remember to change this to fit the new sprites

                    int num = sLeaser.sprites.Length + pData.Sproutcat.cheekFluff.scalePos.Length + pData.Sproutcat.backScales.numberOfSprites;
                    pData.Sproutcat.spriteIndexes[0] = num++; //++ so Array.Resize gets correct length
                    pData.Sproutcat.spriteIndexes[1] = num++;
                    pData.Sproutcat.spriteIndexes[2] = num++;

                    Array.Resize(ref sLeaser.sprites, num);

                    sLeaser.sprites[pData.Sproutcat.spriteIndexes[0]] = new FSprite("sproutcat_bodyscar", true); //body scar
                    sLeaser.sprites[pData.Sproutcat.spriteIndexes[1]] = TriangleMesh.MakeLongMesh(self.ropeSegments.Length - 1, false, true); //rope
                    sLeaser.sprites[pData.Sproutcat.spriteIndexes[2]] = new FSprite("jsSproutcatLeftScarHeadA0", true); //face scar

                    pData.Sproutcat.cheekFluff.InitiateSprites(sLeaser);
                    pData.Sproutcat.backScales.InitiateSprites(sLeaser);
                }

                #if true
                Debug.Log($"{Plugin.MOD_NAME}: {self.player.SlugCatClass}'s sLeaser.sprites.Length is {sLeaser.sprites.Length}");
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (null == sLeaser.sprites[i])
                        Debug.Log($"\tsLeaser.sprites[{i}] is NULL");
                    else
                        Debug.Log($"\tsLeaser.sprites[{i}].element.name is {sLeaser.sprites[i].element.name}");
                }
                #endif

                //no need to call self.AddToContainer(sLeaser, rCam, null); since i IL'd to before that
            }
        });
    }
    public static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        orig(self, sLeaser, rCam, newContainer);
        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData) && pData.SpritesInited)
        {
            pData.SpritesInited = false; //for next time
            if (pData.IsLightpup)
            {
                AddNewSpritesToContainer(sLeaser, rCam, pData.Lightpup.stripeIndex);
                sLeaser.sprites[2].MoveBehindOtherNode(sLeaser.sprites[1]); //tail behind hips
            }
            else if (pData.IsSproutcat)
            {
                FContainer container = newContainer ?? rCam.ReturnFContainer("Midground");

                pData.Sproutcat.backScales.AddToContainer(sLeaser, container);
                pData.Sproutcat.cheekFluff.AddToContainer(sLeaser, container); //so fluff is behind everything else

                foreach (int i in pData.Sproutcat.spriteIndexes)
                {
                    AddNewSpritesToContainer(sLeaser, rCam, i);
                }
                RopeMethods.AddToContainer(self, sLeaser);
            }
        }
    }
    public static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        bool PlayerIsModcat = Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData playerData) && playerData.IsModcat;

        if (PlayerIsModcat)
        {
            #region sprite math
            //why are you doing maths? because the sprites are less laggy and follow the body better, esp when slug is at higher speeds
            //why is it like that? i dont know

            //maths for lightpup's body stripes / sproutcat's body scar
            Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
            float xPos = (vector2.x * 2f + vector.x) / 3f - camPos.x;
            float yPos = (vector2.y * 2f + vector.y) / 3f - camPos.y - self.player.sleepCurlUp * 3f;

            //maths for sproutcat / strawberry face sprites
            Vector2 headPos = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
            float num3 = Custom.AimFromOneVectorToAnother(Vector2.Lerp(vector2, vector, 0.5f), headPos);

            if (self.player.aerobicLevel > 0.5f)
            {
                headPos -= Custom.DirVec(vector2, vector) * Mathf.Lerp(-1f, 1f, 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(self.lastBreath, self.breath, timeStacker) * 3.1415927f * 2f)) * Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, self.player.aerobicLevel), 1.5f) * 0.75f;
            }
            if (self.player.sleepCurlUp > 0f)
            {
                headPos.y += 1f * self.player.sleepCurlUp;
                headPos.x += Mathf.Sign(vector.x - vector2.x) * 2f * self.player.sleepCurlUp;
                num3 = Mathf.Lerp(num3, 45f * Mathf.Sign(vector.x - vector2.x), self.player.sleepCurlUp);
            }
            if (ModManager.CoopAvailable && self.player.bool1) //what the hell is bool1
            {
                //i think bool1 is a haunted variable
                headPos.y -= 1.9f;
                num3 = Mathf.Lerp(num3, 45f * Mathf.Sign(vector.x - vector2.x), 0.7f);
            }
            #endregion

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
                int bodyScarIndex = playerData.Sproutcat.spriteIndexes[BODY_SCAR_INDEX];

                sLeaser.sprites[bodyScarIndex].color = playerData.tailPattern.PatternColour;
                sLeaser.sprites[bodyScarIndex].x = xPos;
                sLeaser.sprites[bodyScarIndex].y = yPos;
                sLeaser.sprites[bodyScarIndex].rotation = Custom.AimFromOneVectorToAnother(vector2, vector); //index 0 rotation
                //scaleX and scaleY done after orig

                int faceScarIndex = playerData.Sproutcat.spriteIndexes[FACE_SCAR_INDEX];
                sLeaser.sprites[faceScarIndex].x = headPos.x - camPos.x;
                sLeaser.sprites[faceScarIndex].y = headPos.y - camPos.y;
                sLeaser.sprites[faceScarIndex].rotation = num3;

                playerData.Sproutcat.cheekFluff.DrawSprites(sLeaser, timeStacker, camPos);
                playerData.Sproutcat.backScales.DrawSprites(sLeaser, timeStacker, camPos);
            }

            playerData.ModCompat_DressMySlugcat_DrawSprites(rCam, sLeaser); //white tail in here
        }

        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (PlayerIsModcat)
        {
            //rotund world compatibility so stripes follow the body scale
            if (playerData.IsLightpup)
            {
                if (playerData.Lightpup.flareWindup > 5)
                {
                    sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("FaceStunned");
                }

                if (Plugin.SkinnyScale_Index1.TryGet(self.player, out float scale1) && sLeaser.sprites[1].scale != scale1
                    && Plugin.StripeScale.TryGet(self.player, out float stripeScale))
                {
                    ////hes going ROUND
                    //float bodyXYRatio = 14f / 19f; //i think its the body, not hips sprite, that i got this from
                    //sLeaser.sprites[0].scale = Mathf.Max(sLeaser.sprites[0].scaleX, sLeaser.sprites[0].scaleY);
                    //sLeaser.sprites[1].scale = Mathf.Max(sLeaser.sprites[1].scaleX, sLeaser.sprites[1].scaleY);
                    //sLeaser.sprites[0].scale *= bodyXYRatio;
                    //sLeaser.sprites[1].scale *= bodyXYRatio;
                    ////OH MY GOD HES SO SKINNY

                    int stripeIndex = playerData.Lightpup.stripeIndex;
                    float scaleRatioX = sLeaser.sprites[1].scaleX / scale1;
                    float scaleRatioY = sLeaser.sprites[1].scaleY / scale1;
                    sLeaser.sprites[stripeIndex].scaleX = stripeScale * scaleRatioX;
                    sLeaser.sprites[stripeIndex].scaleY = stripeScale * scaleRatioY;
                }
            }
            else if (playerData.IsSproutcat)
            {
                if (!playerData.usingDMSHeadSprite)
                {
                    //head & face scar asymmetry
                    string headSpriteName = sLeaser.sprites[3].element.name;
                    string scarSpriteName;
                    int faceScarIndex = playerData.Sproutcat.spriteIndexes[FACE_SCAR_INDEX];

                    headSpriteName = headSpriteName.Remove(0, headSpriteName.IndexOf("Head"));

                    if (self.RenderAsPup && headSpriteName.StartsWith("HeadC"))
                    {
                        headSpriteName = "HeadA" + headSpriteName.Remove(0, "HeadC".Length);
                    }

                    if (headSpriteName.StartsWith("HeadA"))
                    {
                        if (sLeaser.sprites[3].scaleX < 0)
                        {
                            //lmao jsSproutcatLeftScarjsSproutcatLeftHeadA4, don't swap this order
                            scarSpriteName = "jsSproutcatLeftScar" + headSpriteName;
                            headSpriteName = "jsSproutcatLeft" + headSpriteName;
                        }
                        else
                        {
                            scarSpriteName = "jsSproutcatRightScar" + headSpriteName;
                            headSpriteName = "jsSproutcatRight" + headSpriteName;
                        }

                        sLeaser.sprites[3].element = Futile.atlasManager.GetElementWithName(headSpriteName);

                        sLeaser.sprites[faceScarIndex].element = Futile.atlasManager.GetElementWithName(scarSpriteName);
                        sLeaser.sprites[faceScarIndex].scaleX = sLeaser.sprites[3].scaleX;
                        sLeaser.sprites[faceScarIndex].scaleY = sLeaser.sprites[3].scaleY;
                    }
                }

                //body scar scale
                int bodyScarIndex = playerData.Sproutcat.spriteIndexes[BODY_SCAR_INDEX];
                sLeaser.sprites[bodyScarIndex].scaleX = sLeaser.sprites[1].scaleX;
                sLeaser.sprites[bodyScarIndex].scaleY = sLeaser.sprites[1].scaleY;

                RopeMethods.DrawSprites(self, sLeaser, timeStacker, camPos);
            }
            else if (playerData.IsStrawberry)
            {
                if (!playerData.usingDMSHeadSprite)
                {
                    string headSpriteName = sLeaser.sprites[3].element.name;
                    headSpriteName = "jsStrawberryHeadA" + headSpriteName.Remove(0, headSpriteName.IndexOf("Head") + "HeadA".Length);
                    sLeaser.sprites[3].element = Futile.atlasManager.GetElementWithName(headSpriteName);
                }
            }
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
                sLeaser.sprites[playerData.Sproutcat.spriteIndexes[BODY_SCAR_INDEX]].color = stripeColour;
                sLeaser.sprites[playerData.Sproutcat.spriteIndexes[FACE_SCAR_INDEX]].color = stripeColour;

                playerData.Sproutcat.cheekFluff.ApplyPalette(sLeaser);
                playerData.Sproutcat.backScales.ApplyPalette(sLeaser, stripeColour);
                RopeMethods.ApplyPalette(self, sLeaser, stripeColour);
            }
        }
    }
    #endregion
} //end of class