using Debug = UnityEngine.Debug;
using DataPearlType = DataPearl.AbstractDataPearl.DataPearlType;
using static SLOracleBehaviorHasMark;
using static JourneysStart.Utility;
using static JourneysStart.Slugcats.Lightbringer.MiscData.FRDData;

namespace JourneysStart.Shared.OracleStuff
{
    public class PearlDialogue
    {
        public static void Hook()
        {
            On.Conversation.DataPearlToConversation += Conversation_DataPearlToConversation;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
        }

        public static Conversation.ID Conversation_DataPearlToConversation(On.Conversation.orig_DataPearlToConversation orig, DataPearlType type)
        {
            var val = orig(type);
            return LightpupPearl == type ? LightpupPearlConvoID : val;
        }
        public static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, MoonConversation self)
        {
            orig(self);
            if (LightpupPearlConvoID == self.id)
            {
                string regionName = self.myBehavior.oracle.room.world.region.name;
                bool isDMAndLightpup = ("DM" == regionName || MoreSlugcats.MoreSlugcatsEnums.OracleID.DM == self.myBehavior.oracle.ID) && Plugin.lghtbrpup == self.currentSaveFile;
                Debug.Log($"{Plugin.MOD_NAME}: {LightpupPearl.value} is being read by {regionName}");

                if (Plugin.Lightpup_Debug_SkipLightpupPearlIntroDialogue.TryGet(self.myBehavior.oracle.room.game, out bool skipPearlIntro) && skipPearlIntro)
                    goto PlayPearlDialogue;

                string startingText = "The contents of this pearl are from those of a local group far from here. ";
                if (isDMAndLightpup)
                {
                    self.AddMessage("Is this the pearl you were entrusted with, little messenger? Let us see what is written in its contents.", 10, 10);
                    self.AddMessage(startingText + "You've travelled a great distance to bring this to me.", textLinger: 10);
                }
                else
                {
                    if ("SL" == regionName || Oracle.OracleID.SL == self.myBehavior.oracle.ID)
                        self.PearlIntro();
                    else
                    {
                        //switch random?
                        self.AddMessage("Let us see what you have here.", 10, 10);
                    }
                    self.AddMessage(startingText + "It's a surprise to see how far the rain can wash things away.", textLinger: 10);
                }

            PlayPearlDialogue:
                self.LoadEventsFromFile(LightpupPearl.value);

                bool hasNotReadPearl = 0 == self.myBehavior.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.playerEncountersWithMark; //hijacking this for this purpose
                //pearl gets added to significantPearls list before doing dialogue
                if (isDMAndLightpup && hasNotReadPearl)
                {
                    if (!self.myBehavior.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.significantPearls.Contains(LightpupPearl))
                        self.myBehavior.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.significantPearls.Add(LightpupPearl);
                    //it shouldve already contain this pearl in the list, but in case it doesnt, add it

                    self.myBehavior.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.playerEncountersWithMark++;
                    self.LoadEventsFromFile("Lightpup_DM_UnlockProgression");
                }
            }
        }
    }
}
