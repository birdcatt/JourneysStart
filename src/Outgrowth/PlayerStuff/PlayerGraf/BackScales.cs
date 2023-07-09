using System;

namespace JourneysStart.Outgrowth.PlayerStuff.PlayerGraf;

public class BackScales
{
    //note: look at SpineSpikes
    public WeakReference<Player> playerRef;

    public const string spriteName = "LizardScaleA0";

    public readonly int startIndex;
    public readonly int endIndex;

    public PlayerGraphics.AxolotlScale[] scales;

    public BackScales(PlayerGraphics pGraf, int startIndex)
    {
        playerRef = new WeakReference<Player>(pGraf.player);
        this.startIndex = startIndex;

        scales = new PlayerGraphics.AxolotlScale[3];

        for (int i = 0; i < scales.Length; i++)
        {
            scales[i] = new PlayerGraphics.AxolotlScale(pGraf);
        }

        endIndex = startIndex + scales.Length;
    }

    public void Update()
    {

    }
}
