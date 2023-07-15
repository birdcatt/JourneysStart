using BepInEx;
using System.Runtime.CompilerServices; //CWT
using System.Linq; //for ModManager.ActiveMods.Any
using Exception = System.Exception;

using Debug = UnityEngine.Debug;
using Texture2D = UnityEngine.Texture2D;
using KeyCode = UnityEngine.KeyCode;

using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using ImprovedInput;

using JourneysStart.Shared;
using JourneysStart.FisobsItems;
using JourneysStart.FisobsItems.Seed;
using JourneysStart.FisobsItems.Taser;
using JourneysStart.Shared.PlayerStuff.PlayerGraf;
using PlayerData = JourneysStart.Shared.PlayerStuff.PlayerData;
using JourneysStart.Slugcats.Lightbringer;
using JourneysStart.Slugcats.Strawberry;
using JourneysStart.Slugcats.Outgrowth;

namespace JourneysStart
{
    [BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
    class Plugin : BaseUnityPlugin
    {
        public static new BepInEx.Logging.ManualLogSource Logger; //hiding BaseUnityPlugin.Logger

        public const string MOD_ID = "bluecubism.journeysstart";
        public const string MOD_NAME = "Journey's Start";
        public const string MOD_VERSION = "0.1.0";

        public static readonly SlugcatStats.Name lghtbrpup = new("Lightbringer");
        public static readonly SlugcatStats.Name sproutcat = new("sproutcat");
        public static readonly SlugcatStats.Name strawberry = new("Strawberry");
        public static ConditionalWeakTable<Player, PlayerData> PlayerDataCWT = new();

        public static Texture2D LightpupTailTexture;
        public static Texture2D SproutcatTailTexture;
        public static Texture2D StrawberryTailTexture;

        public static readonly PlayerKeybind FlareKeybind = PlayerKeybind.Register("JourneysStart:LightpupFlare", MOD_NAME, "Flare",
            KeyCode.LeftControl, KeyCode.Joystick1Button4);

        #region sfx
        public static SoundID sproutcat_bush_rustle1;
        public static SoundID sproutcat_bush_rustle2;
        public static SoundID sproutcat_bush_rustle3;
        public static SoundID sproutcat_bush_rustle4;
        public static SoundID sproutcat_bush_rustle5;
        #endregion

        #region lightpup slugbase variables
        public static readonly PlayerFeature<int> LikesFoodJumpValue = PlayerInt("lightpup/likes_food_jump_value");

        public static readonly PlayerFeature<float> SkinnyScale_Index0 = PlayerFloat("lightpup/skinny_scale_index0");
        public static readonly PlayerFeature<float> SkinnyScale_Index1 = PlayerFloat("lightpup/skinny_scale_index1");
        public static readonly PlayerFeature<float> StripeScale = PlayerFloat("lightpup/stripe_scale");

        public static readonly PlayerFeature<int> IdleLookWaitInput = PlayerInt("lightpup/idle_look_wait_input");
        public static readonly PlayerFeature<int> IdleLookPointChangeTime = PlayerInt("lightpup/idle_look_point_change_time");
        public static readonly PlayerFeature<int[]> IdleLookWaitRandomRange = PlayerInts("lightpup/idle_look_wait_random_range");
        public static readonly PlayerFeature<int[]> IdleLookVectorRange = PlayerInts("lightpup/idle_look_vector_range");

        public static readonly GameFeature<bool> Lightpup_Debug_OverseerSpawn = GameBool("lightpup/debug/overseer_spawn");
        public static readonly GameFeature<bool> Lightpup_Debug_UnlockProgression = GameBool("lightpup/debug/unlock_progression");
        public static readonly GameFeature<bool> Lightpup_Debug_SkipLightpupPearlIntroDialogue = GameBool("lightpup/debug/skip_lightpup_pearl_intro_dialogue");
        public static readonly GameFeature<bool> Lightpup_Debug_DisableStartRain = GameBool("lightpup/debug/disable_start_rain");
        #endregion

        #region outgrowth slugbase variables
        public static readonly GameFeature<bool> Sprout_Debug_UnlockProgression = GameBool("sproutcat/debug/unlock_progression");
        public static readonly PlayerFeature<bool> Sprout_Debug_CheekFluffColours = PlayerBool("sproutcat/debug/cheek_fluff_colours");
        public static readonly PlayerFeature<bool> Sprout_Debug_NoAncientBot = PlayerBool("sproutcat/debug/no_ancient_bot");
        #endregion

        private static bool isPostInit;
        private static bool isOnDisabled;

        internal static bool ModEnabled_DressMySlugcat = false;

        // Add hooks
        public void OnEnable()
        {
            Logger = base.Logger;

            FlareKeybind.Description = "The key held to have the Lightbringer emit an bright, electric glow to stun all near him.";

            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            Hook();
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
            On.RainWorld.OnModsDisabled += RainWorld_OnModsDisabled;
        }

        // Load any resources, such as sprites or sounds
        public void LoadResources(RainWorld rainWorld)
        {
            //most of the time, its fine to register values once then never unregister
            //OnModsInit is called after OnModsDisabled

            MachineConnector.SetRegisteredOI(MOD_ID, ConfigMenu.instance);

            if (!Futile.atlasManager.DoesContainAtlas("journeysstart_assets"))
                Futile.atlasManager.LoadAtlas("atlases/journeysstart_assets");

            sproutcat_bush_rustle1 = new("sproutcat-bush-rustle1", true); //cant have _ in them
            sproutcat_bush_rustle2 = new("sproutcat-bush-rustle2", true);
            sproutcat_bush_rustle3 = new("sproutcat-bush-rustle3", true);
            sproutcat_bush_rustle4 = new("sproutcat-bush-rustle4", true);
            sproutcat_bush_rustle5 = new("sproutcat-bush-rustle5", true);

            PlayerGrafMethods.TailTextureFilePath(ref LightpupTailTexture, "lightpup_tailstripes");
            PlayerGrafMethods.TailTextureFilePath(ref SproutcatTailTexture, "sproutcat_tailtexture");
            PlayerGrafMethods.TailTextureFilePath(ref StrawberryTailTexture, "strawberry_tailtexture");
        }

        #region
        public void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);

            try
            {
                if (isPostInit) return;
                isPostInit = true;

                ModEnabled_DressMySlugcat = ModManager.ActiveMods.Any(mod => mod.id == "dressmyslugcat");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        public void RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods)
        {
            orig(self, newlyDisabledMods);

            try
            {
                if (isOnDisabled) return;
                isOnDisabled = true;

                if (newlyDisabledMods.Any(mod => mod.id == MOD_ID))
                {
                    TaserFisob.UnregisterValues();
                    SeedFisob.UnregisterValues();

                    if (MultiplayerUnlocks.CreatureUnlockList.Contains(TaserFisob.ArenaTaser))
                        MultiplayerUnlocks.CreatureUnlockList.Remove(TaserFisob.ArenaTaser);

                    if (MultiplayerUnlocks.CreatureUnlockList.Contains(SeedFisob.ArenaSeed))
                        MultiplayerUnlocks.CreatureUnlockList.Remove(SeedFisob.ArenaSeed);

                    if (Futile.atlasManager.DoesContainAtlas("journeysstart_assets"))
                        Futile.atlasManager.UnloadAtlas("journeysstart_assets");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        #endregion

        public static void Hook()
        {
            FisobsGeneral.Hook();
            LightpupGeneral.Hook();
            OutgrowthGeneral.Hook();
            StrawberryGeneral.Hook();
            SharedGeneral.Hook();
        }
    }
}