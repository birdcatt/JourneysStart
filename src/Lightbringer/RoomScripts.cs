using Debug = UnityEngine.Debug;
using Vector2 = UnityEngine.Vector2;
using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
using AbstractDataPearl = DataPearl.AbstractDataPearl;
using static JourneysStart.Lightbringer.Data.FRDData;
using static JourneysStart.Utility;

namespace JourneysStart.Lightbringer;

public class RoomScripts
{
    #region GATE_SB_OE den scripts
    public class LightpupGateStart : UpdatableAndDeletable
    {
        public static bool alreadyRun;
        private Player RealizedPlayer => room.game.Players.Count > 0 ? room.game.Players[0].realizedCreature as Player : null; //nymph and warp menu does this;
        public LightpupGateStart(Room room)
        {
            this.room = room;
            Debug.Log($"{Plugin.MOD_NAME}: Spawn start Script in {room.abstractRoom.name}");
        }
        public override void Update(bool eu)
        {
            base.Update(eu);

            if (null == RealizedPlayer || alreadyRun)
            {
                return;
            }

            alreadyRun = true;
            RealizedPlayer.craftingTutorial = false;

            room.regionGate.mode = RegionGate.Mode.ClosingMiddle;
            room.regionGate.doors[1].closedFac = 0f;

            foreach (AbstractCreature lilGuy in room.game.Players)
            {
                Player player = lilGuy.realizedCreature as Player;
                foreach (BodyChunk bodyChunk in player.bodyChunks)
                {
                    bodyChunk.HardSetPosition(new Vector2(657, 176));
                }
                player.standing = true;
            }

            RealizedPlayer.objectInStomach = new AbstractDataPearl(room.world, AbstractObjectType.DataPearl, null, RealizedPlayer.abstractPhysicalObject.pos, room.game.GetNewID(), -1, -1, null, LightpupPearl);
            room.game.GetStorySession.saveState.miscWorldSaveData.playerGuideState.likesPlayer = 1f;

            Destroy();
        }
    }

    public class LightpupPuffBallTutorial : UpdatableAndDeletable
    {
        public LightpupPuffBallTutorial(Room room)
        {
            this.room = room;
            Debug.Log($"{Plugin.MOD_NAME}: Puffball recipe Script in {room.abstractRoom.name}");
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            if (room.game.FirstAlivePlayer.realizedCreature is Player player && !player.craftingTutorial)
            {
                player.craftingTutorial = true;
                room.game.cameras[0].hud.textPrompt.AddMessage("Spore puffs can be crafted with a gooieduck and a rock. There are plenty of other crafting recipes out there!", 40, 240, false, true);
            }
            Destroy();
        }
    }
    #endregion

    #region OE_FINAL03 den scripts
    public class LightpupOEStart : UpdatableAndDeletable
    {

        internal static bool alreadyRun;
        private Player RealizedPlayer => room.game.Players.Count > 0 ? room.game.Players[0].realizedCreature as Player : null;
        public LightpupOEStart(Room room)
        {
            this.room = room;
            Debug.Log($"{Plugin.MOD_NAME}: (LightpupOEStart) Start script in {room.abstractRoom.name}");
        }
        public override void Destroy()
        {
            Debug.Log($"{Plugin.MOD_NAME}: (LightpupOEStart) Destroy script");
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
            RealizedPlayer.craftingTutorial = false;

            if (Plugin.Lightpup_Debug_DisableStartRain.TryGet(room.game, out bool skipRain) && !skipRain)
                room.world.rainCycle.timer = room.world.rainCycle.cycleLength; //rains not lethal past sunken pier :(

            foreach (AbstractCreature lilGuy in room.game.Players)
            {
                Player player = lilGuy.realizedCreature as Player;
                foreach (BodyChunk bodyChunk in player.bodyChunks)
                {
                    bodyChunk.HardSetPosition(new Vector2(318, 174));
                }
                player.flipDirection = 1;
                player.standing = false;
                player.playerState.foodInStomach = SlugcatStats.SlugcatFoodMeter(Plugin.lghtbrpup).y;
            }

            AbstractDataPearl porl = new(room.world, AbstractObjectType.DataPearl, null, RealizedPlayer.abstractPhysicalObject.pos, room.game.GetNewID(), -1, -1, null, LightpupPearl);

            if (-1 != RealizedPlayer.FreeHand())
            {
                SpawnItemInHand(RealizedPlayer, porl);
            }
            else
                RealizedPlayer.objectInStomach = porl;

            room.game.GetStorySession.saveState.miscWorldSaveData.playerGuideState.likesPlayer = 1f;

            Destroy();
        }
    }
    public class LightpupOEPearlWarning : UpdatableAndDeletable
    {
        //oe_cave03
        public LightpupOEPearlWarning(Room room)
        {
            this.room = room;
            Debug.Log($"{Plugin.MOD_NAME}: (LightpupOEPearlWarning) OE Pearl warning in {room.abstractRoom.name}");
        }
        public override void Update(bool eu)
        {
            base.Update(eu);

            foreach (AbstractCreature lilGuy in room.game.Players)
            {
                Player player = lilGuy.realizedCreature as Player;

                if (player.objectInStomach is AbstractDataPearl pearl && pearl.dataPearlType == LightpupPearl)
                    goto EndPearlWarning;

                foreach (Creature.Grasp grasp in player.grasps)
                {
                    if (grasp?.grabbed.abstractPhysicalObject is AbstractDataPearl p && p.dataPearlType == LightpupPearl)
                        goto EndPearlWarning;
                }
            }
            
            room.game.cameras[0].hud.textPrompt.AddMessage("WARNING: You are about to leave Outer Expanse without the campaign-specific pearl.", 40, 240, false, true);
            room.game.cameras[0].hud.textPrompt.AddMessage("This pearl is required to unlock the alternate ending of The Lightbringer campaign.", 40, 240, false, true);

            EndPearlWarning:
            Destroy();
        }
    }
    #endregion

    //then add ending script, inherit from OE_NPCCONTROL
}
