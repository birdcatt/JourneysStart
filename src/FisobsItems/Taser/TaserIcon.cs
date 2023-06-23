using Fisobs.Core;
using Colour = UnityEngine.Color;

namespace JourneysStart.FisobsItems.Taser;

public class TaserIcon : Icon
{
    public override int Data(AbstractPhysicalObject apo)
    {
        //hue: 0 is red, 70 is orange
        //return apo is TaserAbstract taser ? (int)(taser.hue * 1000f) : 0;

        //oh is this for the colour of the arena icon
        return apo is TaserAbstract ? (int)(200 * 1000f) : 0;
    }

    public override Colour SpriteColor(int data)
    {
        return RWCustom.Custom.HSL2RGB(data / 1000f, 0.65f, 0.4f);
    }

    public override string SpriteName(int data)
    {
        // Fisobs autoloads the embedded resource named `icon_{Type}` automatically
        return "icon_LightpupTaser";
    }
}
