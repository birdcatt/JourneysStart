using SlugBase.DataTypes;
using SlugBase.Features;
using SlugBase;
using System;
using UnityEngine;
using Colour = UnityEngine.Color;
using Custom = RWCustom.Custom;

//beecat started off most of this code o7 thanks
//this code is quite ugly but also it works
namespace JourneysStart.Shared.PlayerStuff.PlayerGraf
{
    public class SlugTailTexture
    {
        public Colour BodyColour;
        public Colour PatternColour; //used to be called StripeColour until i got sproutcat
        public FAtlas TailAtlas;
        public WeakReference<Player> playerRef;

        public Colour OldPatternColour; //gets changed from the flare charges and is used for hypothermia and when going thru pipes
        public bool usingDMSTailSprite; //dont colour in tail gdi

        public readonly string TailTextureName;
        public Texture2D TailTexture;

        //private Unity.Collections.NativeArray<byte> TextureMipData;

        private byte[] RedInTextureRef = { 255, 0, 0 }; //start as red/white, bc the mipData changes after the first time you apply
        private byte[] WhiteInTextureRef = { 255, 255, 255 };

        public SlugTailTexture(Player player)
        {
            SlugcatStats.Name name = player.slugcatStats.name;

            Debug.Log($"{Plugin.MOD_NAME}: (SlugTailTexture) Creating new tail texture for {name.value}");
            if (!Utility.IsModcat(name))
            {
                Debug.Log($"{Plugin.MOD_NAME}: Player {name} {player.playerState.playerNumber} is not supposed to get a new tail! How did this happen?!");
                return;
            }

            if (Plugin.lghtbrpup == name)
            {
                TailTextureName = "lightpup_tailstripes";
                TailTexture = Plugin.LightpupTailTexture;
            }
            else if (Plugin.sproutcat == name)
            {
                TailTextureName = "sproutcat_tailtexture";
                TailTexture = Plugin.SproutcatTailTexture;
            }
            else
            {
                TailTextureName = "strawberry_tailtexture";
                TailTexture = Plugin.StrawberryTailTexture;
            }

            playerRef = new WeakReference<Player>(player);

            //SetupColours(player);
            BodyColour = Utility.GetSlugcatColour(player, 0);
            PatternColour = Utility.GetSlugcatColour(player, 2);

            if (Colour.white == BodyColour)
                BodyColour = Custom.hexToColor("feffff"); //pure white is not a viable body colour, since the stripes will colour the entire tail

            LoadTailAtlas();

            OldPatternColour = PatternColour;

            RedInTextureRef[0] = (byte)(BodyColour.r * 255);
            RedInTextureRef[1] = (byte)(BodyColour.g * 255);
            RedInTextureRef[2] = (byte)(BodyColour.b * 255);

            WhiteInTextureRef[0] = (byte)(PatternColour.r * 255);
            WhiteInTextureRef[1] = (byte)(PatternColour.g * 255);
            WhiteInTextureRef[2] = (byte)(PatternColour.b * 255);
        }
        ~SlugTailTexture()
        {
            try
            {
                TailAtlas?.Unload();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

#if false
        #region colours
        public void SetupColours(Player player)
        {
            //prevent arena colours being assinged to main player outside of arena
            bool jollyDefaultColourMode = ModManager.CoopAvailable && Custom.rainWorld.options.jollyColorMode == Options.JollyColorMode.DEFAULT;
            int playerNumber = player.room.game.session is not ArenaGameSession && (player.playerState.playerNumber == 0 || jollyDefaultColourMode) ? -1 : player.playerState.playerNumber;

            LoadDefaultColours(playerNumber);

            if (PlayerGraphics.customColors != null && !player.IsJollyPlayer && !ModManager.JollyCoop)
            {
                if (PlayerGraphics.customColors.Count > 0)
                    BodyColour = PlayerGraphics.CustomColorSafety(0);
                if (PlayerGraphics.customColors.Count > 2)
                    PatternColour = PlayerGraphics.CustomColorSafety(2);
            }
            else if (ModManager.CoopAvailable)
            {
                if (playerNumber > 0)
                {
                    //why is p1 null. entire reason i have to check for player and colour mode
                    BodyColour = PlayerGraphics.JollyColor(playerNumber, 0);
                    PatternColour = PlayerGraphics.JollyColor(playerNumber, 2);
                }
                else if (Custom.rainWorld.options.jollyColorMode == Options.JollyColorMode.CUSTOM)
                {
                    //these are the jolly custom colours btw
                    JollyCoop.JollyMenu.JollyPlayerOptions jollyPlayer = Custom.rainWorld.options.jollyPlayerOptionsArray[0];
                    BodyColour = jollyPlayer.GetBodyColor();
                    PatternColour = jollyPlayer.GetUniqueColor();
                }
            }
            if (Colour.white == BodyColour)
                BodyColour = Custom.hexToColor("feffff"); //pure white is not a viable body colour, since the stripes will colour the entire tail
        }
        public void LoadDefaultColours(int playerNumber)
        {
            if (!playerRef.TryGetTarget(out Player player))
                return;

            if (SlugBaseCharacter.TryGet(player.slugcatStats.name, out SlugBaseCharacter charac)
                && charac.Features.TryGet(PlayerFeatures.CustomColors, out ColorSlot[] customColours))
            {
                //loading default colours
                if (customColours.Length > 0)
                    BodyColour = customColours[0].GetColor(playerNumber);
                if (customColours.Length > 2)
                    PatternColour = customColours[2].GetColor(playerNumber);
            }
        }
        #endregion
#endif

        public void LoadTailAtlas()
        {
            Texture2D tailTexture = new(TailTexture.width, TailTexture.height, TextureFormat.ARGB32, false); //RGBA32 makes the red part invis
            Graphics.CopyTexture(TailTexture, tailTexture);

            MapTextureColour(tailTexture, Colour.red, BodyColour);
            MapTextureColour(tailTexture, Colour.white, PatternColour);

            if (playerRef.TryGetTarget(out Player player))
            {
                TailAtlas = Futile.atlasManager.LoadAtlasFromTexture(TailTextureName + "_" + player.playerState.playerNumber + UnityEngine.Random.value, tailTexture, false);
            }
        }
        public void LoadTailAtlasInGame()
        {
            //if (playerRef.TryGetTarget(out Player player))
            //{
            //    Debug.Log($"{Plugin.MOD_NAME}: Recolouring tail for player {player.playerState.playerNumber}");
            //}

            var mipData = (TailAtlas.texture as Texture2D).GetPixelData<byte>(0);
            //var mipData = TextureMipData;
            //mip map levels are different versions of a texture (for diff resolutions n all)
            //mip 0 is the og texture. here, there's only 1 texture
            //TailTexture.mipmapCount is 1

            //in the texture, mipData[i] is only 255 or 0
            //225, 225, 0, 0 is red (ARGB32)
            //225, 225, 225, 225 is white

            MapTextureColourInGame(ref mipData, ref RedInTextureRef, BodyColour);
            MapTextureColourInGame(ref mipData, ref WhiteInTextureRef, PatternColour);

            (TailAtlas.texture as Texture2D).SetPixelData(mipData, 0, 0); //mipmap level 0
            (TailAtlas.texture as Texture2D).Apply(false);
        }

        public void RecolourTail(Colour newBody, Colour newStripe)
        {
            if (newBody == BodyColour && newStripe == PatternColour)
                return;

            if (!playerRef.TryGetTarget(out _))
                return;

            BodyColour = newBody;
            PatternColour = newStripe;

            RecolourTail();
        }
        public void RecolourTail()
        {
            if (!playerRef.TryGetTarget(out _))
                return;

            //TailAtlas?.Unload();

            if (usingDMSTailSprite)
                return;

            LoadTailAtlasInGame();
            //LoadTailAtlas();
            RecolourUVMapTail();
        }
        public void RecolourUVMapTail()
        {
            if (usingDMSTailSprite)
                return;

            if (!(playerRef.TryGetTarget(out Player player) && player.room != null))
                return;

            foreach (RoomCamera.SpriteLeaser sLeaser in player.room.game.cameras[0].spriteLeasers)
            {
                if (sLeaser.drawableObject is PlayerGraphics pGraph && pGraph.player == player)
                {
                    PlayerGrafMethods.UVMapTail(player, sLeaser);
                    break;
                }
            }
        }
        public void MapTextureColour(Texture2D texture, Colour from, Colour to)
        {
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    if (texture.GetPixel(x, y) == from)
                    {
                        texture.SetPixel(x, y, to);
                    }
                }
            }
            texture.Apply(false);
        }
        public void MapTextureColourInGame(ref Unity.Collections.NativeArray<byte> mipData, ref byte[] from, Colour to)
        {
            byte toR = (byte)(to.r * 255);
            byte toG = (byte)(to.g * 255);
            byte toB = (byte)(to.b * 255);

            if (from[0] == toR && from[1] == toG && from[2] == toB)
                return; //just leave if its the same colour

            //Debug.Log($"{Plugin.MOD_NAME}: ({from[0]}, {from[1]}, {from[2]}) -> ({toR}, {toG}, {toB})");
            //int colsChanged = 0;

            for (int i = 0; i < mipData.Length; i += 4)
            {
                //if alpha is invisible, continue
                if (mipData[i] == 0) //0 = invis, 255 = full vis
                    continue;

                int indexR = i + 1;
                int indexG = i + 2;
                int indexB = i + 3;
                //Debug.Log($"\t(alpha) mipData[{i}] = {mipData[i]}");
                if (mipData[indexR] == from[0] && mipData[indexG] == from[1] && mipData[indexB] == from[2])
                {
                    //colsChanged++; //debug
                    mipData[indexR] = toR;
                    mipData[indexG] = toG;
                    mipData[indexB] = toB;
                }
            }

            from[0] = toR; //change the byte array data since the tail texture is being changed in CPU
            from[1] = toG;
            from[2] = toB;

            //Debug.Log($"\t{Plugin.MOD_NAME}: Changed colour {colsChanged} times");
            //if (0 == colsChanged)
            //{
            //    Debug.Log($"{Plugin.MOD_NAME}: INCOMING LAG! I NEED TO READ THIS BYTE ARRAY");
            //    for (int i = 0; i < mipData.Length; i++)
            //    {
            //        Debug.Log($"\t{Plugin.MOD_NAME}: mipData[{i}] = {mipData[i]}");
            //    }
            //}
        }
    }
}
