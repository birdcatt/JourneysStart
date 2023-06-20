using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
//using Colour = UnityEngine.Color;

namespace JourneysStart.Lightbringer.FisobsTaser;

public class TaserFisob : Fisob
{
    public static readonly AbstractPhysicalObject.AbstractObjectType AbstrTaser = new("JourneysStart_LightpupAbstrTaser", true);
    public static readonly MultiplayerUnlocks.SandboxUnlockID ArenaTaser = new("JourneysStart_LightpupArenaTaser", true);

    public static void UnregisterValues()
    {
        AbstrTaser?.Unregister();
        ArenaTaser?.Unregister();
    }

    public TaserFisob() : base (AbstrTaser)
    {
        //Icon = new TaserIcon(); //i dont have icon_Taser png yet

        RegisterUnlock(ArenaTaser, MultiplayerUnlocks.SandboxUnlockID.Slugcat, 0); //unlocked when slugcat is unlocked
    }

    #pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override AbstractPhysicalObject Parse(World world, EntitySaveData saveData, SandboxUnlock? unlock)
    #pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        //This saves the data of the object and also handles its hue when it’s a sandbox unlock.

        // Crate data is just floats separated by ; characters.
        string[] p = saveData.CustomData.Split(';');

        if (p.Length < 3)
        {
            p = new string[3];
        }

        var result = new TaserAbstract(world, saveData.Pos, saveData.ID)
        {
            //hue = float.TryParse(p[0], out var h) ? h : 0,
            //saturation = float.TryParse(p[1], out var s) ? s : 1,
            //scaleX = float.TryParse(p[2], out var x) ? x : 1,
            //scaleY = float.TryParse(p[3], out var y) ? y : 1,
            
            scaleX = float.TryParse(p[0], out var x) ? x : 1,
            scaleY = float.TryParse(p[1], out var y) ? y : 1,
            electricCharge = int.TryParse(p[2], out var e) ? e : 1,
        };

        // If this is coming from a sandbox unlock, the hue and size should depend on the data value (see CrateIcon below).
        if (unlock is SandboxUnlock u)
        {
            //result.hue = u.Data / 1000f;

            if (u.Data == 0)
            {
                result.scaleX += 0.2f;
                result.scaleY += 0.2f;
            }
        }

        return result;
    }

    //initialise our object’s properties
    private static readonly TaserProperties properties = new();
    public override ItemProperties Properties(PhysicalObject forObject)
    {
        // If you need to use the forObject parameter, pass it to your ItemProperties class's constructor.
        // The Mosquitoes example from the Fisobs github demonstrates this.
        return properties;
    }
}
