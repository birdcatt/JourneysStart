using System.Collections.Generic;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace JourneysStart.Slugcats.Outgrowth.PlayerStuff;

internal class PyroResist
{
    public static void Hook()
    {
        On.Explosion.Update += Explosion_Update;
        IL.Creature.Update += Creature_Update;
    }

    private static void Explosion_Update(On.Explosion.orig_Update orig, Explosion self, bool eu)
    {
        foreach (List<PhysicalObject> physObjList in self.room.physicalObjects)
        {
            foreach (PhysicalObject physObj in physObjList)
            {
                if (!(self.sourceObject != physObj && !physObj.slatedForDeletetion))
                    continue;

                if (physObj is not Player player)
                    continue;

                if (!Plugin.PlayerDataCWT.TryGetValue(player, out var pData) || !pData.IsSproutcat)
                    continue;

                pData.Sproutcat.pyroJump++;
            }
        }

        orig(self, eu);
    }

    private static void Creature_Update(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        if (!c.TryGotoNext(MoveType.Before,
            i => i.MatchCallOrCallvirt<Player>(nameof(Player.PyroDeath)),
            i => i.MatchBr(out label)
            ))
        {
            return;
        }

        if (!c.TryGotoPrev(MoveType.After,
            i => i.MatchCallOrCallvirt<Creature>("get_dead"),
            i => i.Match(OpCodes.Brtrue_S)
            ))
        {
            return;
        }

        c.Emit(OpCodes.Ldarg_0);

        c.EmitDelegate((Creature self) =>
        {
            if (self is not Player)
                return false;

            if (!Plugin.PlayerDataCWT.TryGetValue(self as Player, out var pData) || !pData.IsSproutcat)
                return false;

            if (pData.Sproutcat.pyroJump++ >= MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity.Value)
                return false;

            return true;
            //if true, skip dying
        });

        c.Emit(OpCodes.Brtrue, label);
    }
}
