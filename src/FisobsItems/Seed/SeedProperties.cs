using Fisobs.Properties;

namespace JourneysStart.FisobsItems.Seed;

public class SeedProperties : ItemProperties
{
    public override void Throwable(Player player, ref bool throwable)
        => throwable = true;

    public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
    {
        grabability = Player.ObjectGrabability.OneHand;
    }

    public override void Nourishment(Player player, ref int quarterPips)
        => quarterPips = 0;
}
