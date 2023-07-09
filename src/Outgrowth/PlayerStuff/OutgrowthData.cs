using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Mathf = UnityEngine.Mathf;
using MoreSlugcats;
using JourneysStart.Shared.PlayerStuff;
using JourneysStart.Outgrowth.PlayerStuff.PlayerGraf;

namespace JourneysStart.Outgrowth.PlayerStuff;

sealed class OutgrowthData
{
    public PlayerData playerData;

    public int SeedSpitUpMax;
    public bool AteABugThisCycle;

    public int inWater;
    public const int inWaterMax = 4 * 40;

    public int[] spriteIndexes;
    //0 - body scar
    //1 - rope
    //2 - rope 2
    //3 - mushroom necklace
    //4 - face scar
    //check fluff isnt in here, it stores its own indices
    public const int BODY_SCAR_INDEX = 0;
    public const int ROPE_INDEX = 1;

    public CheekFluff cheekFluff;

    public OutgrowthData(PlayerData playerData)
    {
        this.playerData = playerData;

        AteABugThisCycle = false;
        SeedSpitUpMax = Random.Range(2, 5);

        playerData.playerRef.TryGetTarget(out Player player);
        if (player.room.game.session is StoryGameSession story)
        {
            SeedSpitUpMax = Mathf.Min(6, SeedSpitUpMax + story.saveState.food);
        }
    }

    public void Update()
    {
        if (!playerData.playerRef.TryGetTarget(out Player player))
            return;

        if (player.dead)
            return;

        if (player.Submersion > 0 && !player.input[0].AnyInput)
        {
            if (inWater >= inWaterMax)
            {
                player.AddQuarterFood();
                inWater = 1 * 40;
            }
            else
                inWater++;
        }
        else if (inWater != 0)
            inWater = 0;
    }

    public void TongueUpdate()
    {
        if (!playerData.playerRef.TryGetTarget(out Player player))
            return;

        //from ClassMechanicsSaint
        if (CanShootTongue())
        {
            Vector2 vector = new(player.flipDirection, 0.7f);
            Vector2 normalized = vector.normalized;
            if (player.input[0].y > 0)
            {
                normalized = new Vector2(0f, 1f);
            }
            normalized = (normalized + player.mainBodyChunk.vel.normalized * 0.2f).normalized;
            player.tongue.Shoot(normalized);
        }
    }

    public bool CannotEatBugsThisCycle(IPlayerEdible eatenobject)
    {
        return AteABugThisCycle && Utility.EdibleIsBug(eatenobject);
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