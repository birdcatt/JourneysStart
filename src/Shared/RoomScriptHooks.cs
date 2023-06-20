using SlugBase.Features;
using SlugBase;
using static JourneysStart.Lightbringer.RoomScripts;
using static JourneysStart.Outgrowth.RoomScripts;

namespace JourneysStart.Shared
{
    public class RoomScriptHooks
    {
        public static void Hook()
        {
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update += OE_GourmandEnding_Update;
            On.MoreSlugcats.MSCRoomSpecificScript.AddRoomSpecificScript += MSCRoomSpecificScript_AddRoomSpecificScript;
        }

        public static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            LightpupGateStart.alreadyRun = false; //god damn it
            LightpupOEStart.alreadyRun = false; //i hate this
            SproutcatLCStart.alreadyRun = false; //another one to the hall of shame
        }
        public static void OE_GourmandEnding_Update(On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.orig_Update orig, MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding self, bool eu)
        {
            if (Plugin.lghtbrpup == self.room.game.StoryCharacter)
            {
                self.Destroy();
                return;
            }
            orig(self, eu);
        }

        public static void MSCRoomSpecificScript_AddRoomSpecificScript(On.MoreSlugcats.MSCRoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            orig(room);

            if (room.game.session is StoryGameSession story)
            {
                string name = room.abstractRoom.name;

                if (Plugin.lghtbrpup == story.game.StoryCharacter)
                {
                    if ("GATE_SB_OE" == story.saveState.denPosition)
                    {
                        if ("GATE_SB_OE" == name)
                            room.AddObject(new LightpupGateStart(room));
                        else if ("SB_GOR01" == name) //showing up in SB_GOR02, may need to specify screen
                            room.AddObject(new LightpupPuffBallTutorial(room));
                    }

                    else if ("OE_FINAL03" == story.saveState.denPosition)
                    {
                        if ("OE_FINAL03" == name)
                            room.AddObject(new LightpupOEStart(room));
                    }

                    if (SlugBaseCharacter.TryGet(room.world.game.StoryCharacter, out SlugBaseCharacter charac)
                        && charac.Features.TryGet(GameFeatures.StartRoom, out string[] den)
                        && den[0] == "OE_FINAL03")
                    {
                        if ("OE_CAVE03" == name)
                            room.AddObject(new LightpupOEPearlWarning(room));
                        //else if (OE_FINAL03 && Utility.ProgressionUnlocked(story))
                        //        //add ending
                    }
                }
                else if (Plugin.sproutcat == story.game.StoryCharacter)
                {
                    if ("LC_FINAL" == story.saveState.denPosition)
                        room.AddObject(new SproutcatLCStart(room));
                    //else if progression unlocked && the LC echo room && echo not in room/got LC echo
                }
            }

        } //end class RoomScripts
    }
}
