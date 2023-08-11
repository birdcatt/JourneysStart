using static JourneysStart.Plugin;
using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
using MSC_AbstractObjectType = MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType;
using System.Linq;
using JourneysStart.Shared.PlayerStuff;
using JourneysStart.Shared.ArtOnScreen;
using JourneysStart.Shared.PlayerStuff.PlayerGraf;
using JourneysStart.Shared.OracleStuff;
using JourneysStart.Slugcats.Lightbringer.MiscData;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace JourneysStart.Shared;

public class SharedGeneral
{
    public static void Hook()
    {
        //CutscenesSlideshows.Hook();
        JollySelectMenu.Hook();

        OracleDialogue.Hook();
        PearlDialogue.Hook();

        PlayerGrafHooks.Hook();
        Crafting.Hook();

        RoomScriptHooks.Hook();

        GeneralHooks();
    }

    public static void GeneralHooks()
    {
        On.Player.ctor += Player_ctor;
        On.Player.Update += Player_Update;
        On.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;

        On.Player.Grabability += Player_Grabability;
        On.Player.Jump += Player_Jump;
        On.Player.ObjectEaten += Player_ObjectEaten; //food reaction + eating a bug
        IL.Player.Die += Player_Die;

        On.SlugcatStats.SpearSpawnModifier += SlugcatStats_SpearSpawnModifier;
        On.SlugcatStats.SpearSpawnElectricRandomChance += SpearSpawnElectricChance;
        On.SlugcatStats.SpearSpawnExplosiveRandomChance += SpearSpawnExplosiveChance;

        On.WorldLoader.OverseerSpawnConditions += WorldLoader_OverseerSpawnConditions;
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
                self.tongue = new(self, 0); //2nd arg is index
            }
        }
    }
    public static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.room != null && PlayerDataCWT.TryGetValue(self, out PlayerData playerData))
        {
            playerData.Update();
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
            if (playerData.IsSproutcat && Utility.EdibleIsBug(edible) && !playerData.Sproutcat.ateABugThisCycle)
            {
                playerData.Sproutcat.ateABugThisCycle = true;
            }

            #region lightpup food rxn
            else if (playerData.IsLightpup && !self.exhausted) //please dont jump if youre chonk as all hell
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
            return PlayerDataCWT.TryGetValue(self, out PlayerData pData)
            && (pData.IsLightpup && pData.Lightpup.hitByZapcoil
            || pData.IsSproutcat && pData.Sproutcat.pyroJump < MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity.Value);
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

    #region spear spawn chance
    private static float SlugcatStats_SpearSpawnModifier(On.SlugcatStats.orig_SpearSpawnModifier orig, SlugcatStats.Name index, float originalSpearChance)
    {
        if (lghtbrpup == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear;
        else if (sproutcat == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;
        else if (strawberry == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint;
        return orig(index, originalSpearChance);
    }
    public static float SpearSpawnElectricChance(On.SlugcatStats.orig_SpearSpawnElectricRandomChance orig, SlugcatStats.Name index)
    {
        if (lghtbrpup == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear;
        else if (sproutcat == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;
        else if (strawberry == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint;
        return orig(index);
    }
    public static float SpearSpawnExplosiveChance(On.SlugcatStats.orig_SpearSpawnExplosiveRandomChance orig, SlugcatStats.Name index)
    {
        if (lghtbrpup == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear;
        else if (sproutcat == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;
        else if (strawberry == index)
            index = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint;
        return orig(index);
    }
    #endregion

    public static bool WorldLoader_OverseerSpawnConditions(On.WorldLoader.orig_OverseerSpawnConditions orig, WorldLoader self, SlugcatStats.Name character)
    {
        bool val = orig(self, character);
        if (self.game.session is StoryGameSession story && !story.saveState.guideOverseerDead)
        {
            SaveState saveState = story.saveState;
            if (lghtbrpup == story.game.StoryCharacter)
            {
                return Lightpup_Debug_OverseerSpawn.TryGet(story.game, out bool spawnNow) && spawnNow
                    || Utility.ProgressionUnlocked(story) && !self.world.abstractRooms.Any(room => room.name == "AI");
            }
            if (sproutcat == story.game.StoryCharacter)
            {
                int cycleNumber = saveState.cycleNumber;
                return UnityEngine.Random.value < self.world.region.regionParams.playerGuideOverseerSpawnChance
                    && saveState.miscWorldSaveData.SSaiConversationsHad > 0
                    && 9 < cycleNumber && cycleNumber < 25
                    && !saveState.miscWorldSaveData.playerGuideState.angryWithPlayer;
            }
        }
        return val;
    }
}
