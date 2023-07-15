using SlugBase.Features;
using SlugBase;
using System.Linq;
using JourneysStart.FisobsItems.Taser;
using JourneysStart.Slugcats.Lightbringer.MiscData;
using JourneysStart.Slugcats.Lightbringer.PlayerStuff;

namespace JourneysStart.Slugcats.Lightbringer
{
    public class LightpupGeneral
    {
        public static void Hook()
        {
            FRDData.Hook();
            Crafting.Hook();
            ElecResist.Hook();
            GeneralHooks();
        }

        public static void GeneralHooks()
        {
            On.WorldLoader.GeneratePopulation += WorldLoader_GeneratePopulation;

            On.Player.ThrownSpear += Player_ThrownSpear;
            On.Player.CanBeSwallowed += Player_CanBeSwallowed;

            On.PlayerGraphics.LookAtObject += PlayerGraphics_LookAtObject;

            On.SlugcatStats.PearlsGivePassageProgress += SlugcatStats_PearlsGivePassageProgress;

            On.RegionGate.customOEGateRequirements += RegionGate_customOEGateRequirements;
            On.RegionGate.customKarmaGateRequirements += RegionGate_customKarmaGateRequirements;
        }

        public static bool IsAIRoom(string name)
        {
            if (name.Length < "EX_AI".Length) //EX for example
                return false;

            int len = name.Length - 1;

            return name[len - 2] == '_' && name[len - 1] == 'A' && name[len] == 'I';
        }

        public static void WorldLoader_GeneratePopulation(On.WorldLoader.orig_GeneratePopulation orig, WorldLoader self, bool fresh)
        {
            //disable rot spawns even on modded regions except for iterator regions
            if (Utility.IsLightpup(self.game.StoryCharacter)
                && !self.abstractRooms.Any(abstrRoom => IsAIRoom(abstrRoom.name)))
            {
                self.spawners.RemoveAll(spawn => spawn is World.SimpleSpawner spawner && Utility.CreatureIsRot(StaticWorld.GetCreatureTemplate(spawner.creatureType).type));
            }
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
