using Colour = UnityEngine.Color;
using System;
using Debug = UnityEngine.Debug;
using JourneysStart.Slugcats.Lightbringer.PlayerStuff;
using JourneysStart.Slugcats.Strawberry;
using JourneysStart.Slugcats.Outgrowth.PlayerStuff;

namespace JourneysStart.Slugcats;

public class PlayerData
{
    //yeah ok you dont need a WeakRef to PlayerData for either modcat data
    //theres nothing here that really changes

    public WeakReference<Player> playerRef;

    public bool IsModcat => IsLightpup || IsSproutcat || IsStrawberry;
    public bool IsLightpup => Lightpup != null;
    public bool IsSproutcat => Sproutcat != null;
    public bool IsStrawberry => Strawberry != null;

    public LightpupData Lightpup;
    public OutgrowthData Sproutcat;
    public StrawberryData Strawberry;

    public bool SpritesInited;
    public SlugTailTexture tailPattern;
    public bool usingDMSHeadSprite;

    public ModCompat.DressMySlugcatPatch dmsModCompat;

    public PlayerData(Player player)
    {
        Debug.Log($"{Plugin.MOD_NAME}: Adding {player.SlugCatClass} player {player.playerState.playerNumber} to PlayerDataCWT");

        playerRef = new WeakReference<Player>(player);

        if (Plugin.ModEnabled_DressMySlugcat)
            dmsModCompat = new();

        SlugcatStats.Name name = player.slugcatStats.name;

        if (Plugin.lghtbrpup == name)
        {
            Lightpup = new(this);
        }
        else if (Plugin.sproutcat == name)
        {
            Sproutcat = new(this);
        }
        else if (Plugin.strawberry == name)
        {
            Strawberry = new(this);
        }
        else
        {
            Debug.Log($"{Plugin.MOD_NAME}: How did {player.SlugCatClass} player {player.playerState.playerNumber} get in the PlayerDataCWT?!");
        }
    }

    public void Update(bool eu)
    {
        if (!playerRef.TryGetTarget(out _))
            return;

        Lightpup?.Update();
        Sproutcat?.Update();
        Strawberry?.Update(eu);
    }

    public void ModCompat_DressMySlugcat_DrawSprites(RoomCamera rCam, RoomCamera.SpriteLeaser sLeaser)
    {
        try
        {
            if (Plugin.ModEnabled_DressMySlugcat && playerRef.TryGetTarget(out Player player))
            {
                dmsModCompat?.DressMySlugcat_DrawSprites(player.graphicsModule as PlayerGraphics, sLeaser, rCam);
            }
            if (!tailPattern.usingDMSTailSprite)
                sLeaser.sprites[2].color = Colour.white; //white tail
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

    }
}
