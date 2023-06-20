﻿using Fisobs.Properties;

namespace JourneysStart.Lightbringer.FisobsTaser;

public class TaserProperties : ItemProperties
{
    public override void Throwable(Player player, ref bool throwable)
        => throwable = true;

    public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
    {
        grabability = Player.ObjectGrabability.OneHand;
    }
}
