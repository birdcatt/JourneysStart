using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Mathf = UnityEngine.Mathf;
using MoreSlugcats;
using JourneysStart.Shared.PlayerStuff;
using JourneysStart.Slugcats.Outgrowth.Rope;
using JourneysStart.Slugcats.Outgrowth.PlayerStuff.PlayerGraf;

namespace JourneysStart.Slugcats.Outgrowth.PlayerStuff;

sealed class OutgrowthData
{
    public PlayerData playerData;
    public CheekFluff cheekFluff;

    public int[] spriteIndexes;
    //0 - body scar
    //1 - rope
    //2 - face scar
    //3 - mushroom necklace
    //4 - rope 2
    //check fluff isnt in here, it stores its own indices
    public const int BODY_SCAR_INDEX = 0;
    public const int ROPE_INDEX = 1;
    public const int FACE_SCAR_INDEX = 2;

    public bool usingDMSFaceSprite;

    public int seedSpitUpMax;
    public bool ateABugThisCycle;

    public int inWater;
    public const int IN_WATER_MAX = 4 * 40;

    public bool foundNearestCreature;
    public int vineInAir; //timer
    public const int VINE_IN_AIR_MAX = 4 * 40;

    public int pyroJump; //used for acid/explosion resist, max is MoreSlugcats.cfgArtificerExplosionCapacity
    public const int PYRO_JUMP_CD_MAX = 60;
    public int pyroJumpCD = PYRO_JUMP_CD_MAX; //used to count down to when pyroJump should be reset

    public OutgrowthData(PlayerData playerData)
    {
        this.playerData = playerData;

        ateABugThisCycle = false;
        seedSpitUpMax = Random.Range(2, 5);

        playerData.playerRef.TryGetTarget(out Player player);
        if (player.room.game.session is StoryGameSession story)
        {
            seedSpitUpMax = Mathf.Min(6, seedSpitUpMax + story.saveState.food);
        }
    }

    public void Update()
    {
        if (!playerData.playerRef.TryGetTarget(out Player player))
            return;

        if (player.dead || player.stun > 0)
            return;

        if (player.Submersion > 0 && !player.input[0].AnyInput)
        {
            if (inWater >= IN_WATER_MAX)
            {
                player.AddQuarterFood();
                inWater = 2 * 40;
            }
            else
                inWater++;
        }
        else
            inWater = 0;

        if (player.FoodInStomach >= player.MaxFoodInStomach) //go drown badly if youre full
            player.slugcatStats.lungsFac = 1.3f;
        //now add else if (get value from json) go back to normal
    }

    public void ClassMechanicsSproutcat()
    {
        //used in ClassMechanicsSaint hook
        if (!playerData.playerRef.TryGetTarget(out Player player))
            return;

        if (pyroJump > 0 && pyroJumpCD-- <= 0)
        {
            pyroJump--;
            if (pyroJumpCD >= Mathf.Max(1, MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity.Value - 5))
                pyroJumpCD = 40;
            else
                pyroJumpCD = 60;
        }

        if (CanShootTongue())
        {
            Vector2 normalized = new Vector2(player.flipDirection, 0.7f).normalized;
            if (player.input[0].y > 0)
            {
                normalized = new Vector2(0f, 1f);
            }
            normalized = (normalized + player.mainBodyChunk.vel.normalized * 0.2f).normalized;
            player.tongue.Shoot(normalized);
        }

        #region vine combat
        bool doesntHaveAllGraspsNull = false;
        for (int i = 0; i < player.grasps.Length; i++)
        {
            if (player.grasps[i] != null && player.IsObjectThrowable(player.grasps[i].grabbed))
            {
                doesntHaveAllGraspsNull = true;
                break;
            }
        }

        if (!doesntHaveAllGraspsNull && player.input[0].thrw && !player.input[1].thrw)
        {
            //Debug.Log($"{Plugin.MOD_NAME}: (VineCombat) Throw pressed");
            Vector2 targetDir = VineCombat.FindNearestCreaturePos(player);
            if (!foundNearestCreature)
            {
                targetDir = new Vector2(player.flipDirection, 0.7f).normalized;
                if (player.input[0].y > 0)
                {
                    targetDir = new Vector2(0f, 1f);
                }
                targetDir = (targetDir + player.mainBodyChunk.vel.normalized * 0.2f).normalized;
            }
            player.tongue.CombatShoot(targetDir);

            //if (foundNearestCreature)
            //{
            //    SharedPhysics.CollisionResult collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(null, player.room, player.tongue.pos, ref targetDir, 1f, 1, player.tongue.baseChunk.owner, false);
            //    player.tongue.CombatHitCreature(collisionResult);
            //}

            //foundNearestCreature = false;

            //self.mode = Player.Tongue.Mode.Retracting;
        }
        #endregion
    }

    public bool CannotEatBugsThisCycle(IPlayerEdible eatenobject)
    {
        return ateABugThisCycle && Utility.EdibleIsBug(eatenobject);
    }
    public bool CanShootTongue()
    {
        if (!playerData.playerRef.TryGetTarget(out Player player))
            return false;

        if (player.tongue.mode != Player.Tongue.Mode.Retracted)
            return false;

        if (!(player.input[0].jmp && !player.input[1].jmp && !player.input[0].pckp && player.canJump <= 0))
            return false;

        if (player.bodyMode == Player.BodyModeIndex.Crawl
            || player.bodyMode == Player.BodyModeIndex.CorridorClimb
            || player.bodyMode == Player.BodyModeIndex.ClimbIntoShortCut
            || player.bodyMode == Player.BodyModeIndex.WallClimb
            || player.bodyMode == Player.BodyModeIndex.Swimming)
            return false;

        if (player.animation == Player.AnimationIndex.ClimbOnBeam
            || player.animation == Player.AnimationIndex.AntlerClimb
            || player.animation == Player.AnimationIndex.HangFromBeam
            || player.animation == Player.AnimationIndex.VineGrab
            || player.animation == Player.AnimationIndex.ZeroGPoleGrab)
            return false;

        //if (cfgOldTongue), code elsewhere should take care of it
        return !MMF.cfgOldTongue.Value && player.Consious && !player.corridorDrop;
    }
}