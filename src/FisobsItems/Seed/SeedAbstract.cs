namespace JourneysStart.FisobsItems.Seed;

public class SeedAbstract : AbstractConsumable
{
    //i dont have anything to save lol
    public SeedAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, SeedFisob.AbstrSeed, null, pos, ID, -1, -1, null) { }
    public override void Realize()
    {
        base.Realize();
        realizedObject ??= new SproutSeed(this);
    }
}
