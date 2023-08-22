using static JourneysStart.Plugin;
using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
using MSC_AbstractObjectType = MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType;
using JourneysStart.Slugcats.Lightbringer.MiscData;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using JourneysStart.Slugcats;

namespace JourneysStart.Shared.PlayerStuff;

internal class PlayerHooks
{
    public static void Hook()
    {
        On.Player.ctor += Player_ctor;
        On.Player.Update += Player_Update;
        On.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;

        On.Player.Grabability += Player_Grabability;
        On.Player.Jump += Player_Jump;
        On.Player.ObjectEaten += Player_ObjectEaten; //food reaction + eating a bug
        IL.Player.Die += Player_Die;
    }

    #region player
    #region ctor and update
    public static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        SlugcatStats.Name name = self.slugcatStats.name;
        if (Utility.IsModcat(name))
        {
            if (!PlayerDataCWT.TryGetValue(self, out _))
                PlayerDataCWT.Add(self, new(self));

            if (lghtbrpup == name)
            {
                self.setPupStatus(true); //thanks oatmealine
            }
            else if (sproutcat == name)
            {
                self.tongue = new(self, 0); //2nd arg is index of tongue (tubeworms have 2 tongues)
            }
        }
    }
    public static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.room != null && PlayerDataCWT.TryGetValue(self, out PlayerData playerData))
        {
            playerData.Update(eu);
        }
    }
    public static void Player_ClassMechanicsSaint(On.Player.orig_ClassMechanicsSaint orig, Player self)
    {
        if (self.room != null && PlayerDataCWT.TryGetValue(self, out PlayerData pData) && pData.IsSproutcat)
        {
            pData.Sproutcat.ClassMechanicsSproutcat();
        }
        orig(self);
    }
    #endregion

    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        var val = orig(self, obj);
        if (obj is Flare)
            return Player.ObjectGrabability.CantGrab;
        if (sproutcat == self.slugcatStats.name && obj is Spear)
            return Player.ObjectGrabability.OneHand;
        return val;
    }

    public static void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        orig(self);
        if (lghtbrpup == self.slugcatStats.name)
        {
            self.jumpBoost *= 1f + 0.175f;
        }
        else if (strawberry == self.slugcatStats.name)
        {
            if (Player.AnimationIndex.Flip == self.animation)
                self.jumpBoost *= 1f + 0.85f;
            else
                self.jumpBoost *= 1f + 0.2f;
        }
    }

    public static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
    {
        orig(self, edible);

        if (PlayerDataCWT.TryGetValue(self, out PlayerData playerData))
        {
            //if (playerData.IsSproutcat && Utility.EdibleIsBug(edible) && !playerData.Sproutcat.ateABugThisCycle)
            //{
            //    playerData.Sproutcat.ateABugThisCycle = true;
            //}

            #region lightpup food rxn
            if (playerData.IsLightpup && !self.exhausted) //please dont jump if youre chonk as all hell
            {
                if (edible is Creature critter)
                {
                    CreatureTemplate.Type crit = critter.abstractCreature.creatureTemplate.type;
                    if (CreatureTemplate.Type.SmallCentipede == crit)
                        LikesFood();
                    else if (CreatureTemplate.Type.SmallNeedleWorm == crit)
                        DislikesFood();
                    return;
                }

                AbstractObjectType foodObj = (edible as PhysicalObject).abstractPhysicalObject.type;

                if (MSC_AbstractObjectType.LillyPuck == foodObj
                    || MSC_AbstractObjectType.DandelionPeach == foodObj
                    || MSC_AbstractObjectType.Seed == foodObj
                    || edible as PhysicalObject is OracleSwarmer)
                {
                    LikesFood();
                    return;
                }

                if (AbstractObjectType.DangleFruit == foodObj
                    || AbstractObjectType.SlimeMold == foodObj
                    || MSC_AbstractObjectType.GooieDuck == foodObj
                    || AbstractObjectType.JellyFish == foodObj)
                {
                    DislikesFood();
                    return;
                }

                void LikesFood()
                {
                    if (!self.room.game.IsStorySession) //can only have negative food reactions outside of story
                        return;
                    if (LikesFoodJumpValue.TryGet(self, out int jumpValue))
                    {
                        playerData.Lightpup.controller.likesFood = jumpValue; //the number determines how large the jump is
                    }
                }

                void DislikesFood()
                {
                    (self.graphicsModule as PlayerGraphics).blink = 0;
                    self.Blink(40);
                    playerData.Lightpup.controller.likesFood = 0;
                }
            }
            #endregion
        }
    }

    public static void Player_Die(ILContext il)
    {
        //another place where i need to not call orig
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        //this is for leaving orig early for lightpup's electric (centi/zapcoil) resistance
        //and for sproutcat's explosion/acid resistance

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player self) =>
        {
            if (!PlayerDataCWT.TryGetValue(self, out PlayerData pData))
                return false;

            if (pData.IsLightpup)
                return pData.Lightpup.hitByZapcoil;

            if (pData.IsSproutcat)
                return pData.Sproutcat.pyroJump < MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity.Value;

            return false;
        });
        c.Emit(OpCodes.Brfalse, label);

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player self) =>
        {
            PlayerDataCWT.TryGetValue(self, out PlayerData pData);

            if (pData.IsLightpup)
            {
                pData.Lightpup.hitByZapcoil = false;
                pData.Lightpup.RemoveFlareCharge();
                self.room.PlaySound(SoundID.Fire_Spear_Pop, self.firstChunk.pos);
                (self.graphicsModule as PlayerGraphics).blink = 0;
                self.Blink(90);
                //dont stun, player still needs movement to not crash into a 2nd zapcoil in 0g
            }
            //no values required to set or anything for sproutcat
        });
        c.Emit(OpCodes.Ret);

        c.MarkLabel(label);
    }
    #endregion

}
