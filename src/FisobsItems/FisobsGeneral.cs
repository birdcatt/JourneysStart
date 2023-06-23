using Fisobs.Core;
using JourneysStart.FisobsItems.Taser;
using JourneysStart.FisobsItems.Seed;

namespace JourneysStart.FisobsItems;

public class FisobsGeneral
{
    public static void Hook()
    {
        Content.Register(new TaserFisob());
        Content.Register(new SeedFisob());

        HooksTaser.Hook();
    }
}
