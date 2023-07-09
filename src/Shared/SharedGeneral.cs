using static JourneysStart.Plugin;
using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
using MSC_AbstractObjectType = MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType;
using System.Linq;
using JourneysStart.Shared.PlayerStuff;
using JourneysStart.Shared.ArtOnScreen;
using JourneysStart.Shared.PlayerStuff.PlayerGraf;

namespace JourneysStart.Shared;

public class SharedGeneral
{
    public static void Hook()
    {
        PlayerGrafHooks.Hook();
        Crafting.Hook();

        CutscenesSlideshows.Hook();
        JollySelectMenu.Hook();
        OracleDialogue.Hook();
        RoomScriptHooks.Hook();

        GeneralHooks();
    }

    public static void GeneralHooks()
    {
        On.Player.ctor += Player_ctor;
        On.Player.Update += Player_Update;
        On.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;

        On.SlugcatStats.SpearSpawnElectricRandomChance += SpearSpawnElectricChance;
        On.SlugcatStats.SpearSpawnExplosiveRandomChance += SpearSpawnExplosiveChance;

        On.WorldLoader.OverseerSpawnConditions += WorldLoader_OverseerSpawnConditions;

        On.Player.ObjectEaten += Player_ObjectEaten; //food reaction + eating a bug
    }

    #region ctor and update
    public static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        SlugcatStats.Name name = self.slugcatStats.name;
        if (Utility.IsModcat(name))
        {
            if (!PlayerDataCWT.TryGetValue(self, out _))
                PlayerDataCWT.Add(self, new PlayerData(self));

            if (lghtbrpup == name)
            {
                self.setPupStatus(true); //thanks oatmealine
            }
            else if (sproutcat == name)
            {
                self.tongue = new Player.Tongue(self, 0); //2nd arg is index
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
    public static void Player_ClassMechanicsSaint(On.Player.orig_ClassMechanicsSaint orig, Player self)
    {
        if (self.room != null && PlayerDataCWT.TryGetValue(self, out PlayerData pData) && pData.IsSproutcat)
        {
            pData.Sproutcat.TongueUpdate();
        }
        orig(self);
    }
    #endregion

    #region spear spawn chance
    public static float SpearSpawnElectricChance(On.SlugcatStats.orig_SpearSpawnElectricRandomChance orig, SlugcatStats.Name index)
    {
        if (lghtbrpup == index)
            return orig(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear);
        if (sproutcat == index)
            return orig(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer);
        return orig(index);
    }
    public static float SpearSpawnExplosiveChance(On.SlugcatStats.orig_SpearSpawnExplosiveRandomChance orig, SlugcatStats.Name index)
    {
        if (lghtbrpup == index)
            return orig(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear);
        if (sproutcat == index)
            return orig(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer);
        return orig(index);
    }
    #endregion

    public static bool WorldLoader_OverseerSpawnConditions(On.WorldLoader.orig_OverseerSpawnConditions orig, WorldLoader self, SlugcatStats.Name character)
    {
        bool val = orig(self, character);
        StoryGameSession story = self.game.GetStorySession;
        SaveState saveState = story.saveState;
        if (!saveState.guideOverseerDead)
        {
            if (lghtbrpup == story.game.StoryCharacter)
            {
                return Lightpup_Debug_OverseerSpawn.TryGet(story.game, out bool spawnNow) && spawnNow
                    || Utility.ProgressionUnlocked(story) && !self.world.abstractRooms.Any(room => room.name == "AI");
            }
            if (sproutcat == story.game.StoryCharacter)
            {
                int cycleNumber = saveState.cycleNumber;
                return UnityEngine.Random.value < self.world.region.regionParams.playerGuideOverseerSpawnChance
                    && saveState.miscWorldSaveData.SSaiConversationsHad > 0
                    && 9 < cycleNumber && cycleNumber < 25
                    && !saveState.miscWorldSaveData.playerGuideState.angryWithPlayer;
            }
        }
        return val;
    }

    public static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
    {
        orig(self, edible);

        if (PlayerDataCWT.TryGetValue(self, out PlayerData playerData))
        {

            if (playerData.IsSproutcat && Utility.EdibleIsBug(edible) && !playerData.Sproutcat.AteABugThisCycle)
            {
                playerData.Sproutcat.AteABugThisCycle = true;
            }

            #region lightpup food rxn
            else if (playerData.IsLightpup && !self.exhausted) //please dont jump if youre chonk as all hell
            {
                if (edible is Creature critter)
                {
                    CreatureTemplate.Type crit = critter.abstractCreature.creatureTemplate.type;
                    if (CreatureTemplate.Type.SmallCentipede == crit)
                        LikesFood();
                    else if (CreatureTemplate.Type.SmallNeedleWorm == crit)
                        DislikesFood();
                    return;
                }

                AbstractObjectType foodObj = (edible as PhysicalObject).abstractPhysicalObject.type;

                if (MSC_AbstractObjectType.LillyPuck == foodObj
                    || MSC_AbstractObjectType.DandelionPeach == foodObj
                    || MSC_AbstractObjectType.Seed == foodObj
                    || edible as PhysicalObject is OracleSwarmer)
                {
                    LikesFood();
                    return;
                }

                if (AbstractObjectType.DangleFruit == foodObj
                    || AbstractObjectType.SlimeMold == foodObj
                    || MSC_AbstractObjectType.GooieDuck == foodObj
                    || AbstractObjectType.JellyFish == foodObj)
                {
                    DislikesFood();
                    return;
                }

                void LikesFood()
                {
                    if (!self.room.game.IsStorySession) //can only have negative food reactions outside of story
                        return;
                    if (LikesFoodJumpValue.TryGet(self, out int jumpValue))
                    {
                        playerData.Lightpup.controller.likesFood = jumpValue; //the number determines how large the jump is
                    }
                }

                void DislikesFood()
                {
                    (self.graphicsModule as PlayerGraphics).blink = 0;
                    self.Blink(40);
                    playerData.Lightpup.controller.likesFood = 0;
                }
            }
            #endregion
        }
    }
}
