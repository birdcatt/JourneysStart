using JourneysStart.FisobsItems.Taser;
using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
//using MSC_AbstractObjectType = MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType;
using static JourneysStart.Slugcats.Lightbringer.PlayerStuff.Crafting;
using static JourneysStart.Utility;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using JourneysStart.Slugcats.Lightbringer.PlayerStuff;
using JourneysStart.Slugcats.Outgrowth.PlayerStuff;

namespace JourneysStart.Shared.PlayerStuff;

public class Crafting
{
    public static void Hook()
    {
        On.Player.GraspsCanBeCrafted += Player_GraspsCanBeCrafted;
        IL.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;

        On.Player.SwallowObject += Player_SwallowObject;
    }

    #region crafting
    public static bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
    {
        bool val = orig(self);

        if (Plugin.sproutcat == self.slugcatStats.name)
        {
            foreach (Creature.Grasp grasp in self.grasps)
            {
                if (grasp?.grabbed is Spear)
                    return true;
            }
            return false;
        }
        else if (Plugin.lghtbrpup == self.slugcatStats.name)
        {
            if (-1 != self.FreeHand() || self.input[0].y <= 0)
                return false;

            if (self.grasps[0].grabbed.abstractPhysicalObject is TaserAbstract t1 && self.grasps[1].grabbed.abstractPhysicalObject is TaserAbstract t2)
            {
                if (t1.electricCharge == 0 || t2.electricCharge == 0)
                    return false;
            }

            bool graspIsEdible = false;
            bool graspIsSpear = false;
            bool graspIsFullElec = false; //electric spear or taser
            bool graspIsRock = false;
            bool graspIsElecAnyCharge = false; //electric with charge > 0

            for (int i = 0; i < 2; i++)
            {
                Creature.Grasp grasp = self.grasps[i];

                if (null == grasp)
                    return false;

                AbstractObjectType abstrType = grasp.grabbed.abstractPhysicalObject.type;

                if (abstrType == AbstractObjectType.Spear)
                {
                    if (!graspIsElecAnyCharge)
                        graspIsElecAnyCharge = GraspIsSpearWithCharge(grasp);
                    if (!graspIsFullElec)
                        graspIsFullElec = GraspIsElectricSpearFullCharge(grasp);
                    graspIsSpear = true;
                }
                else if (abstrType == TaserFisob.AbstrTaser)
                {
                    if (!graspIsElecAnyCharge)
                        graspIsElecAnyCharge = GraspIsTaserWithCharge(grasp);
                    if (!graspIsFullElec)
                        graspIsFullElec = GraspIsTaserFullCharge(grasp);
                }
                else if (grasp.grabbed is IPlayerEdible) //yoo thanks NaClO
                    graspIsEdible = true;
                else if (abstrType == AbstractObjectType.Rock)
                    graspIsRock = true;
            }

            if (graspIsEdible && (graspIsFullElec && self.FoodInStomach < self.MaxFoodInStomach || graspIsSpear && self.FoodInStomach < 1))
                return false;

            if (graspIsRock && graspIsSpear && !graspIsElecAnyCharge) //rock + elec spear = shaving the spear
                return false;

            return null != CraftingLibrary.GetObjectType(self.grasps[0], self.grasps[1]);
        }

        return val;
    }

    public static void Player_SpitUpCraftedObject(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player self) =>
        {
            return Plugin.lghtbrpup == self.slugcatStats.name || Plugin.sproutcat == self.slugcatStats.name;
        });
        c.Emit(OpCodes.Brfalse, label);

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player self) =>
        {
            if (Plugin.lghtbrpup == self.slugcatStats.name)
                self.LightpupCrafting();
            else if (Plugin.sproutcat == self.slugcatStats.name)
                self.SproutcatCrafting();
        });
        c.Emit(OpCodes.Ret);

        c.MarkLabel(label);
    }
    #endregion

    public static void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        orig(self, grasp);

        if (Plugin.sproutcat == self.slugcatStats.name)
        {
            if (AbstractObjectType.FlareBomb == self.objectInStomach.type)
            {
                self.objectInStomach = new AbstractPhysicalObject(self.room.world, AbstractObjectType.Lantern, null, self.abstractCreature.pos, self.room.game.GetNewID());
                self.SubtractFood(1);
            }
            else if (AbstractObjectType.FlyLure == self.objectInStomach.type)
            {
                self.objectInStomach = new AbstractConsumable(self.room.world, AbstractObjectType.FirecrackerPlant, null, self.abstractCreature.pos, self.room.game.GetNewID(), -1, -1, null);
                self.SubtractFood(1);
            }
            else if (AbstractObjectType.PuffBall == self.objectInStomach.type)
            {
                self.objectInStomach = null;
                self.AddFood(1);
            }
        }
        else if (Plugin.lghtbrpup == self.slugcatStats.name)
        {
            if (self.objectInStomach is TaserAbstract abstrTaser && abstrTaser.electricCharge == 0)
            {
                abstrTaser.electricCharge = 3;
                self.SubtractFood(1);
            }
        }
    }
}
