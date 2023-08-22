namespace JourneysStart.Slugcats.Strawberry;

public class StrawberryData
{
    public PlayerData playerData;

    public int rollExtender;
    public int rollFallExtender;

    public bool isClimbingWall; //use it to make player graphics animations for wall climbing

    public StrawberryData(PlayerData playerData)
    {
        this.playerData = playerData;
    }

    public void Update(bool eu)
    {
        if (!playerData.playerRef.TryGetTarget(out var player))
            return;

        //wall climb
        if (player.bodyChunks[0].contactPoint.x != 0)
        {
            foreach (var chunk in player.bodyChunks)
            {
                if (chunk.vel.y <= 0)
                    chunk.vel.y = 0;
            }

            if (player.input[0].y > 0 && Player.BodyModeIndex.WallClimb == player.bodyMode && eu)
            {
                isClimbingWall = true;
                foreach (var chunk in player.bodyChunks)
                {
                    if (chunk.vel.y < 6)
                        chunk.vel.y += 2;
                }
            }
            else
                isClimbingWall = false;
        }
    }
}
