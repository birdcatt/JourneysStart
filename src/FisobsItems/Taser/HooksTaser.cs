using static ScavengerAI;
using UnityEngine;
//using Colour = UnityEngine.Color;
using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
using JourneysStart.Lightbringer.Data;
using static JourneysStart.Utility;

namespace JourneysStart.FisobsItems.Taser;

public class HooksTaser
{
    public static void Hook()
    {
        On.Lantern.HitByWeapon += Lantern_HitByWeapon;
        On.MoreSlugcats.ElectricSpear.DrawSprites += ElectricSpear_DrawSprites;
        On.ScavengerAI.WeaponScore += ScavengerAI_WeaponScore;
        On.ScavengerAI.CollectScore_PhysicalObject_bool += ScavengerAI_CollectScore_PhysicalObject_bool;
    }

    public static void Lantern_HitByWeapon(On.Lantern.orig_HitByWeapon orig, Lantern self, Weapon weapon)
    {
        orig(self, weapon);
        if (self.room.game.IsStorySession && SlugIsMod(self.room.game.StoryCharacter))
        {
            Random.InitState(Time.time.GetHashCode());
            if (weapon is ExplosiveSpear || weapon is ScavengerBomb || Random.value < 0.2f)
            {
                World world = self.room.world;
                WorldCoordinate coord = self.room.GetWorldCoordinate(self.firstChunk.pos);

                GlowingSlimeMold glowingSlimeMold = new(new AbstractConsumable(world, AbstractObjectType.SlimeMold, null, coord, self.room.game.GetNewID(), -1, -1, null));
                ResilientFlarebomb resilientFlareBomb = new(new AbstractConsumable(world, AbstractObjectType.FlareBomb, null, coord, self.room.game.GetNewID(), -1, -1, null));

                if (Random.value < 0.9f)
                {
                    SpawnItem(self.room, glowingSlimeMold.abstractPhysicalObject);
                    SpawnItem(self.room, resilientFlareBomb.abstractPhysicalObject);
                }
                else if (Random.value < 0.7f)
                    SpawnItem(self.room, glowingSlimeMold.abstractPhysicalObject);
                else
                {
                    SpawnItem(self.room, resilientFlareBomb.abstractPhysicalObject);
                }

                if (Random.value < 0.1f)
                {
                    GlowingSlimeMold glowingSlimeMold2 = new(new AbstractConsumable(world, AbstractObjectType.SlimeMold, null, coord, self.room.game.GetNewID(), -1, -1, null));
                    SpawnItem(self.room, glowingSlimeMold2.abstractPhysicalObject);
                }

                self.room.PlaySound(SoundID.Fire_Spear_Pop, self.firstChunk.pos);
                self.Destroy();
            }
        }
    }

    public static void ElectricSpear_DrawSprites(On.MoreSlugcats.ElectricSpear.orig_DrawSprites orig, MoreSlugcats.ElectricSpear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.room.game.IsStorySession && SlugIsMod(self.room.game.StoryCharacter))
        {
            int electricCharge = self.abstractSpear.electricCharge;
            if (electricCharge < 3)
            {
                for (int i = 0; i < self.segments; i++)
                {
                    SetChargeDependantElectricColour(sLeaser, rCam, 1 + i, electricCharge);
                }
            }
        }
    }

    public static int ScavengerAI_WeaponScore(On.ScavengerAI.orig_WeaponScore orig, ScavengerAI self, PhysicalObject obj, bool pickupDropInsteadOfWeaponSelection)
    {
        if (obj is Taser t)
        {
            if (t.abstractTaser.electricCharge > 0)
            {
                //from orig spear section
                if (!pickupDropInsteadOfWeaponSelection && (self.currentViolenceType == ViolenceType.NonLethal || self.currentViolenceType == ViolenceType.ForFun))
                {
                    return 2;
                }
                return 3;
            }

            //from orig rock section
            if (self.currentViolenceType == ViolenceType.NonLethal)
            {
                for (int i = 0; i < self.scavenger.grasps.Length; i++)
                {
                    if (self.scavenger.grasps[0] == null)
                    {
                        return 4;
                    }
                }
            }
            return 2;
        }
        return orig(self, obj, pickupDropInsteadOfWeaponSelection);
    }

    public static int ScavengerAI_CollectScore_PhysicalObject_bool(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
    {
        if (obj is Taser taser)
        {
            //from orig
            if (self.scavenger.room != null)
            {
                SocialEventRecognizer.OwnedItemOnGround ownedItemOnGround = self.scavenger.room.socialEventRecognizer.ItemOwnership(obj);
                if (ownedItemOnGround != null && ownedItemOnGround.offeredTo != null && ownedItemOnGround.offeredTo != self.scavenger)
                {
                    return 0;
                }
            }

            if (taser.abstractTaser.electricCharge > 0)
            {
                return Mathf.Min(taser.abstractTaser.electricCharge + 1, 3); //max 3 score
            }

            //from orig rock section
            int num = 0;
            for (int i = 0; i < self.scavenger.grasps.Length; i++)
            {
                if (self.scavenger.grasps[i] != null
                    && (self.scavenger.grasps[i].grabbed is Rock || self.scavenger.grasps[i].grabbed is Taser t && t.abstractTaser.electricCharge == 0)
                    && self.scavenger.grasps[i].grabbed != obj)
                {
                    num++;
                }
            }
            if (num >= (self.creature.abstractAI as ScavengerAbstractAI).carryRocks)
            {
                return 0;
            }
            return 1;
        }

        return orig(self, obj, weaponFiltered);
    }
}
