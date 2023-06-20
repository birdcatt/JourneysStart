using JourneysStart.Lightbringer.PlayerStuff;
using JourneysStart.Outgrowth.PlayerStuff;
using Colour = UnityEngine.Color;
using System;

namespace JourneysStart.Shared.PlayerStuff;

sealed class PlayerData
{
    public WeakReference<Player> playerRef;
    public readonly bool IsLightpup;
    public readonly bool IsSproutcat;
    public readonly bool IsModcat;

    public LightpupData Lightpup;
    public OutgrowthData Sproutcat;

    public SlugTailTexture tailPattern;

    public ModCompatibility.DressMySlugcatPatch dmsModCompat;

    public PlayerData(Player player)
    {
        playerRef = new WeakReference<Player>(player);

        IsLightpup = Plugin.lghtbrpup == player.slugcatStats.name;
        IsSproutcat = Plugin.sproutcat == player.slugcatStats.name;
        IsModcat = IsLightpup || IsSproutcat;

        if (IsLightpup)
            Lightpup = new(this);
        else if (IsSproutcat)
            Sproutcat = new(this);

        tailPattern = new(player);

        if (Plugin.ModEnabled_DressMySlugcat)
            dmsModCompat = new();
    }

    public void Update()
    {
        if (!playerRef.TryGetTarget(out _))
            return;

        Lightpup?.Update();
        Sproutcat?.Update();
    }

    public void DressMySlugcat_ModCompat_DrawSprites(RoomCamera rCam, RoomCamera.SpriteLeaser sLeaser)
    {
        if (Plugin.ModEnabled_DressMySlugcat && playerRef.TryGetTarget(out Player player))
        {
            dmsModCompat?.DressMySlugcat_DrawSprites(player.graphicsModule as PlayerGraphics, sLeaser, rCam);
        }
        if (!tailPattern.usingDMSTailSprite)
            sLeaser.sprites[2].color = Colour.white; //white tail
    }
}
