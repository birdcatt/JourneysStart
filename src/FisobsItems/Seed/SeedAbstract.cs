using Fisobs.Core;
using Colour = UnityEngine.Color;
using RWCustom;

namespace JourneysStart.FisobsItems.Seed;

public class SeedAbstract : AbstractConsumable
{
    public Colour innerColour;
    public Colour outerColour;

    public SeedAbstract(World world, WorldCoordinate pos, EntityID ID, Colour innerColour, Colour outerColour) : base(world, SeedFisob.AbstrSeed, null, pos, ID, -1, -1, null)
    {
        if (innerColour == null)
            innerColour = Colour.white;
        this.innerColour = innerColour;

        if (outerColour == null)
            outerColour = Colour.gray;
        this.outerColour = outerColour;
    }

    public override void Realize()
    {
        base.Realize();
        realizedObject ??= new Seed(this);
    }

    public override string ToString()
    {
        return this.SaveToString($"{innerColour};{outerColour}");
    }
}
