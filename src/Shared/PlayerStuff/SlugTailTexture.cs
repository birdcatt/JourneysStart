using SlugBase.DataTypes;
using SlugBase.Features;
using SlugBase;
using System;
using UnityEngine;
using Colour = UnityEngine.Color;
using Custom = RWCustom.Custom;

namespace JourneysStart.Shared.PlayerStuff
{
    public class SlugTailTexture
    {
        public SlugcatStats.Name name;
        public Colour BodyColour;
        public Colour PatternColour; //used to be called StripeColour until i got sproutcat
        public FAtlas TailAtlas;
        public WeakReference<Player> playerRef; //yo what is this

        public Colour OldPatternColour; //gets changed from the flare charges and is used for hypothermia and when going thru pipes
        public bool usingDMSTailSprite; //dont colour in tail gdi

        public readonly string TailTextureName;
        public Texture2D TailTexture;

        public SlugTailTexture(Player player)
        {
            name = player.slugcatStats.name;

            if (!Utility.SlugIsMod(name))
            {
                return;
            }

            if (Plugin.lghtbrpup == name)
            {
                TailTextureName = "lightpup_tailstripes";
                TailTexture = PlayerGrafHooks.LightpupTailTexture;
            }
            else //if (Plugin.sproutcat == name)
            {
                TailTextureName = "sproutcat_tailtexture";
                TailTexture = PlayerGrafHooks.SproutcatTailTexture;
            }

            Debug.Log($"{Plugin.MOD_NAME}: (SlugTailTexture) Creating new tail texture for {name.value}");

            playerRef = new WeakReference<Player>(player);

            SetupColours(player);
            LoadTailAtlas();

            OldPatternColour = PatternColour;
        }
        ~SlugTailTexture()
        {
            try
            {
                TailAtlas.Unload();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
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
            if (SlugBaseCharacter.TryGet(name, out SlugBaseCharacter charac)
                && charac.Features.TryGet(PlayerFeatures.CustomColors, out ColorSlot[] customColours))
            {
                //loading default colours
                if (customColours.Length > 0)
                    BodyColour = customColours[0].GetColor(playerNumber);
                if (customColours.Length > 2)
                    PatternColour = customColours[2].GetColor(playerNumber);
            }
        }
        public void LoadTailAtlas()
        {
            Texture2D tailTexture = new(TailTexture.width, TailTexture.height, TextureFormat.ARGB32, false);
            Graphics.CopyTexture(TailTexture, tailTexture);

            MapTextureColour(tailTexture, Colour.red, BodyColour);
            MapTextureColour(tailTexture, Colour.white, PatternColour);

            if (playerRef.TryGetTarget(out Player player))
            {
                TailAtlas = Futile.atlasManager.LoadAtlasFromTexture(TailTextureName + "_" + player.playerState.playerNumber + UnityEngine.Random.value, tailTexture, false);
            }
        }
        public void RecolourTail(Colour newBody, Colour newStripe)
        {
            if (newBody == BodyColour && newStripe == PatternColour)
                return;

            if (!playerRef.TryGetTarget(out _))
                return;

            //Debug.Log(Plugin.MOD_NAME + ": Recolouring tail to " + newBody.ToString() + " (body) and " + newStripe.ToString() + " (stripes)");
            BodyColour = newBody;
            PatternColour = newStripe;

            RecolourTail();
        }
        public void RecolourTail()
        {
            if (!playerRef.TryGetTarget(out _))
                return;

            TailAtlas?.Unload();

            if (usingDMSTailSprite)
                return;

            LoadTailAtlas();
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
                    PlayerGrafHooks.UVMapTail(player, sLeaser);
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
    }
}
