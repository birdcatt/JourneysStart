using static JourneysStart.Plugin;
using System.Linq;
using JourneysStart.Shared.PlayerStuff;
using JourneysStart.Shared.ArtOnScreen;
using JourneysStart.Shared.PlayerStuff.PlayerGraf;
using JourneysStart.Shared.OracleStuff;

namespace JourneysStart.Shared;

public class SharedGeneral
{
    public static void Hook()
    {
        //CutscenesSlideshows.Hook();
        JollySelectMenu.Hook();

        OracleDialogue.Hook();
        PearlDialogue.Hook();

        PlayerGrafHooks.Hook();
        PlayerHooks.Hook();
        Crafting.Hook();

        RoomScriptHooks.Hook();

        GeneralHooks();
    }

    public static void GeneralHooks()
    {
        On.SlugcatStats.SpearSpawnModifier += SlugcatStats_SpearSpawnModifier;
        On.SlugcatStats.SpearSpawnElectricRandomChance += SpearSpawnElectricChance;
        On.SlugcatStats.SpearSpawnExplosiveRandomChance += SpearSpawnExplosiveChance;

        On.WorldLoader.OverseerSpawnConditions += WorldLoader_OverseerSpawnConditions;
    }

    #region spear spawn chance
    private static float SlugcatStats_SpearSpawnModifier(On.SlugcatStats.orig_SpearSpawnModifier orig, SlugcatStats.Name index, float originalSpearChance)
    {
        if (lghtbrpup == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear;
        else if (sproutcat == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;
        else if (strawberry == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint;
        return orig(index, originalSpearChance);
    }
    public static float SpearSpawnElectricChance(On.SlugcatStats.orig_SpearSpawnElectricRandomChance orig, SlugcatStats.Name index)
    {
        if (lghtbrpup == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear;
        else if (sproutcat == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;
        else if (strawberry == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint;
        return orig(index);
    }
    public static float SpearSpawnExplosiveChance(On.SlugcatStats.orig_SpearSpawnExplosiveRandomChance orig, SlugcatStats.Name index)
    {
        if (lghtbrpup == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear;
        else if (sproutcat == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;
        else if (strawberry == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint;
        return orig(index);
    }
    #endregion

    public static bool WorldLoader_OverseerSpawnConditions(On.WorldLoader.orig_OverseerSpawnConditions orig, WorldLoader self, SlugcatStats.Name character)
    {
        bool val = orig(self, character);
        if (self.game.session is StoryGameSession story && !story.saveState.guideOverseerDead)
        {
            SaveState saveState = story.saveState;
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
}
