using System.Linq;
using static MoreSlugcats.MoreSlugcatsEnums;
using Vector2 = UnityEngine.Vector2;
using Debug = UnityEngine.Debug;
using static JourneysStart.Plugin;

namespace JourneysStart.Slugcats.Strawberry;

public class RoomScripts
{
    public class StrawberryStart : UpdatableAndDeletable
    {
        public static bool alreadyRun; //god.
        private Player RealizedPlayer => room.game.Players.Count > 0 ? room.game.Players[0].realizedCreature as Player : null;

        public StrawberryStart(Room room)
        {
            this.room = room;
            Debug.Log($"{Plugin.MOD_NAME}: Created new {nameof(StrawberryStart)} room script in {room.abstractRoom.name}");
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (RealizedPlayer == null)
            {
                return;
            }

            alreadyRun = true;

            TeleportPlayers();
            SpawnBlue();
        }

        public void TeleportPlayers()
        {
            if (alreadyRun) return;

            Vector2 spawnPos = new(543, 914); //this pos is for HI_B02
            foreach (var abstrPlayer in room.game.Players)
            {
                if (abstrPlayer.realizedCreature == null) continue;

                var player = abstrPlayer.realizedCreature as Player;
                player.SuperHardSetPosition(spawnPos);
                player.standing = false;
            }
        }
        public void SpawnBlue()
        {
            //spawn new pup and put on strawberry's back
            if (!BlueExists)
            {
                var abstractSlugpup = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplateType.SlugNPC), null, RealizedPlayer.abstractCreature.pos, StrawbBlueID);

                room.abstractRoom.entities.Add(abstractSlugpup);
                abstractSlugpup.RealizeInRoom();

                if (RealizedPlayer.FreeHand() != -1)
                    RealizedPlayer.SlugcatGrab(abstractSlugpup.realizedCreature, RealizedPlayer.FreeHand());
                else if (!P1HasASlugOnBack(out _))
                {
                    RealizedPlayer.slugOnBack.SlugToBack(abstractSlugpup.realizedCreature as Player);
                }
            }
        }

        public bool BlueExists => P1HasASlugOnBack(out Player player) && player.abstractCreature.ID == StrawbBlueID || P1HasBlueInHand || BlueIsInRoom;

        private bool BlueIsInRoom => room.abstractRoom.creatures.Any(abstrCrit => abstrCrit.realizedCreature is Player && abstrCrit.ID == StrawbBlueID);
        private bool P1HasASlugOnBack(out Player player)
        {
            player = RealizedPlayer != null && RealizedPlayer.slugOnBack != null ? RealizedPlayer.slugOnBack.slugcat : null;
            return player != null;
        }
        private bool P1HasBlueInHand => RealizedPlayer.grasps.Any(grasps => grasps?.grabbed is Player player && player.abstractCreature.ID == StrawbBlueID);
    }
}
