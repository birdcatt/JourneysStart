using JourneysStart.Lightbringer.FisobsTaser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneysStart.Outgrowth.FisobsSeed;

public class SeedAbstract : AbstractConsumable
{
    //i dont have anything to save lol
    public SeedAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, TaserFisob.AbstrTaser, null, pos, ID, -1, -1, null) { }
    public override void Realize()
    {
        base.Realize();
        realizedObject ??= new OutgrowthSeed(this);
    }
}
