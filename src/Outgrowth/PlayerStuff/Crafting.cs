using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using Debug = UnityEngine.Debug;
using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
using MSC_AbstractObjectType = MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType;
using JourneysStart.FisobsItems.Taser;
using static JourneysStart.Utility;

namespace JourneysStart.Outgrowth.PlayerStuff;

public static class Crafting
{
    public static void Hook()
    {
        IL.Player.GrabUpdate += Player_GrabUpdate;
    }

    public static void Player_GrabUpdate(ILContext il)
    {
        try
        {
            ILCursor c = new(il);
            ILLabel label = il.DefineLabel();

            if (!c.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Player>("FreeHand"), i => i.MatchLdcI4(-1), i => i.Match(OpCodes.Beq_S)))
            {
                Plugin.Logger.LogError("Unable to find Player.FreeHand() == -1 in ILhook");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Player, bool>>(self =>
            {
                return Plugin.sproutcat == self.slugcatStats.name;
            });
            c.Emit(OpCodes.Brtrue_S, label);

            c.GotoNext(MoveType.Before, i => i.MatchLdarg(0), i => i.MatchCallOrCallvirt<Player>("GraspsCanBeCrafted"));
            c.MarkLabel(label);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public static void SproutcatCrafting(this Player self)
    {
        self.room.PlaySound(SoundID.Spear_Fragment_Bounce, self.mainBodyChunk);
        AddCraftingSpark(self);

        for (int i = 0; i < self.grasps.Length; i++)
        {
            AbstractPhysicalObject grasp = self.grasps[i]?.grabbed.abstractPhysicalObject;

            if (grasp == null)
                continue;

            if (grasp is AbstractSpear spear)
            {
                EntityID id = self.room.game.GetNewID();
                AbstractPhysicalObject item;

                if (spear.explosive)
                    item = new AbstractConsumable(self.room.world, AbstractObjectType.ScavengerBomb, null, self.abstractCreature.pos, id, -1, -1, null);
                else if (spear.electric)
                {
                    item = new TaserAbstract(self.room.world, self.abstractCreature.pos, id)
                    {
                        electricCharge = spear.electricCharge
                    };
                }
                else if (spear.hue > 0)
                    item = new AbstractConsumable(self.room.world, MSC_AbstractObjectType.FireEgg, null, self.abstractCreature.pos, id, -1, -1, null);
                else //change it to be the broken spear fisob
                    item = new(self.room.world, AbstractObjectType.Rock, null, self.abstractCreature.pos, id);

                if (self.room.game.session is StoryGameSession story)
                    story.RemovePersistentTracker(grasp);

                self.ReleaseGrasp(i);

                grasp.LoseAllStuckObjects();
                grasp.realizedObject.RemoveFromRoom();
                self.room.abstractRoom.RemoveEntity(grasp);

                SpawnItemInHand(self, item);
            }
        }
    }
}
