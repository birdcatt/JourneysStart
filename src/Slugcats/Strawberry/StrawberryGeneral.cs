namespace JourneysStart.Slugcats.Strawberry;

public class StrawberryGeneral
{
    public static void Hook()
    {
        GeneralHooks();
    }
    public static void GeneralHooks()
    {
        On.Player.CanIPickThisUp += Player_CanIPickThisUp;
        On.Player.UpdateBodyMode += Player_UpdateBodyMode;
    }

    private static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        return orig(self, obj) || self.SlugCatClass == Plugin.strawberry && obj is Spear spear && spear.mode == Weapon.Mode.StuckInWall;
    }

    private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player self)
    {
        orig(self);

        if (Plugin.PlayerDataCWT.TryGetValue(self, out var pData) && pData.IsStrawberry)
        {
            if (Player.BodyModeIndex.Crawl == self.bodyMode)
            {
                for (int i = 0; i < self.dynamicRunSpeed.Length; i++)
                {
                    self.dynamicRunSpeed[i] *= 1f + 0.85f;
                }
            }
            else if (Player.AnimationIndex.Roll == self.animation)
            {
                //yo thanks for the roll extender code bry!
                if (self.rollCounter > 15 && pData.Strawberry.rollExtender < (self.Malnourished ? 25 : 40) && self.input[0].downDiagonal != 0)
                {
                    //extends roll duration
                    pData.Strawberry.rollExtender++;
                    self.rollCounter--;
                }

                if (self.stopRollingCounter > 3 && pData.Strawberry.rollFallExtender < 12)
                {
                    //extends how long you can fall mid-roll w/o fall being cancelled
                    pData.Strawberry.rollFallExtender++;
                    self.stopRollingCounter--;
                }
            }
            else
            {
                pData.Strawberry.rollExtender = 0;
                pData.Strawberry.rollFallExtender = 0;
            }
        }
    }
}
