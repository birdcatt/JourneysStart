using System;
using Colour = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Mathf = UnityEngine.Mathf;
using RWCustom;

namespace JourneysStart.Slugcats.Outgrowth.PlayerStuff.PlayerGraf;

public class SproutScales
{
    //its just LizardCosmetics SpineSpikes
    public WeakReference<Player> playerRef;

    public const string spriteName = "LizardScaleA0";

    public readonly int startIndex;
    public readonly int endIndex;
    public readonly int numberOfSprites;

    public SproutScales(PlayerGraphics pGraf, int startIndex)
    {
        playerRef = new WeakReference<Player>(pGraf.player);
        this.startIndex = startIndex;

        numberOfSprites = 2;
        endIndex = startIndex + numberOfSprites;
    }

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            sLeaser.sprites[i] = new(spriteName)
            {
                anchorY = 0.15f,
                scaleY = 0.9f
            };
        }
    }

    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, FContainer container)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            sLeaser.sprites[i].RemoveFromContainer();
            container.AddChild(sLeaser.sprites[i]);
            sLeaser.sprites[i].MoveBehindOtherNode(sLeaser.sprites[2]);
        }
    }

    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, float timeStacker, Vector2 camPos)
    {
        if (!playerRef.TryGetTarget(out Player player))
            return;

        for (int i = startIndex; i < endIndex; i++)
        {
            var spineData = (player.graphicsModule as PlayerGraphics).SpinePosition(Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(startIndex, endIndex, i)), timeStacker);
            sLeaser.sprites[i].x = spineData.outerPos.x - camPos.x;
            sLeaser.sprites[i].y = spineData.outerPos.y - camPos.y;
            sLeaser.sprites[i].rotation = Custom.VecToDeg(spineData.perp);
        }
    }

    public void ApplyPalette(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser)
    {
        Plugin.PlayerDataCWT.TryGetValue(self.player, out var playerData);

        var colour = self.GetMalnourishedColour(Plugin.TailScales) ?? self.GetMalnourishedColour(playerData.tailPattern.OldPatternColour);

        for (int i = startIndex; i < endIndex; i++)
        {
            sLeaser.sprites[i].color = colour;
        }
    }
}