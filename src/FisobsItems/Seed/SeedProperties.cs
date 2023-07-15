using Fisobs.Properties;

namespace JourneysStart.FisobsItems.Seed;

public class SeedProperties : ItemProperties
{
    public override void Throwable(Player player, ref bool throwable)
        => throwable = true;

    public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
        => grabability = Player.ObjectGrabability.OneHand;

    public override void Nourishment(Player player, ref int quarterPips)
        => quarterPips = 0;

    public override void LethalWeapon(Scavenger scav, ref bool isLethal)
        => isLethal = false;

    public override void ScavCollectScore(Scavenger scav, ref int score)
        => score = 0;
}
