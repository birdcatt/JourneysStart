using JourneysStart.Lightbringer.FisobsTaser;
using JourneysStart.Lightbringer.Data;
using SlugBase.Features;
using SlugBase;
using System.Linq;
using JourneysStart.Lightbringer.OracleStuff;
using JourneysStart.Lightbringer.PlayerStuff;

namespace JourneysStart.Lightbringer
{
    public class LightpupGeneral
    {
        public static void Hook()
        {
            FRDData.Hook();
            HooksTaser.Hook();
            Crafting.Hook();
            PearlDialogue.Hook();
            GeneralHooks();
        }

        public static void GeneralHooks()
        {
            On.WorldLoader.GeneratePopulation += WorldLoader_GeneratePopulation;

            On.Player.ThrownSpear += Player_ThrownSpear;
            On.Player.Grabability += Player_Grabability;
            On.Player.CanBeSwallowed += Player_CanBeSwallowed;

            On.PlayerGraphics.LookAtObject += PlayerGraphics_LookAtObject;

            On.SlugcatStats.PearlsGivePassageProgress += SlugcatStats_PearlsGivePassageProgress;

            On.RegionGate.customOEGateRequirements += RegionGate_customOEGateRequirements;
            On.RegionGate.customKarmaGateRequirements += RegionGate_customKarmaGateRequirements;
        }

        public static void WorldLoader_GeneratePopulation(On.WorldLoader.orig_GeneratePopulation orig, WorldLoader self, bool fresh)
        {
            //disable rot spawns even on modded regions except for iterator regions
            if (Utility.SlugIsLightpup(self.game.StoryCharacter) && !self.abstractRooms.Any(abstrRoom => abstrRoom.name == "AI"))
                self.spawners.RemoveAll(spawn => spawn is World.SimpleSpawner spawner && Utility.CreatureIsRot(StaticWorld.GetCreatureTemplate(spawner.creatureType).type));
            orig(self, fresh);
        }

        public static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self, spear);
            if (Plugin.lghtbrpup == self.slugcatStats.name)
            {
                spear.spearDamageBonus = (spear.spearDamageBonus + 0.8f) / 2; //1 is 0.8f, 2 is 1.25f
            }
        }

        public static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            return obj is Flare ? Player.ObjectGrabability.CantGrab : orig(self, obj);
        }
        public static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            return orig(self, testObj) || testObj is Taser;
        }
        public static void PlayerGraphics_LookAtObject(On.PlayerGraphics.orig_LookAtObject orig, PlayerGraphics self, PhysicalObject obj)
        {
            if (obj is not Flare)
                orig(self, obj);
        }

        public static bool SlugcatStats_PearlsGivePassageProgress(On.SlugcatStats.orig_PearlsGivePassageProgress orig, StoryGameSession session)
        {
            return orig(session) || Plugin.lghtbrpup == session.saveStateNumber;
        }
        public static bool RegionGate_customOEGateRequirements(On.RegionGate.orig_customOEGateRequirements orig, RegionGate self)
        {
            return orig(self) || Utility.ProgressionUnlocked(self.room.game);
        }
        public static void RegionGate_customKarmaGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            orig(self);

            if (SlugBaseCharacter.TryGet(self.room.world.game.StoryCharacter, out SlugBaseCharacter charac)
                && charac.Features.TryGet(GameFeatures.StartRoom, out string[] den)
                && den[0] == self.room.abstractRoom.name && "GATE_SB_OE" == self.room.abstractRoom.name)
            {
                self.karmaRequirements[0] = RegionGate.GateRequirement.OneKarma; //oe side of the gate
            }
        }
    }
}
