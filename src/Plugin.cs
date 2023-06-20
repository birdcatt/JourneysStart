using System.Runtime.CompilerServices; //CWT
using BepInEx;
using SlugBase.Features;
//using SlugBase.DataTypes;
//using SlugBase;
using static SlugBase.Features.FeatureTypes;
using Debug = UnityEngine.Debug;
using System.Linq; //for ModManager.ActiveMods.Any
using Exception = System.Exception;

using JourneysStart.Lightbringer.FisobsTaser;
using JourneysStart.Shared;
using JourneysStart.Lightbringer;
using JourneysStart.Outgrowth;
using JourneysStart.Outgrowth.FisobsSeed;
using Fisobs.Core;
using JourneysStart.Shared.PlayerStuff;
using PlayerData = JourneysStart.Shared.PlayerStuff.PlayerData;

namespace JourneysStart
{
    [BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
    class Plugin : BaseUnityPlugin
    {
        public const string MOD_ID = "bluecubism.journeysstart";
        public const string MOD_NAME = "Journey's Start";
        public const string MOD_VERSION = "0.1.0";

        public static readonly SlugcatStats.Name lghtbrpup = new("Lightbringer");
        public static readonly SlugcatStats.Name sproutcat = new("sproutcat");
        public static ConditionalWeakTable<Player, PlayerData> PlayerDataCWT = new();

        #region lightpup variables
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

        #region outgrowth variables
        public static readonly GameFeature<bool> Sprout_Debug_UnlockProgression = GameBool("sproutcat/debug/unlock_progression");
        #endregion

        private static bool isPostInit;
        private static bool isOnDisabled;

        internal static bool ModEnabled_DressMySlugcat = false;

        // Add hooks
        public void OnEnable()
        {
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

            PlayerGrafHooks.TailTextureFilePath(ref PlayerGrafHooks.LightpupTailTexture, "lightpup_tailstripes");
            PlayerGrafHooks.TailTextureFilePath(ref PlayerGrafHooks.SproutcatTailTexture, "sproutcat_tailtexture");
            On.Player.ctor += Player_ctor;
            PlayerGrafHooks.Hook();

            On.Player.Update += Player_Update; //i think this has to be here because it uses the graf variables
            //i still honestly dont know. can i put it back in OnEnable? probably
        }
        #region
        public void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);

            try
            {
                if (isPostInit) return;
                isPostInit = true;

                if (ModManager.ActiveMods.Any(mod => mod.id == "dressmyslugcat"))
                    ModEnabled_DressMySlugcat = true;
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
            Content.Register(new TaserFisob());
            Content.Register(new SeedFisob());
            LightpupGeneral.Hook();
            OutgrowthGeneral.Hook();
            SharedGeneral.Hook();
        }

        public static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            SlugcatStats.Name name = self.slugcatStats.name;
            if (Utility.SlugIsMod(name))
            {
                if (!PlayerDataCWT.TryGetValue(self, out _))
                    PlayerDataCWT.Add(self, new PlayerData(self));

                if (lghtbrpup == name)
                {
                    self.setPupStatus(true); //thanks oatmealine
                }
                else if (sproutcat == name)
                {
                    self.tongue = new Player.Tongue(self, 0);
                }
            }
        }
        public static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.room != null && PlayerDataCWT.TryGetValue(self, out PlayerData playerData))
            {
                playerData.Update();
            }
        }
    }
}