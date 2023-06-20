using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using Debug = UnityEngine.Debug;

namespace JourneysStart.Outgrowth.PlayerStuff;

public class Movement
{
    public static void Hook()
    {
        IL.Player.SaintTongueCheck += Player_SaintTongueCheck;
    }

    public static void Player_SaintTongueCheck(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        try
        {
            c.GotoNext(MoveType.Before, i => i.MatchLdarg(0), i => i.MatchLdfld<Player>("SlugCatClass"), i => i.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Saint"));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Player, bool>>((self) =>
            {
                return Plugin.sproutcat == self.slugcatStats.name;
            });
            c.Emit(OpCodes.Brtrue, label);

            c.GotoNext(MoveType.After, i => i.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Saint"), i => i.Match(OpCodes.Call), i => i.Match(OpCodes.Brfalse));
            c.MarkLabel(label);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
