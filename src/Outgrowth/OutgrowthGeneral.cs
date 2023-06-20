using MonoMod.RuntimeDetour;
using System.Reflection;
using JourneysStart.Outgrowth.PlayerStuff;
using JourneysStart.Outgrowth.Food;

namespace JourneysStart.Outgrowth
{
    public class OutgrowthGeneral
    {
        public static void Hook()
        {
            //HooksSeed.Hook();
            Diet.Hook();
            SeedSpitup.Hook();
            Movement.Hook();
            GeneralHooks();
        }
        public static void GeneralHooks()
        {
            new Hook(typeof(RegionGate).GetProperty("MeetRequirement", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(OutgrowthGeneral).GetMethod("RegionGate_MeetRequirement_get", BindingFlags.Static | BindingFlags.Public));
        }

        public delegate bool orig_RegionGate_MeetRequirement(RegionGate self);
        public static bool RegionGate_MeetRequirement_get(orig_RegionGate_MeetRequirement orig, RegionGate self)
        {
            bool val = orig(self);

            if (ModManager.MSC && Utility.SlugIsSprout(self.room.game.StoryCharacter)
                && self.karmaRequirements[(!self.letThroughDir) ? 1 : 0] == MoreSlugcats.MoreSlugcatsEnums.GateRequirement.RoboLock
                && "UW" == self.room.world.region.name && self.room.abstractRoom.name.Contains("LC")
                && self.room.game.GetStorySession.saveState.hasRobo)
            {
                return true;
            }

            return val;
        }
    }
}
