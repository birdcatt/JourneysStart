using JourneysStart.Shared.PlayerStuff.PlayerGraf;
using Colour = UnityEngine.Color;
using System;
using Debug = UnityEngine.Debug;
using JourneysStart.Slugcats.Lightbringer.PlayerStuff;
using JourneysStart.Slugcats.Strawberry;
using JourneysStart.Slugcats.Outgrowth.PlayerStuff;

namespace JourneysStart.Shared.PlayerStuff;

sealed class PlayerData
{
    //yeah ok you dont need a WeakRef to PlayerData for either modcat data
    //theres nothing here that really changes

    public WeakReference<Player> playerRef;

    public readonly bool IsModcat;
    public readonly bool IsLightpup;
    public readonly bool IsSproutcat;
    public readonly bool IsStrawberry;

    public LightpupData Lightpup;
    public OutgrowthData Sproutcat;
    public StrawberryData Strawberry;

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
            IsModcat = true;
            IsLightpup = true;
            Lightpup = new(this);
        }
        else if (Plugin.sproutcat == name)
        {
            IsModcat = true;
            IsSproutcat = true;
            Sproutcat = new(this);
        }
        else if (Plugin.strawberry == name)
        {
            IsModcat = true;
            IsStrawberry = true;
            Strawberry = new();
        }
        else
        {
            Debug.Log($"{Plugin.MOD_NAME}: How did {player.SlugCatClass} player {player.playerState.playerNumber} get in the PlayerDataCWT?!");
        }

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
