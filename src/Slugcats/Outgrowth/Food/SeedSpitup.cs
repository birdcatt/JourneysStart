using MonoMod.Cil;
using Mono.Cecil.Cil;
using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
using System;
using Debug = UnityEngine.Debug;
using JourneysStart.FisobsItems.Seed;
using JourneysStart.Shared.PlayerStuff;
using Colour = UnityEngine.Color;

namespace JourneysStart.Slugcats.Outgrowth.Food;

public class SeedSpitup
{
    public static void Hook()
    {
        On.Player.Regurgitate += Player_Regurgitate;
        IL.Player.GrabUpdate += Player_GrabUpdate;
        IL.PlayerGraphics.Update += PlayerGraphics_Update;
    }

    public static bool CanRegurgitate(Player self)
    {
        return Plugin.PlayerDataCWT.TryGetValue(self, out PlayerData p)
            && p.IsSproutcat
            && (p.Sproutcat.seedSpitUpMax > 0 || self.FoodInStomach > 0)
            && null == self.objectInStomach
            && -1 != self.FreeHand();
    }

    #region oatmealine's regurgitation code from modding academy (thanks a lot!)
    public static void Player_Regurgitate(On.Player.orig_Regurgitate orig, Player self)
    {
        if (self.objectInStomach == null && CanRegurgitate(self) && Plugin.PlayerDataCWT.TryGetValue(self, out PlayerData p) && p.IsSproutcat)
        {
            if (p.Sproutcat.seedSpitUpMax > 0)
            {
                p.Sproutcat.seedSpitUpMax--;
                Debug.Log($"{Plugin.MOD_NAME}: (Outgrowth, Player_Regurgitate) Regurgitating free seed ({p.Sproutcat.seedSpitUpMax} left)");
            }
            else
            {
                self.SubtractFood(1);
                Debug.Log($"{Plugin.MOD_NAME}: (Outgrowth, Player_Regurgitate) Regurgitating seed, subtracted food");
            }
            Colour baseCol = Utility.GetSlugcatColour(self, 0);
            Colour seedCol = Utility.GetSlugcatColour(self, 2);
            self.objectInStomach = new SeedAbstract(self.room.world, self.abstractCreature.pos, self.room.game.GetNewID(), baseCol, seedCol);
        }
        orig(self);
    }
    public static void Player_GrabUpdate(ILContext il)
    {
        ILCursor c = new(il);

        // replace all mentions of `isGourmand` with the equivalent of `(isGourmand || CanRegurgitate(player))`

        // match `isGourmand`, brfalse edition
        while (
            c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchCallOrCallvirt<Player>("get_isGourmand"),
                i => i.Match(OpCodes.Brfalse_S) || i.Match(OpCodes.Brfalse)
            )
        )
        {
            // this is the condition we should skip to if our check succeeds, replicating the behavior if the vanilla check was to succeed
            ILLabel skipGourmandCond = c.MarkLabel();
            c.GotoPrev(MoveType.Before, i => i.MatchLdarg(0), i => i.Match(OpCodes.Call) || i.Match(OpCodes.Callvirt));

            // insert the condition
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(CanRegurgitate);

            // if it's true, skip ahead
            c.Emit(OpCodes.Brtrue_S, skipGourmandCond);

            // move forwards to avoid an infloop
            c.GotoNext(MoveType.After, i => i.Match(OpCodes.Brfalse_S) || i.Match(OpCodes.Brfalse));
        }

        c.Index = 0;

        // match `isGourmand`, brtrue edition
        while (
            c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchCallOrCallvirt<Player>("get_isGourmand"),
                i => i.Match(OpCodes.Brtrue_S) || i.Match(OpCodes.Brtrue)
            )
        )
        {
            // a lot easier here, since you can just insert another cond

            ILLabel proceedCond = c.Prev.Operand as ILLabel;

            // insert the condition
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Player, bool>>(player =>
            {
                return CanRegurgitate(player);
            });
            // if it's true, proceed as usual
            c.Emit(OpCodes.Brtrue_S, proceedCond);
        }
    }
    public static void PlayerGraphics_Update(ILContext il)
    {
        ILCursor c = new(il);

        // match for `player.objectInStomach != null`
        c.GotoNext(MoveType.After,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<PlayerGraphics>("player"),
            i => i.MatchLdfld<Player>("objectInStomach"),
            i => i.Match(OpCodes.Brtrue_S)
        );

        // match for `player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand ...`
        c.GotoNext(MoveType.After,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<PlayerGraphics>("player"),
            i => i.MatchLdfld<Player>("SlugCatClass"),
            i => i.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Gourmand"),
            i => i.Match(OpCodes.Call), // this is a mess of generics; not matching this, but it's the equation call
            i => i.Match(OpCodes.Brtrue_S)
        );

        ILLabel proceedCond = c.Prev.Operand as ILLabel;

        // insert our condition
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<PlayerGraphics, bool>>(playerGraphics =>
        {
            return CanRegurgitate(playerGraphics.player);
        });
        // if it's true, proceed as usual
        c.Emit(OpCodes.Brtrue_S, proceedCond);
    }
    #endregion
}
