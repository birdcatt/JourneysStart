using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;

namespace JourneysStart.FisobsItems.Seed;

public class SeedFisob : Fisob
{
    public static readonly AbstractPhysicalObject.AbstractObjectType AbstrSeed = new("OutgrowthAbstrSeed", true);
    public static readonly MultiplayerUnlocks.SandboxUnlockID ArenaSeed = new("OutgrowthArenaSeed", true);

    public static void UnregisterValues()
    {
        AbstrSeed?.Unregister();
        ArenaSeed?.Unregister();
    }

    public SeedFisob() : base(AbstrSeed)
    {
        //Icon = new SeedIcon(); //i dont have icon_Taser png yet
        RegisterUnlock(ArenaSeed, MultiplayerUnlocks.SandboxUnlockID.Slugcat, 0);
    }

    public override AbstractPhysicalObject Parse(World world, EntitySaveData saveData, SandboxUnlock unlock)
    {
        return new SeedAbstract(world, saveData.Pos, saveData.ID);
    }

    private static readonly SeedProperties properties = new();
    public override ItemProperties Properties(PhysicalObject forObject)
    {
        // If you need to use the forObject parameter, pass it to your ItemProperties class's constructor.
        // The Mosquitoes example from the Fisobs github demonstrates this.
        return properties;
    }
}
