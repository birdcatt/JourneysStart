using Fisobs.Core;

namespace JourneysStart.FisobsItems.Taser;

public class TaserAbstract : AbstractPhysicalObject
{
    public float scaleX;
    public float scaleY;
    public int electricCharge;

    public TaserAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, TaserFisob.AbstrTaser, null, pos, ID)
    {
        scaleX = 0.8f;
        scaleY = 0.8f;
        electricCharge = 3;
    }

    public override void Realize()
    {
        base.Realize();
        realizedObject ??= new Taser(this); //same as if (realizedObject == null) realizedObject = new Taser(this);
        //returns value of left-hand operand if not null, otherwise returns the right
    }

    public override string ToString()
    {
        return this.SaveToString($"{scaleX};{scaleY};{electricCharge}");
    }
}
