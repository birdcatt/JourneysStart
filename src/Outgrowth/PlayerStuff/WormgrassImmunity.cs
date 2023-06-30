using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;

namespace JourneysStart.Outgrowth.PlayerStuff;

public class WormgrassImmunity
{
    public static void Hook()
    {
        //this is the better Wormgrass Immunity mod btw
        IL.WormGrass.WormGrassPatch.InteractWithCreature += WormGrassPatch_InteractWithCreature;
        On.WormGrass.WormGrassPatch.Update += WormGrassPatch_Update;
        On.WormGrass.WormGrassPatch.AlreadyTrackingCreature += WormGrassPatch_AlreadyTrackingCreature;
    }

    public static void WormGrassPatch_InteractWithCreature(ILContext il)
    {
        //another day another dont call orig
        //theres no new code, its just skip orig
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        c.Emit(OpCodes.Ldarg_1);
        c.EmitDelegate((WormGrass.WormGrassPatch.CreatureAndPull creatureAndPull) =>
        {
            return Utility.IsSproutcat(creatureAndPull.creature);
        });
        c.Emit(OpCodes.Brfalse_S, label);
        c.Emit(OpCodes.Ret);
        c.MarkLabel(label);
    }
    public static void WormGrassPatch_Update(On.WormGrass.WormGrassPatch.orig_Update orig, WormGrass.WormGrassPatch self)
    {
        foreach (WormGrass.WormGrassPatch.CreatureAndPull crit in self.trackedCreatures)
        {
            if (Utility.IsSproutcat(crit.creature))
                self.trackedCreatures.Remove(crit);
        }
    }
    public static bool WormGrassPatch_AlreadyTrackingCreature(On.WormGrass.WormGrassPatch.orig_AlreadyTrackingCreature orig, WormGrass.WormGrassPatch self, Creature creature)
    {
        bool val = orig(self, creature);
        if (self.trackedCreatures.Any(creatureAndPull => creatureAndPull.creature == creature && Utility.IsSproutcat(creature)))
            return true;
        return val;
    }
}
