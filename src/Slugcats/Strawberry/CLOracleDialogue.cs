using MoreSlugcats;
using Random = UnityEngine.Random;
using static JourneysStart.Utility;

namespace JourneysStart.Slugcats.Strawberry;

public class CLOracleDialogue
{
    public static void Hook()
    {
        On.MoreSlugcats.CLOracleBehavior.InitateConversation += CLOracleBehavior_InitateConversation;
    }

    private static void CLOracleBehavior_InitateConversation(On.MoreSlugcats.CLOracleBehavior.orig_InitateConversation orig, CLOracleBehavior self)
    {
        if (!IsStrawberry(self.oracle.room.game.StoryCharacter))
        {
            orig(self);
            return;
        }

        if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.halcyonStolen)
        {
            if (Random.value < 0.15f)
            {
                self.dialogBox.NewMessage("You again, thief. Why take all I have left?", 0);
                return;
            }
            if (Random.value < 0.15f)
            {
                self.dialogBox.NewMessage("After everything, I lose the one last thing I have to a simple-minded beast.", 0);
                return;
            }
            self.dialogBox.NewMessage(self.Translate("Bring the stolen pearl back, from whatever little den you've stashed it in."), 0);
            return;
        }
        else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts > 0)
        {
            if (Random.value < 0.15f)
            {
                self.dialogBox.NewMessage(self.Translate("Go away."), 0);
                return;
            }
            if (Random.value < 0.15f)
            {
                self.dialogBox.NewMessage(self.Translate("You return, and expect me to welcome you with open arms after what you've done? I haven't forgotten, beast."), 0);
                return;
            }
            //if (Random.value < 0.15f)
            //{
            //    self.dialogBox.NewMessage(self.Translate("...So little... left. Why hurt... me more..."), 0);
            //    return;
            //}
            self.dialogBox.NewMessage(self.Translate("Scurry off to wherever you and your kind dwell, and leave me alone."), 0);
            return;
        }
        else
        {
            if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0)
            {
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
                string[] convo =
                {
                    "Another one.",
                    "Have you and your kind been showing yourself the way to me? I find myself unable to understand what is so interesting about myself.",
                    "There's nothing here, not anymore. It's all gone now. Whatever remains has been crushed beneath my structure."
                };
                foreach (string msg in convo)
                {
                    self.dialogBox.NewMessage(self.Translate(msg), 0);
                }
                return;
            }

            if (self.oracle.room.world.rainCycle.TimeUntilRain < 1600)
            {
                self.rainInterrupt = true;
                if (Random.value < 0.15f)
                {
                    self.dialogBox.NewMessage(self.Translate("The hail is coming. With myself collapsed, I no longer generate enough heat to warm the surface."), 0);
                    return;
                }
                if (Random.value < 0.15f)
                {
                    self.dialogBox.NewMessage(self.Translate("...Go find... shelter."), 0);
                    return;
                }
                self.dialogBox.NewMessage(self.Translate("...Friend... please find... safety."), 0);
                return;
            }
            else
            {
                if (Random.value < 0.15f)
                {
                    self.dialogBox.NewMessage(self.Translate("Why have you come back?"), 0);
                    return;
                }
                if (Random.value < 0.15f)
                {
                    self.dialogBox.NewMessage(self.Translate("...It is... warmer... today."), 0);
                    return;
                }
                if (Random.value < 0.15f)
                {
                    self.dialogBox.NewMessage(self.Translate("...Little green friend. Hello."), 0);
                    return;
                }
                if (Random.value < 0.15f)
                {
                    self.dialogBox.NewMessage(self.Translate("...Nice to see..."), 0);
                    return;
                }
                self.dialogBox.NewMessage(self.Translate("...Thank you... for... company."), 0);
                return;
            }
        }
    }
}
