using Debug = UnityEngine.Debug;
using static SSOracleBehavior;
using SSOracleBehaviorAction = MoreSlugcats.MoreSlugcatsEnums.SSOracleBehaviorAction;
using Action = SSOracleBehavior.Action;
using static JourneysStart.Utility;
using static JourneysStart.Slugcats.Lightbringer.MiscData.FRDData;
using static MoreSlugcats.MoreSlugcatsEnums;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace JourneysStart.Shared.OracleStuff
{
    public class OracleDialogue
    {
        public static void Hook()
        {
            IL.MoreSlugcats.OraclePanicDisplay.Update += OraclePanicDisplay_Update;
            On.SSOracleBehavior.SpecialEvent += SSOracleBehavior_SpecialEvent;

            On.SSOracleBehavior.SSSleepoverBehavior.ctor += SSSleepoverBehavior_ctor;
            On.SSOracleBehavior.Update += SSOracleBehavior_Update;
            On.SSOracleBehavior.ThrowOutBehavior.Update += ThrowOutBehavior_Update;

            On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
        }

        #region moon panic
        public static void OraclePanicDisplay_Update(ILContext il)
        {
            //another day another dont call orig
            //theres no new code, its just skip orig
            ILCursor c = new(il);
            ILLabel label = il.DefineLabel();

            c.Emit(OpCodes.Ldarg_0); //put argument 0 on the stack
            c.EmitDelegate((MoreSlugcats.OraclePanicDisplay self) =>
            {
                //pop argument 0 off the stack
                return IsLightpup(self.oracle.room.game.StoryCharacter); //push a bool onto the stack
            });
            c.Emit(OpCodes.Brfalse_S, label); //if bool is false, go to orig
            c.Emit(OpCodes.Ret); //return
            c.MarkLabel(label);
        }
        public static void SSOracleBehavior_SpecialEvent(On.SSOracleBehavior.orig_SpecialEvent orig, SSOracleBehavior self, string eventName)
        {
            if (IsLightpup(self.oracle.room.game.StoryCharacter) && "panic" == eventName)
            {
                Debug.Log($"{Plugin.MOD_NAME}: Prevented special event {eventName}");
                eventName = "NOO MOON DON'T PANIC (brought to you by " + Plugin.MOD_NAME + ")";
            }
            orig(self, eventName);
        }
        #endregion

        public static void SSSleepoverBehavior_ctor(On.SSOracleBehavior.SSSleepoverBehavior.orig_ctor orig, SSSleepoverBehavior self, SSOracleBehavior owner)
        {
            orig(self, owner);

            if (self.oracle.room.game.session is StoryGameSession story
                && Plugin.lghtbrpup == story.saveStateNumber
                && 0 == story.saveState.miscWorldSaveData.SLOracleState.playerEncounters)
            {
                story.saveState.miscWorldSaveData.SLOracleState.playerEncounters++;
                self.dialogBox.messages.RemoveAll(x => x.text == "Welcome back little messenger." || x.text == "Thank you for visiting me, but I'm afraid there is nothing here for you.");
            }
        }

        #region update methods
        public static void SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            if (IsLightpup(self.oracle.room.game.StoryCharacter))
            {
                if (Action.General_GiveMark == self.action)
                {
                    self.SlugcatEnterRoomReaction();
                    self.getToWorking = 0;
                    self.LockShortcuts();
                    self.movementBehavior = MovementBehavior.Talk;

                    self.NewAction(self.afterGiveMarkAction); //goes from General_GiveMark to General_MarkTalk
                    if (self.conversation != null)
                        self.conversation.paused = false;
                }
            }
            orig(self, eu);
        }

        public static void ThrowOutBehavior_Update(On.SSOracleBehavior.ThrowOutBehavior.orig_Update orig, ThrowOutBehavior self)
        {
            orig(self);

            if (IsLightpup(self.oracle.room.game.StoryCharacter))
            {
                self.owner.getToWorking = 1f;
                self.owner.UnlockShortcuts();

                if ("DM" == self.oracle.room.world.region.name)
                {
                    self.movementBehavior = MovementBehavior.Idle; //maybe Meditate? idk
                    self.owner.NewAction(SSOracleBehaviorAction.Moon_SlumberParty);
                }
                else if (self.owner.throwOutCounter == 700)
                    self.dialogBox.Interrupt("I told you to leave. This is your final warning.", 0);
                else if (self.owner.throwOutCounter > 980)
                    self.owner.NewAction(Action.ThrowOut_KillOnSight);
            }
        }
        #endregion

        #region convo
        public static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, PebblesConversation self)
        {
            Debug.Log($"{Plugin.MOD_NAME}: ConversationID is {self.id}");

            string regionName = self.owner.oracle.room.world.region.name;

            if (!(IsLightpup(self.owner.oracle.room.game.StoryCharacter) && (IsPebblesConvo() || IsMoonConvo())))
            {
                //region checking so this guy plays nice with custom iterators
                orig(self);
                return;
            }

            bool IsPebblesConvo()
            {
                return ("SS" == regionName || Oracle.OracleID.SS == self.owner.oracle.ID) && Conversation.ID.Pebbles_White == self.id;
            }
            bool IsMoonConvo()
            {
                return ("DM" == regionName || OracleID.DM == self.owner.oracle.ID)
                    && (ConversationID.MoonGiveMark == self.id || ConversationID.MoonGiveMarkAfter == self.id || Conversation.ID.Pebbles_White == self.id);
            }

            //wait for the player to be still
            self.events.Add(new PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 5));

            bool hasLightpupPearl = false;
            bool hasGenericPearl = false;
            foreach (Creature.Grasp grasp in self.owner.player.grasps)
            {
                if (grasp?.grabbed is DataPearl pearl)
                {
                    if (LightpupPearl == pearl.AbstractPearl.dataPearlType)
                    {
                        hasLightpupPearl = true;
                        break;
                    }
                    else
                        hasGenericPearl = true;
                }
            }

            if ("DM" == regionName || OracleID.DM == self.owner.oracle.ID)
            {
                self.owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.playerEncounters = 0; //set to 1 in SSSleepoverBehavior.ctor
                self.owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.playerEncountersWithMark = 0; //porl reading purposes

                self.LoadEventsFromFile("Lightpup_DM_FirstMeeting_0");
                if (hasLightpupPearl)
                {
                    if (!self.owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.significantPearls.Contains(LightpupPearl))
                        self.owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.significantPearls.Add(LightpupPearl);

                    self.owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.playerEncountersWithMark++; //bc im hijacking it for pearl reading purposes

                    self.LoadEventsFromFile(LightpupPearl.value);
                    self.LoadEventsFromFile("Lightpup_DM_UnlockProgression");
                    self.LoadEventsFromFile("Lightpup_DM_FirstMeeting_2_ProgressionUnlocked");
                }
                else
                {
                    if (hasGenericPearl)
                        self.LoadEventsFromFile("Lightpup_DM_FirstMeeting_1_ReadGenericPearl");
                    else
                        self.LoadEventsFromFile("Lightpup_DM_FirstMeeting_1_NoPearl");
                    self.LoadEventsFromFile("Lightpup_DM_FirstMeeting_2_ProgressionLocked");
                }
            }
            else
            {
                self.owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;

                self.LoadEventsFromFile("Lightpup_SS_FirstMeeting_0");
                if (hasLightpupPearl)
                {
                    self.LoadEventsFromFile("Lightpup_SS_FirstMeeting_1_ReadLightpupPearl");
                }
                else if (hasGenericPearl)
                    self.LoadEventsFromFile("Lightpup_SS_FirstMeeting_1_ReadGenericPearl");
                else
                    self.LoadEventsFromFile("Lightpup_SS_FirstMeeting_1_NoPearl");
                self.LoadEventsFromFile("Lightpup_SS_FirstMeeting_2");

                CreatureTemplate.Type critType = self.owner.CheckStrayCreatureInRoom();
                if (self.owner.CheckSlugpupsInRoom() || critType != CreatureTemplate.Type.StandardGroundCreature)
                {
                    self.AddMessage("And take them with you!");
                    if (CreatureIsRot(critType))
                        self.AddMessage("Where did you even find one?", textLinger: 10);
                    else
                        self.owner.CreatureJokeDialog();
                }
            }
        }
        #endregion convo
    }
}