using JourneysStart.Lightbringer.PlayerStuff;
using JourneysStart.Outgrowth.PlayerStuff;
using Colour = UnityEngine.Color;
using System;
using Debug = UnityEngine.Debug;

namespace JourneysStart.Shared.PlayerStuff;

sealed class PlayerData
{
    public WeakReference<Player> playerRef;
    public readonly bool IsLightpup;
    public readonly bool IsSproutcat;
    public readonly bool IsModcat;

    public LightpupData Lightpup;
    public OutgrowthData Sproutcat;

    public bool SpritesInited;
    public SlugTailTexture tailPattern;

    public ModCompatibility.DressMySlugcatPatch dmsModCompat;

    public PlayerData(Player player)
    {
        Debug.Log($"{Plugin.MOD_NAME}: Adding {player.SlugCatClass} player {player.playerState.playerNumber} to PlayerDataCWT");
        playerRef = new WeakReference<Player>(player);

        SlugcatStats.Name name = player.slugcatStats.name;
        if (Plugin.lghtbrpup == name)
        {
            IsLightpup = true;
            Lightpup = new(this);
        }
        else if (Plugin.sproutcat == name)
        {
            IsSproutcat = true;
            Sproutcat = new(this);
        }
        IsModcat = IsLightpup || IsSproutcat;

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

    public void ModCompat_DressMySlugcat_DrawSprites(RoomCamera rCam, RoomCamera.SpriteLeaser sLeaser)
    {
        if (Plugin.ModEnabled_DressMySlugcat && playerRef.TryGetTarget(out Player player))
        {
            dmsModCompat?.DressMySlugcat_DrawSprites(player.graphicsModule as PlayerGraphics, sLeaser, rCam);
        }
        if (!tailPattern.usingDMSTailSprite)
            sLeaser.sprites[2].color = Colour.white; //white tail
    }
}
