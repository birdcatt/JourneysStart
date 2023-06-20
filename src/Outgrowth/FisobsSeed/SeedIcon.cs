using Fisobs.Core;
using Colour = UnityEngine.Color;

namespace JourneysStart.Outgrowth.FisobsSeed;

public class SeedIcon : Icon
{
    public override int Data(AbstractPhysicalObject apo)
    {
        //hue: 0 is red, 70 is orange
        return apo is SeedAbstract ? (int)(148 * 1000f) : 0; //hope its right, its the H in HSL of outgrowths colour
    }

    public override Colour SpriteColor(int data)
    {
        return RWCustom.Custom.HSL2RGB(data / 1000f, 0.65f, 0.4f);
    }

    public override string SpriteName(int data)
    {
        // Fisobs autoloads the embedded resource named `icon_{Type}` automatically
        return "icon_OutgrowthSeed";
    }
}
