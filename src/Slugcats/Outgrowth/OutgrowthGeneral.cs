using MonoMod.RuntimeDetour;
using System.Reflection;
//using MonoMod.Cil;
//using Mono.Cecil.Cil;
using Debug = UnityEngine.Debug;
using JourneysStart.Slugcats.Outgrowth.Food;
using JourneysStart.Slugcats.Outgrowth.PlayerStuff;
using JourneysStart.Slugcats.Outgrowth.Rope;

namespace JourneysStart.Slugcats.Outgrowth
{
    public class OutgrowthGeneral
    {
        public static void Hook()
        {
            Diet.Hook();
            SeedSpitup.Hook();

            PyroResist.Hook();
            WormgrassImmunity.Hook();
            Crafting.Hook();

            RopeHooks.Hook();
            VineCombat.Hook();

            GeneralHooks();
        }
        public static void GeneralHooks()
        {
            new Hook(typeof(RegionGate).GetProperty("MeetRequirement", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(OutgrowthGeneral).GetMethod("RegionGate_MeetRequirement_get", BindingFlags.Static | BindingFlags.Public));

            On.Player.ThrowObject += Player_ThrowObject;
        }

        public delegate bool orig_RegionGate_MeetRequirement(RegionGate self);
        public static bool RegionGate_MeetRequirement_get(orig_RegionGate_MeetRequirement orig, RegionGate self)
        {
            bool val = orig(self);
            if (ModManager.MSC && self.room.game.session is StoryGameSession story && Utility.IsSproutcat(self.room.game.StoryCharacter)
                && self.karmaRequirements[!self.letThroughDir ? 1 : 0] == MoreSlugcats.MoreSlugcatsEnums.GateRequirement.RoboLock
                && "UW" == self.room.world.region.name && self.room.abstractRoom.name.Contains("LC")
                && story.saveState.hasRobo)
            {
                return true;
            }
            return val;
        }
        public static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            if (self.grasps[grasp]?.grabbed is Spear && Plugin.sproutcat == self.slugcatStats.name)
            {
                self.TossObject(grasp, eu);
                self.ReleaseGrasp(grasp);
                //returns in orig since grasp is null now
            }
            orig(self, grasp, eu);
        }
    }
}
