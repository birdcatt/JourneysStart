using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Mathf = UnityEngine.Mathf;
using MoreSlugcats;
using JourneysStart.Shared.PlayerStuff;

namespace JourneysStart.Outgrowth.PlayerStuff;

sealed class OutgrowthData
{
    public PlayerData playerData;

    public int SeedSpitUpMax;
    public bool AteABugThisCycle;

    public int[] spriteIndexes;
    //0 - body scar
    //1 - mushroom necklace
    //2 - face scar?
    //check fluff isnt in here
    public const int BodyScarIndex = 0;

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
        //if (!MMF.cfgOldTongue.Value
        //    && player.input[0].jmp && !player.input[1].jmp && !player.input[0].pckp && player.canJump <= 0
        //    && player.bodyMode != Player.BodyModeIndex.Crawl
        //    && player.animation != Player.AnimationIndex.ClimbOnBeam && player.animation != Player.AnimationIndex.AntlerClimb
        //    && player.animation != Player.AnimationIndex.HangFromBeam && player.SaintTongueCheck())
        //{
        //    Vector2 vector = new(player.flipDirection, 0.7f);
        //    Vector2 normalized = vector.normalized;
        //    if (player.input[0].y > 0)
        //    {
        //        normalized = new Vector2(0f, 1f);
        //    }
        //    normalized = (normalized + player.mainBodyChunk.vel.normalized * 0.2f).normalized;
        //    player.tongue.Shoot(normalized);
        //}
    }

    public bool CannotEatBugsThisCycle(IPlayerEdible eatenobject)
    {
        return playerData.IsSproutcat && AteABugThisCycle && Utility.EdibleIsBug(eatenobject);
    }
}