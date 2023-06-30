using System.Collections.Generic;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;
using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
using MSC_AbstractObjectType = MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType;
using MoreSlugcats;
using static JourneysStart.Utility;
using JourneysStart.Shared.PlayerStuff;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using JourneysStart.FisobsItems.Taser;

//this is quite painful by itself so lightbringer gets its own separate crafting cs file
namespace JourneysStart.Lightbringer.PlayerStuff
{
    public class Crafting
    {
        public static void Hook()
        {
            On.Player.GrabUpdate += Player_GrabUpdate;
            On.Player.GraspsCanBeCrafted += Player_GraspsCanBeCrafted;
            IL.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
        }

        public static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            orig(self, eu);

            if (Plugin.lghtbrpup == self.slugcatStats.name && CraftWillFail())
            {
                int counter = self.swallowAndRegurgitateCounter;

                if (70 <= counter && counter <= 80 && 0 == counter % 10)
                {
                    //SoundID.Zapper_Zap sounds cooler but this one fits more
                    self.room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, self.firstChunk.pos, 1f, 1.5f + Random.value * 1.5f);
                    AddCraftingLight(self, 80f, 6); //for the homies with sound off
                }
            }

            bool CraftWillFail()
            {
                if (self.FoodInStomach <= 0) //needs at least 1 pip to fail and spark
                    return false;

                if (!self.GraspsCanBeCrafted()) //should return false is grasp is null
                    return false;

                Creature.Grasp graspA = self.grasps[0];
                Creature.Grasp graspB = self.grasps[1];

                if (GraspIsTaserWithCharge(graspA) && GraspIsTaserWithCharge(graspB))
                    return true;

                bool graspAIsFullElectric = GraspIsFullElectric(graspA);
                bool graspBIsFullElectric = GraspIsFullElectric(graspB);

                if (graspAIsFullElectric && graspBIsFullElectric)
                    return true;

                AbstractObjectType abstrTypeA = graspA.grabbed.abstractPhysicalObject.type;
                AbstractObjectType abstrTypeB = graspB.grabbed.abstractPhysicalObject.type;

                if (TaserFisob.AbstrTaser == abstrTypeA && AbstractObjectType.Spear == abstrTypeB || TaserFisob.AbstrTaser == abstrTypeB && AbstractObjectType.Spear == abstrTypeA)
                    return false;

                AbstractObjectType item = CraftingLibrary.GetObjectType(graspA, graspB);

                return (graspAIsFullElectric || graspBIsFullElectric) && (AbstractObjectType.Spear == item || TaserFisob.AbstrTaser == item);
            }
        }

        public static bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
        {
            bool val = orig(self);
            if (Plugin.lghtbrpup != self.slugcatStats.name)
            {
                return val;
            }

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

        public static void Player_SpitUpCraftedObject(ILContext il)
        {
            ILCursor c = new(il);
            ILLabel label = il.DefineLabel();

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Player self) =>
            {
                return Plugin.lghtbrpup == self.slugcatStats.name;
            });
            c.Emit(OpCodes.Brfalse, label);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Player self) =>
            {
                AbstractPhysicalObject item = CraftingLibrary.GetCraftingResult(self);

                if (null == item)
                    return;

                self.craftingTutorial = true;

                AbstractObjectType graspA = self.grasps[0].grabbed.abstractPhysicalObject.type;
                AbstractObjectType graspB = self.grasps[1].grabbed.abstractPhysicalObject.type;

                if (TaserFisob.AbstrTaser == graspA && TaserFisob.AbstrTaser == graspB)
                {
                    ShortCircuitElectricGrasps(self);
                    return;
                }

                bool spawnSecondItem = false;

                if (AbstractObjectType.Spear == item.type || TaserFisob.AbstrTaser == item.type)
                {
                    if (AbstractObjectType.Rock == graspA && AbstractObjectType.Rock == graspB)
                    {
                        (item as AbstractSpear).electric = false;
                        self.room.PlaySound(SoundID.Spear_Fragment_Bounce, self.mainBodyChunk);
                        AddCraftingSpark(self);
                    }
                    else if (ElectricCrafting(self, ref item, ref spawnSecondItem))
                        return;
                }
                else
                    self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);

                //delete grasps in hand
                for (int i = 0; i < 2; i++)
                {
                    AbstractPhysicalObject grasp = self.grasps[i].grabbed.abstractPhysicalObject;

                    if (self.room.game.session is StoryGameSession story)
                        story.RemovePersistentTracker(grasp);

                    self.ReleaseGrasp(i);

                    grasp.LoseAllStuckObjects();
                    grasp.realizedObject.RemoveFromRoom();
                    self.room.abstractRoom.RemoveEntity(grasp);
                }

                if (item is TaserAbstract t)
                    Debug.Log($"{Plugin.MOD_NAME}: (Crafting) Spawning taser with a charge of {t.electricCharge}");
                else if (item is AbstractSpear s && s.electric)
                    Debug.Log($"{Plugin.MOD_NAME}: (Crafting) Spawning electric spear with a charge of {s.electricCharge}");
                else
                    Debug.Log($"{Plugin.MOD_NAME}: (Crafting) Spawning crafted item {item.type}");

                SpawnItemInHand(self, item);

                if (spawnSecondItem && Plugin.PlayerDataCWT.TryGetValue(self, out PlayerData playerData))
                {
                    item = playerData.Lightpup.crafting_SecondItem.Get();

                    if (null == item)
                    {
                        Debug.Log($"{Plugin.MOD_NAME}: (Crafting) Attempting to spawn second item--but item is null?!");
                        return;
                    }

                    int charge = -1; //just for the debug log
                    if (item is AbstractSpear spear)
                        charge = spear.electricCharge;
                    else if (item is TaserAbstract taser)
                        charge = taser.electricCharge;
                    Debug.Log($"{Plugin.MOD_NAME}: (Crafting) Spawning second item ({item.type}) with a charge of {charge}");

                    self.room.PlaySound(SoundID.Zapper_Zap, self.firstChunk.pos, 1f, 1.5f + Random.value * 1.5f);
                    AddCraftingLight(self);
                    AddCraftingSpark(self);

                    SpawnItemInHand(self, item);
                }
                return;
            });
            c.Emit(OpCodes.Ret);

            c.MarkLabel(label);
        }
        public static bool ElectricCrafting(Player self, ref AbstractPhysicalObject item, ref bool spawnSecondItem)
        {
            //return bool is to determine whether to return or continue in SpitUp

            bool graspIsSpear = false;
            bool graspIsTaser = false;
            bool graspIsElecSpear = false;

            bool graspIsRock = false; //strip electric spear of taser
            int spearCharge = 0;
            int taserCharge = 0;

            bool graspIsFullElecSpear = false;
            bool graspIsFullTaser = false;

            EntityID spearID = self.room.game.GetNewID(); //EntityID is a struct and non-nullable
            EntityID taserID = self.room.game.GetNewID();

            for (int i = 0; i < 2; i++)
            {
                AbstractPhysicalObject abstr = self.grasps[i].grabbed.abstractPhysicalObject;

                if (abstr is TaserAbstract taser)
                {
                    graspIsTaser = true;
                    taserCharge = taser.electricCharge;
                    graspIsFullTaser = taser.electricCharge >= 3;
                    if (AbstractObjectType.Spear == item.type)
                        taserID = taser.ID;
                }
                else if (abstr is AbstractSpear spear)
                {
                    graspIsSpear = true;
                    graspIsElecSpear = spear.electric;
                    if (spear.electric)
                    {
                        spearCharge = spear.electricCharge;
                        graspIsFullElecSpear = spear.electricCharge >= 3;
                    }
                    if (TaserFisob.AbstrTaser == item.type)
                        spearID = spear.ID;
                }
                else if (AbstractObjectType.Rock == abstr.type)
                    graspIsRock = true;
            }

            if (graspIsFullElecSpear && graspIsFullTaser)
            {
                ShortCircuitElectricGrasps(self);
                return true;
            }

            if (graspIsElecSpear && (graspIsTaser || graspIsRock))
            {
                //normal spear + taser = elec spear with taser charge, proceed as normal
                //full elec spear + full taser = fail, handled outside
                //elec spear + taser = elec spear with taser charge, taser with spear charge
                //elec spear + rock = spear + taser with spear charge

                //takes 1 pip to craft elec spear or taser with full charge
                //"swapping" or "inserting" the taser onto the spear takes 0 food

                if (graspIsRock)
                {
                    (item as TaserAbstract).electricCharge = spearCharge;
                }
                else if (AbstractObjectType.Spear == item.type) //for swapping
                    (item as AbstractSpear).electricCharge = taserCharge;

                if (Plugin.PlayerDataCWT.TryGetValue(self, out PlayerData playerData))
                {
                    spawnSecondItem = true;
                    if (AbstractObjectType.Spear == item.type)
                        playerData.Lightpup.crafting_SecondItem.Set(AbstractObjectType.Spear, taserID, spearCharge, self.room.world, self.abstractPhysicalObject.pos);
                    else
                        playerData.Lightpup.crafting_SecondItem.Set(TaserFisob.AbstrTaser, spearID, taserCharge, self.room.world, self.abstractPhysicalObject.pos);
                }
                else
                    Debug.Log($"{Plugin.MOD_NAME}: (Crafting) Unable to get value from PlayerDataCWT?!");
            }
            else //grasps are not electric spear and taser
            {
                if (0 == self.FoodInStomach)
                {
                    Debug.Log($"{Plugin.MOD_NAME}: (Crafting) Not enough food in stomach");
                    self.room.PlaySound(SoundID.HUD_Food_Meter_Fill_Plop_A, self.firstChunk.pos);
                    if (self.room.game.IsStorySession)
                        self.showKarmaFoodRainTime = 10;
                    return true;
                }

                AddCraftingLight(self, 80f);
                AddCraftingSpark(self);

                if (graspIsFullElecSpear || /*!graspIsFullElecSpear &&*/ !graspIsSpear && graspIsFullTaser)
                {
                    //no need to check for 2 tasers, they explode/short circuit if crafted
                    Debug.Log($"{Plugin.MOD_NAME}: (Crafting) Electric grasp is already full charge");
                    self.room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, self.firstChunk.pos, 1.5f, 1.5f + Random.value * 1.5f);
                    self.Stun(20);
                    return true;
                }

                self.room.PlaySound(SoundID.Zapper_Zap, self.firstChunk.pos, 1f, 1.5f + Random.value * 1.5f);
                self.SubtractFood(1);

                if (item is AbstractSpear craftedSpear)
                {
                    if (graspIsTaser) //swapping or inserting taser takes the taser's charge
                        craftedSpear.electricCharge = taserCharge;
                    else if (graspIsElecSpear)
                    {
                        //elec spear + flarebomb or something
                        if (++spearCharge < 4)
                            craftedSpear.electricCharge = spearCharge;
                    }
                    else if (Plugin.lghtbrpup == self.room.game.StoryCharacter) //normal spear + flarebomb, etc
                        craftedSpear.electricCharge = 1; //charges fully in other campaigns
                }
                else if (item is TaserAbstract craftedTaser)
                {
                    if (graspIsElecSpear) //elec spear + taser = swapping, so taser take's elec spear's charge
                        craftedTaser.electricCharge = spearCharge;
                    else if (graspIsTaser)
                    {
                        //recharging taser with flarebomb or smth
                        if (++taserCharge < 4)
                            craftedTaser.electricCharge = taserCharge;
                    }
                    else if (Plugin.lghtbrpup == self.room.game.StoryCharacter) //rock + flarebomb, etc
                        craftedTaser.electricCharge = 1;
                }
            }
            return false;
        }
        public static void ShortCircuitElectricGrasps(Player self)
        {
            Debug.Log($"{Plugin.MOD_NAME}: (Crafting) Short circuiting both electric items in grasps");
            for (int i = 0; i < 2; i++)
            {
                Creature.Grasp grasp = self.grasps[i];
                if (GraspIsTaserWithCharge(grasp))
                    (grasp.grabbed as Taser).ShortCircuit();
                else if (GraspIsSpearWithCharge(grasp))
                    (grasp.grabbed as ElectricSpear).ShortCircuit();
            }
            AddCraftingLight(self);
            //get scared
            (self.graphicsModule as PlayerGraphics).blink = 0;
            self.Blink(130);
        }
    } //end of class Crafting

    public static class CraftingLibrary
    {
        public static Dictionary<(AbstractObjectType, AbstractObjectType), AbstractObjectType> craftingTable;
        static CraftingLibrary()
        {
            craftingTable = new();

            AddRecipe(AbstractObjectType.Rock, AbstractObjectType.DangleFruit, AbstractObjectType.FlareBomb);
            AddRecipe(AbstractObjectType.Rock, AbstractObjectType.SlimeMold, AbstractObjectType.Lantern);
            AddRecipe(AbstractObjectType.Rock, AbstractObjectType.Lantern, AbstractObjectType.FlareBomb);

            AddRecipe(AbstractObjectType.SlimeMold, AbstractObjectType.FlareBomb, AbstractObjectType.Lantern);

            AddRecipe(AbstractObjectType.FlyLure, AbstractObjectType.WaterNut, AbstractObjectType.BubbleGrass);

            AddRecipe(AbstractObjectType.Rock, AbstractObjectType.FlareBomb, TaserFisob.AbstrTaser);
            AddRecipe(AbstractObjectType.Rock, AbstractObjectType.JellyFish, TaserFisob.AbstrTaser);

            AddRecipe(TaserFisob.AbstrTaser, AbstractObjectType.FlareBomb, TaserFisob.AbstrTaser);
            AddRecipe(TaserFisob.AbstrTaser, AbstractObjectType.JellyFish, TaserFisob.AbstrTaser);
            AddRecipe(AbstractObjectType.Spear, AbstractObjectType.Rock, TaserFisob.AbstrTaser); //strip electric spear of taser

            AddRecipe(TaserFisob.AbstrTaser, TaserFisob.AbstrTaser, AbstractObjectType.Rock); //short circuits, so result doesnt matter

            AddRecipe(MSC_AbstractObjectType.GooieDuck, AbstractObjectType.Rock, AbstractObjectType.PuffBall);
            AddRecipe(MSC_AbstractObjectType.GooieDuck, AbstractObjectType.Mushroom, AbstractObjectType.PuffBall);

            AddRecipe(AbstractObjectType.Mushroom, AbstractObjectType.Rock, AbstractObjectType.PuffBall);
            AddRecipe(AbstractObjectType.Mushroom, AbstractObjectType.SlimeMold, AbstractObjectType.PuffBall);

            AddRecipe(AbstractObjectType.Rock, AbstractObjectType.Rock, AbstractObjectType.Spear); //makes regular spear
            AddRecipe(AbstractObjectType.Spear, AbstractObjectType.FlareBomb, AbstractObjectType.Spear);
            AddRecipe(AbstractObjectType.Spear, AbstractObjectType.JellyFish, AbstractObjectType.Spear);
            AddRecipe(AbstractObjectType.Spear, TaserFisob.AbstrTaser, AbstractObjectType.Spear); //do not change this, ElectricCrafting is hardcoded to check this

            //joke recipes

            AddRecipe(AbstractObjectType.KarmaFlower, AbstractObjectType.EggBugEgg, MSC_AbstractObjectType.FireEgg);
            AddRecipe(AbstractObjectType.KarmaFlower, AbstractObjectType.DangleFruit, MSC_AbstractObjectType.SingularityBomb);
            AddRecipe(AbstractObjectType.KarmaFlower, AbstractObjectType.Rock, MSC_AbstractObjectType.JokeRifle);
        }
        public static void AddRecipe(AbstractObjectType itemA, AbstractObjectType itemB, AbstractObjectType result)
        {
            craftingTable[(itemA, itemB)] = result;
            craftingTable[(itemB, itemA)] = result;
        }
        public static AbstractObjectType GraspIsCraftable(Creature.Grasp grasp)
        {
            PhysicalObject physObj = grasp.grabbed;
            if (physObj is Creature crit)
            {
                if (CreatureTemplate.Type.SmallCentipede == crit.abstractCreature.creatureTemplate.type) //it doesnt have to be dead
                    return AbstractObjectType.JellyFish;
                return null;
            }
            else if (physObj is WaterNut w)
            {
                if (!w.AbstrNut.swollen)
                    return AbstractObjectType.Rock;
                return AbstractObjectType.DangleFruit;
            }
            AbstractPhysicalObject abstrPhysObj = physObj.abstractPhysicalObject;
            if (abstrPhysObj is AbstractSpear spear && (spear.explosive || spear.hue > 0))
                return null;
            if (!ConfigMenu.enableJokeRecipes.Value && AbstractObjectType.KarmaFlower == abstrPhysObj.type)
                return null;
            return abstrPhysObj.type;
        }
        public static AbstractObjectType GetObjectType(Creature.Grasp graspA, Creature.Grasp graspB)
        {
            if (!ModManager.MSC || null == graspA || null == graspB)
                return null;

            AbstractObjectType objA;
            AbstractObjectType objB;

            if (null == (objA = GraspIsCraftable(graspA)) || null == (objB = GraspIsCraftable(graspB)))
                return null;

            return craftingTable.ContainsKey((objA, objB)) ? craftingTable[(objA, objB)] : null;
        }
        public static AbstractPhysicalObject GetCraftingResult(Player self)
        {
            Creature.Grasp graspA = self.grasps[0];
            Creature.Grasp graspB = self.grasps[1];

            AbstractObjectType item = GetObjectType(graspA, graspB);

            AbstractPhysicalObject objA = graspA.grabbed.abstractPhysicalObject;
            AbstractPhysicalObject objB = graspB.grabbed.abstractPhysicalObject;

            Debug.Log($"{Plugin.MOD_NAME}: {objA.type} + {objB.type} = {item}");

            if (null == item)
                return null;

            World world = self.room.world;
            EntityID id = self.room.game.GetNewID();
            WorldCoordinate pos = self.abstractCreature.pos; //theres also self.room.GetWorldCoordinate(self.firstChunk.pos) & self.abstractPhysicalObject.pos

            if (AbstractObjectType.Spear == item)
            {
                if (AbstractObjectType.Spear == objA.type)
                    id = objA.ID;
                else if (AbstractObjectType.Spear == objB.type)
                    id = objB.ID;
                return new AbstractSpear(world, null, pos, id, false, true);
            }

            if (TaserFisob.AbstrTaser == item)
            {
                //switch cases will not work for spawning tasers, apparently
                if (TaserFisob.AbstrTaser == objA.type)
                    id = objA.ID;
                else if (TaserFisob.AbstrTaser == objB.type)
                    id = objB.ID;
                return new TaserAbstract(world, pos, id);
            }

            if (AbstractObjectType.FlareBomb == item)
            {
                if (AbstractObjectType.Lantern == objA.type)
                    id = objA.ID;
                else if (AbstractObjectType.Lantern == objB.type)
                    id = objB.ID;
                return new AbstractConsumable(world, item, null, pos, id, -1, -1, null);
            }

            if (AbstractObjectType.Lantern == item)
            {
                if (AbstractObjectType.SlimeMold == objA.type || AbstractObjectType.FlareBomb == objA.type)
                    id = objA.ID;
                else if (AbstractObjectType.SlimeMold == objB.type || AbstractObjectType.FlareBomb == objB.type)
                    id = objB.ID;
                return new AbstractPhysicalObject(world, item, null, pos, id);
            }

            if (AbstractObjectType.PuffBall == item)
                return new AbstractConsumable(world, item, null, pos, id, -1, -1, null);

            if (MSC_AbstractObjectType.JokeRifle == item)
                return new JokeRifle.AbstractRifle(world, null, pos, id, JokeRifle.AbstractRifle.AmmoType.Rock);

            return new AbstractPhysicalObject(world, item, null, pos, id);
        }
    }
}