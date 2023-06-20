using MoreSlugcats;
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Debug = UnityEngine.Debug;

namespace JourneysStart.Outgrowth;

public class RoomScripts
{
    public class SproutcatLCStart : UpdatableAndDeletable
    {
        internal static bool alreadyRun;
        private Player RealizedPlayer => room.game.Players.Count > 0 ? room.game.Players[0].realizedCreature as Player : null;
        public SproutcatLCStart(Room room)
        {
            this.room = room;
            Debug.Log($"{Plugin.MOD_NAME}: (SproutcatLCStart) Start script in {room.abstractRoom.name}");
        }
        public override void Destroy()
        {
            Debug.Log($"{Plugin.MOD_NAME}: (SproutcatLCStart) Destroy script");
            base.Destroy();
        }
        public override void Update(bool eu)
        {
            base.Update(eu);

            if (null == RealizedPlayer || alreadyRun)
            {
                return;
            }

            alreadyRun = true;

            room.game.GetStorySession.saveState.hasRobo = true;

            foreach (AbstractCreature lilGuy in room.game.Players)
            {
                Player player = lilGuy.realizedCreature as Player;
                foreach (BodyChunk bodyChunk in player.bodyChunks)
                {
                    bodyChunk.HardSetPosition(new Vector2(2696, 473));
                }
                player.flipDirection = -1;
                player.standing = false;
                player.sleepCounter = 99;
                player.sleepCurlUp = 1f;

                RWCustom.IntVector2 food = SlugcatStats.SlugcatFoodMeter(Plugin.sproutcat);
                player.playerState.foodInStomach = food.x - food.y;
            }

            //only 1 bot
            AncientBot bot = new(new Vector2(2695, 480), new Color(1f, 0f, 0f), RealizedPlayer, true);
            RealizedPlayer.myRobot = bot;
            room.AddObject(bot);

            Destroy();
        }
    } //end of SproutcatStart class
}
