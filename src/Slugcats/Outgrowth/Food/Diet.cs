using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using Debug = UnityEngine.Debug;
using static JourneysStart.Utility;
using JourneysStart.Shared.PlayerStuff;

namespace JourneysStart.Slugcats.Outgrowth.Food;

public class Diet
{
    public static void Hook()
    {
        //On.Player.ObjectEaten hook in SharedGeneral
        IL.Player.ObjectEaten += Player_ObjectEaten;
        //IL.Player.BiteEdibleObject += Player_BiteEdibleObject; //this dont work lol
    }

    public static void Player_ObjectEaten(ILContext il)
    {
        try
        {
            //if already ate a bug, dont add food pips
            ILCursor c = new(il);
            ILLabel label = il.DefineLabel();

            c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<RainWorldGame>("get_IsStorySession"), i => i.Match(OpCodes.Brfalse_S));

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<Player, IPlayerEdible, bool>>((self, eatenobject) =>
            {
                return Plugin.PlayerDataCWT.TryGetValue(self, out PlayerData p) && p.IsSproutcat && p.Sproutcat.CannotEatBugsThisCycle(eatenobject);
            });
            c.Emit(OpCodes.Brtrue_S, label);

            c.GotoNext(MoveType.Before, i => i.MatchLdsfld<ModManager>("MSC"));
            c.MarkLabel(label);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    //public static void Player_BiteEdibleObject(ILContext il)
    //{
    //    //try
    //    //{
    //        //dont eat the object if its a bug or nourishment is 0
    //        ILCursor c = new(il);
    //        ILLabel label = il.DefineLabel();

    //        if (!c.TryGotoNext(MoveType.Before,
    //            i => i.MatchLdarg(0), i => i.MatchCallOrCallvirt<Creature>("get_grasps"), i => i.MatchLdloc(0), i => i.MatchLdelemRef(),
    //            i => i.MatchLdfld<Creature.Grasp>("grabbed"), i => i.MatchIsinst("IPlayerEdible"),
    //            i => i.MatchCallOrCallvirt<IPlayerEdible>("get_BitesLeft"), i => i.MatchLdcI4(1), i => i.Match(OpCodes.Bne_Un_S)))
    //        {
    //            Debug.Log($"{Plugin.MOD_NAME}: (Outgrowth, Player_BiteEdibleObject IL) Unable to find \"(base.grasps[i].grabbed as IPlayerEdible).BitesLeft == 1\"");
    //        }

    //        //push (base.grasps[i].grabbed as IPlayerEdible)
    //        c.Emit(OpCodes.Ldarg_0); //push player
    //        c.Emit(OpCodes.Ldarg_0); //use this to get grasp.grabbed as IPlayerEdible
    //        c.Emit(OpCodes.Call, typeof(Creature).GetProperty(nameof(Creature.grasps))); //or is it GetProperty("get_grasps")
    //        c.Emit(OpCodes.Ldloc_0);
    //        c.Emit(OpCodes.Ldelem_Ref);
    //        c.Emit(OpCodes.Ldfld, typeof(Creature.Grasp).GetField(nameof(Creature.Grasp.grabbed)));
    //        c.Emit(OpCodes.Isinst);
    //        c.EmitDelegate<Func<Player, IPlayerEdible, bool>>((self, edible) =>
    //        {
    //            return 0 == SlugcatStats.NourishmentOfObjectEaten(self.slugcatStats.name, edible) || Plugin.OutgrowthCWT.TryGetValue(self, out var p) && p.CannotEatBugsThisCycle(edible);
    //        });
    //        c.Emit(OpCodes.Brtrue, label);

    //        c.GotoNext(MoveType.Before, i => i.Match(OpCodes.Ret));
    //        c.MarkLabel(label);
    //    //}
    //    //catch (Exception e)
    //    //{
    //    //    Debug.LogException(e);
    //    //}
    //}

    /*public static void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
    {
        //dont eat things if it has a nourishment of 0 or if its a bug
        if (Plugin.sproutcat == self.slugcatStats.name && Plugin.OutgrowthCWT.TryGetValue(self, out var p))
        {
            foreach (Creature.Grasp grasp in self.grasps)
            {
                if (grasp?.grabbed is IPlayerEdible edible && edible.Edible
                    && (0 == SlugcatStats.NourishmentOfObjectEaten(self.slugcatStats.name, edible) || p.CannotEatBugsThisCycle(edible)))
                {
                    return;
                }
            }
        }
        orig(self, eu);
    }*/
}
