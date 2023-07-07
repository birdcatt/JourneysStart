using MonoMod.Cil;
using Mono.Cecil.Cil;
using static JourneysStart.Plugin;

namespace JourneysStart.Outgrowth.PlayerStuff.PlayerGraf;

public class RopeHooks
{
    public static void Hook()
    {
        //all these IL's just for sfx
        IL.Player.Tongue.AttachToChunk += Tongue_AttachToChunk;
        IL.Player.Tongue.AttachToTerrain += Tongue_AttachToTerrain;
        IL.Player.Tongue.Release += Tongue_Release;
        IL.Player.Tongue.Shoot += Tongue_Shoot;
    }

    #region sfx
    public static void Tongue_AttachToChunk(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Room>("PlaySound"));
        c.MarkLabel(label);

        c.GotoPrev(MoveType.Before,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<Player.Tongue>("player"),
            i => i.MatchLdfld<UpdatableAndDeletable>("room"),
            i => i.MatchLdsfld<SoundID>("Tube_Worm_Tongue_Hit_Creature"));

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player.Tongue self) =>
        {
            if (sproutcat == self.player.SlugCatClass)
            {
                self.player.room.PlaySound(sproutcat_bush_rustle3, self.pos);
                return true;
            }
            return false;
        });
        c.Emit(OpCodes.Brtrue_S, label);
    }
    public static void Tongue_AttachToTerrain(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Room>("PlaySound"));
        c.MarkLabel(label);

        c.GotoPrev(MoveType.Before,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<Player.Tongue>("player"),
            i => i.MatchLdfld<UpdatableAndDeletable>("room"),
            i => i.MatchLdsfld<SoundID>("Tube_Worm_Tongue_Hit_Terrain"));

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player.Tongue self) =>
        {
            if (sproutcat == self.player.SlugCatClass)
            {
                self.player.room.PlaySound(sproutcat_bush_rustle2, self.pos);
                return true;
            }
            return false;
        });
        c.Emit(OpCodes.Brtrue_S, label);
    }
    public static void Tongue_Release(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel sound1 = il.DefineLabel();
        ILLabel sound2 = il.DefineLabel();

        c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Room>("PlaySound"));
        c.MarkLabel(sound1);
        c.GotoPrev(MoveType.Before,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<Player.Tongue>("player"),
            i => i.MatchLdfld<UpdatableAndDeletable>("room"));

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player.Tongue self) =>
        {
            //og sound: Tube_Worm_Detatch_Tongue_Creature
            if (sproutcat == self.player.SlugCatClass)
            {
                self.player.room.PlaySound(sproutcat_bush_rustle1, self.pos);
                return true;
            }
            return false;
        });
        c.Emit(OpCodes.Brtrue_S, sound1);
        //dont infinite loop on me
        c.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Room>("PlaySound"));

        c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Room>("PlaySound"));
        c.MarkLabel(sound2);
        c.GotoPrev(MoveType.Before,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<Player.Tongue>("player"),
            i => i.MatchLdfld<UpdatableAndDeletable>("room"));

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player.Tongue self) =>
        {
            //og sound: Tube_Worm_Detach_Tongue_Terrain
            if (sproutcat == self.player.SlugCatClass)
            {
                self.player.room.PlaySound(sproutcat_bush_rustle4, self.pos);
                return true;
            }
            return false;
        });
        c.Emit(OpCodes.Brtrue_S, sound2);
    }
    public static void Tongue_Shoot(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        c.GotoNext(MoveType.After, i => i.MatchLdsfld<SoundID>("Tube_Worm_Shoot_Tongue"));
        c.GotoNext(MoveType.After, i => i.MatchPop());
        c.MarkLabel(label);
        c.GotoPrev(MoveType.Before,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<Player.Tongue>("player"),
            i => i.MatchLdfld<UpdatableAndDeletable>("room"),
            i => i.MatchLdsfld<SoundID>("Tube_Worm_Shoot_Tongue"));

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player.Tongue self) =>
        {
            if (sproutcat == self.player.SlugCatClass)
            {
                self.player.room.PlaySound(sproutcat_bush_rustle5, self.pos);
                return true;
            }
            return false;
        });
        c.Emit(OpCodes.Brtrue_S, label);
    }
    #endregion
}
