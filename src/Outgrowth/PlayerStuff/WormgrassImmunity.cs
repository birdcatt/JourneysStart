using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;

namespace JourneysStart.Outgrowth.PlayerStuff;

public class WormgrassImmunity
{
    public static void Hook()
    {
        //this is the cooler-looking Wormgrass Immunity mod btw
        IL.WormGrass.WormGrassPatch.InteractWithCreature += WormGrassPatch_InteractWithCreature;
        On.WormGrass.WormGrassPatch.Update += WormGrassPatch_Update;
        On.WormGrass.WormGrassPatch.AlreadyTrackingCreature += WormGrassPatch_AlreadyTrackingCreature;
    }

    public static bool IsWormgrassImmune(Creature crit)
    {
        if (null == crit) return false;
        return Utility.IsSproutcat(crit) //is sproutcat
            || crit.grabbedBy.Any(grabbedBy => Utility.IsSproutcat(grabbedBy.grabber)) //grabbed by sproutcat
            || crit is Player player && Utility.IsSproutcat(player.onBack); //on sproutcat's back
    }

    public static void WormGrassPatch_InteractWithCreature(ILContext il)
    {
        //another day another dont call orig
        //theres no new code, its just skip orig
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        c.Emit(OpCodes.Ldarg_1); //put argument 1 on the stack
        c.EmitDelegate((WormGrass.WormGrassPatch.CreatureAndPull creatureAndPull) =>
        {
            //pop argument 1 (creatureAndPull) off the stack
            return IsWormgrassImmune(creatureAndPull.creature); //push a bool onto the stack
        });
        c.Emit(OpCodes.Brfalse_S, label); //if bool is false, go to orig
        c.Emit(OpCodes.Ret); //return
        c.MarkLabel(label);
    }
    public static void WormGrassPatch_Update(On.WormGrass.WormGrassPatch.orig_Update orig, WormGrass.WormGrassPatch self)
    {
        orig(self);
        self.trackedCreatures.RemoveAll(critAndPull => IsWormgrassImmune(critAndPull.creature));
    }
    public static bool WormGrassPatch_AlreadyTrackingCreature(On.WormGrass.WormGrassPatch.orig_AlreadyTrackingCreature orig, WormGrass.WormGrassPatch self, Creature creature)
    {
        bool val = orig(self, creature);
        if (IsWormgrassImmune(creature) && self.trackedCreatures.Any(creatureAndPull => creatureAndPull.creature == creature))
            return true;
        return val;
    }
}
