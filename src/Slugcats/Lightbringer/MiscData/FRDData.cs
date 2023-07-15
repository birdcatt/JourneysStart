using MonoMod.RuntimeDetour;
using System.Reflection;
using Colour = UnityEngine.Color;
using static JourneysStart.Utility;
//using System.Linq;
using Debug = UnityEngine.Debug;
using Custom = RWCustom.Custom;
using DataPearlType = DataPearl.AbstractDataPearl.DataPearlType;
//using System.Collections.Generic;

namespace JourneysStart.Slugcats.Lightbringer.MiscData;

public class FRDData
{
    public static DataPearlType LightpupPearl = new("LightpupPearl", true);
    public static Conversation.ID LightpupPearlConvoID = new("LightpupPearlConvoID", true);

    public static readonly Colour FRDColour = Custom.hexToColor("0dfafb");
    public static readonly Colour FRDPearlHighlightColour = Custom.hexToColor("f7aeeb");

    public static void Hook()
    {
        On.DataPearl.ApplyPalette += DataPearl_ApplyPalette;
        On.DataPearl.UniquePearlMainColor += DataPearl_UniquePearlMainColor;
        On.DataPearl.UniquePearlHighLightColor += DataPearl_UniquePearlHighLightColor;

        new Hook(typeof(OverseerGraphics).GetProperty("MainColor", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            typeof(FRDData).GetMethod("OverseerGraphics_MainColor_get", BindingFlags.Static | BindingFlags.Public));
        On.OverseersWorldAI.DirectionFinder.StoryRoomInRegion += DirectionFinder_StoryRoomInRegion;
    }

    #region pearl
    public static void DataPearl_ApplyPalette(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);

        if (LightpupPearl == self.AbstractPearl.dataPearlType)
        {
            self.color = FRDColour;
            self.highlightColor = FRDPearlHighlightColour;
        }
    }
    public static Colour DataPearl_UniquePearlMainColor(On.DataPearl.orig_UniquePearlMainColor orig, DataPearlType pearlType)
    {
        Colour val = orig(pearlType);
        if (LightpupPearl == pearlType)
            return FRDColour;
        return val;
    }
    public static Colour? DataPearl_UniquePearlHighLightColor(On.DataPearl.orig_UniquePearlHighLightColor orig, DataPearlType pearlType)
    {
        Colour? val = orig(pearlType);
        if (LightpupPearl == pearlType)
            return FRDPearlHighlightColour;
        return val;
    }
    #endregion

    #region overseer
    public delegate Colour orig_OverseerMainColor(OverseerGraphics self);
    public static Colour OverseerGraphics_MainColor_get(orig_OverseerMainColor orig, OverseerGraphics self)
    {
        Colour val = orig(self);
        if (self.overseer.room?.world.game.session is StoryGameSession story && Plugin.lghtbrpup == story.game.StoryCharacter && self.overseer.PlayerGuide)
        {
            return FRDColour;
        }
        return val;
    }
    public static string DirectionFinder_StoryRoomInRegion(On.OverseersWorldAI.DirectionFinder.orig_StoryRoomInRegion orig, OverseersWorldAI.DirectionFinder self, string currentRegion, bool metMoon)
    {
        string val = orig(self, currentRegion, metMoon);
        if (ProgressionUnlocked(self.world.game))
        {
            if ("OE" == currentRegion)
            {
                self.showGateSymbol = false;
                return "OE_FINAL03";
            }
        }
        return val;
    }
    #endregion
}
