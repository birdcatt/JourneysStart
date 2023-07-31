#if false
using System.Collections.Generic;
using SlugBase.SaveData;

namespace JourneysStart;

public static class SaveDataInfo
{
    //MiscWorldSaveData resets on death
    //  - just not saved on death
    //  - is per scug (will work for any scug tho)
    //DeathPersistantSaveData resets on new game
    //  -saved on death
    //MiscProgressionData resets on save slot wipe
    //  - can use to share data btwn scugs, shared savefile-wide, tracks stuff like unlocked scugs

    public static class Lightpup
    {
        private const string StrProgressionUnlocked = "JourneysStart_Lightpup_ProgressionUnlocked"; //use this to check if read lightpup pearl
        private const string StrAltEndingAchieved = "JourneysStart_Lightpup_AltEndingAchieved";

        private const string StrAlreadyMetDM = "JourneysStart_Lightpup_AlreadyMetDM";

        public static bool GetProgressionUnlocked(MiscWorldSaveData data)
        {
            if (!data.GetSlugBaseData().TryGet(StrProgressionUnlocked, out bool save))
                data.GetSlugBaseData().Set(StrProgressionUnlocked, save = false);
            return save;
        }
        public static bool GetAltEndingAchieved(MiscWorldSaveData data)
        {
            if (!data.GetSlugBaseData().TryGet(StrAltEndingAchieved, out bool save))
                data.GetSlugBaseData().Set(StrAltEndingAchieved, save = false);
            return save;
        }
    }

    public static class Sproutcat
    {
        private const string StrProgressionUnlocked = "JourneysStart_Sproutcat_ProgressionUnlocked";
        private const string StrAltEndingAchieved = "JourneysStart_Sproutcat_AltEndingAchieved";
        private const string StrInfectedCrits = "JourneysStart_Sproutcat_InfectedCrits";

        public static bool GetProgressionUnlocked(MiscWorldSaveData data)
        {
            if (!data.GetSlugBaseData().TryGet(StrProgressionUnlocked, out bool save))
                data.GetSlugBaseData().Set(StrProgressionUnlocked, save = false);
            return save;
        }
        public static bool GetAltEndingAchieved(MiscWorldSaveData data)
        {
            if (!data.GetSlugBaseData().TryGet(StrAltEndingAchieved, out bool save))
                data.GetSlugBaseData().Set(StrAltEndingAchieved, save = false);
            return save;
        }

#if false
        public static HashSet<EntityID> GetInfectedCreatures(MiscWorldSaveData data)
        {
            if (!data.GetSlugBaseData().TryGet(StrInfectedCrits, out HashSet<EntityID> save))
                data.GetSlugBaseData().Set(StrInfectedCrits, save = new());
            return save;
        }
        internal static bool AddInfectedCreature(MiscWorldSaveData data, EntityID critID)
        {
            var save = GetInfectedCreatures(data);
            bool success = save.Add(critID); //true if value was inserted
            data.GetSlugBaseData().Set(StrInfectedCrits, save);
            return success;
        }
        internal static bool RemoveInfectedCreature(MiscWorldSaveData data, EntityID critID)
        {
            var save = GetInfectedCreatures(data);
            bool success = save.Remove(critID); //true if found and removed, false if not found
            data.GetSlugBaseData().Set(StrInfectedCrits, save);
            return success;
        }
#endif
    }

    public static class Strawberry
    {
        public enum MenuArt
        {
            //Start, //has both
            //None,

            //FoundYellow,
            //FoundBlue,
            //FoundBoth,

            //MidGame_FoundYellow_LostBlue,
            //MidGame_FoundBlue_LostYellow,

            //PostGame_LostYellow_HasBlue,
            //PostGame_LostBlue_HasYellow,

            //LostBoth

            Start,
            None,

            FoundYellow,
            FoundBlue,
            FoundBoth
        }

        public enum SleepArt
        {
            None,
            Yellow,
            Blue,
            Both
        }

        public enum AscensionArt
        {
            None,
            Yellow,
            Blue,
            Both
        }

        private const string StrYellowRetrieved = "JourneysStart_Strawberry_YellowRetrieved";
        private const string StrBlueRetrieved = "JourneysStart_Strawberry_BlueRetrieved";

        private const string StrYellowDeadMidGame = "JourneysStart_Strawberry_YellowDeadMidGame";
        private const string StrBlueDeadMidGame = "JourneysStart_Strawberry_BlueDeadMidGame";

        private const string StrYellowDeadPostGame = "JourneysStart_Strawberry_YellowDeadPostGame";
        private const string StrBlueDeadPostGame = "JourneysStart_Strawberry_BlueDeadPostGame";

        public static bool GetYellowDead(MiscWorldSaveData data)
        {
            if (!data.GetSlugBaseData().TryGet(StrYellowDeadMidGame, out bool save))
                data.GetSlugBaseData().Set(StrYellowDeadMidGame, save = false);

            if (!data.GetSlugBaseData().TryGet(StrYellowDeadPostGame, out bool save2))
                data.GetSlugBaseData().Set(StrYellowDeadPostGame, save2 = false);

            return save || save2;
        }
    }
}
#endif