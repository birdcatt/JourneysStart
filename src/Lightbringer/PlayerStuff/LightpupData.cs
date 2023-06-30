using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
using KeyCode = UnityEngine.KeyCode;
using Input = UnityEngine.Input;
using Vector2 = UnityEngine.Vector2;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;
using Colour = UnityEngine.Color;
using Flare = JourneysStart.Lightbringer.Data.Flare;
using JourneysStart.FisobsItems.Taser;
using JourneysStart.Shared.PlayerStuff;

namespace JourneysStart.Lightbringer.PlayerStuff;

sealed class LightpupData
{
    public PlayerData playerData;

    public CustomController controller;
    public Crafting_SecondItem crafting_SecondItem;

    public int flareCooldown;
    public int flareCharge;
    public const int flareChargeMax = 4;
    public readonly KeyCode flareInput;

    public int idleLookCounter;
    public Vector2 idlePoint;

    public int stripeIndex;

    public bool HitByZapcoil = false;

    public LightpupData(PlayerData playerData)
    {
        this.playerData = playerData;
        playerData.playerRef.TryGetTarget(out Player player);

        flareCooldown = 0;
        flareCharge = flareChargeMax;
        flareInput = player.playerState.playerNumber switch
        {
            1 => ConfigMenu.p2FlareKey.Value,
            2 => ConfigMenu.p3FlareKey.Value,
            3 => ConfigMenu.p4FlareKey.Value,
            _ => ConfigMenu.p1FlareKey.Value //default
        };

        idleLookCounter = 0;
        idlePoint = player.firstChunk.pos;

        controller = new();

        crafting_SecondItem = new();
    }

    public void Update()
    {
        if (!playerData.playerRef.TryGetTarget(out Player player))
            return;

        if (flareChargeMax > flareCharge)
            playerData.tailPattern.RecolourUVMapTail(); //so the colours arent wrong after going thru pipes/hypothermia

        if (player.dead)
            return;

        SpawnFlare();
        IdleLook();
        FoodReaction();
    }

    public void SpawnFlare()
    {
        if (0 == flareCharge || flareCooldown --> 0)
            return;

        if (!Input.GetKey(flareInput)) //check other inputs too, so cant do this while eating or sleeping
            return;

        RemoveFlareCharge();

        flareCooldown = ConfigMenu.flareCooldown.Value * 40; //update runs 40 times a second

        playerData.playerRef.TryGetTarget(out Player player);
        Room room = player.room;
        AbstractConsumable flareBomb = new(room.world, AbstractObjectType.FlareBomb, null, room.GetWorldCoordinate(player.bodyChunks[1].pos), room.game.GetNewID(), -1, -1, null);
        Flare flare = new(player, flareBomb);

        room.abstractRoom.AddEntity(flare.abstractPhysicalObject);
        flare.abstractPhysicalObject.RealizeInRoom();
        flare.StartBurn();
    }

    public void IdleLook()
    {
        playerData.playerRef.TryGetTarget(out Player player);

        if (!(Plugin.IdleLookWaitInput.TryGet(player, out int value) && Plugin.IdleLookWaitRandomRange.TryGet(player, out int[] randRange)
            && Plugin.IdleLookVectorRange.TryGet(player, out int[] vec) && Plugin.IdleLookPointChangeTime.TryGet(player, out int time)))
            return;

        bool hasMushroom = player.mushroomCounter > 0;
        int waitInputTime = value * 40;

        if (!hasMushroom)
        {
            if (player.input[0].AnyInput)
            {
                idleLookCounter = 0;
                return;
            }
            idleLookCounter++;
        }
        else if (idleLookCounter <= waitInputTime)
            return;

        if (int.MaxValue - 1 == idleLookCounter)
            idleLookCounter = waitInputTime;

        if (hasMushroom || 0 == idleLookCounter % (time * (40 + Random.Range(randRange[0], randRange[1]))))
        {
            //change where the point is
            idlePoint = player.firstChunk.pos;
            idlePoint.x += Random.Range(vec[0], vec[1]);
            idlePoint.y += Random.Range(vec[2], vec[3]);

            //look at it
            (player.graphicsModule as PlayerGraphics).LookAtPoint(idlePoint, 0.2f);
        }
    }

    public void FoodReaction()
    {
        playerData.playerRef.TryGetTarget(out Player player);

        if (player.room.world.game.session is ArenaGameSession arena && arena is not SandboxGameSession) //condition evaluates as intended
            return;

        //when in air, bodyMode is default
        if (0 < controller.likesFood && Player.BodyModeIndex.Stand == player.bodyMode)
        {
            player.controller = controller;
            player.input[0] = controller.GetInput();
            return;
        }
        else if (Player.BodyModeIndex.Default != player.bodyMode || player.exhausted)
            controller.likesFood = 0;

        if (null != player.controller)
            player.controller = null;
    }

    public void RemoveFlareCharge()
    {
        flareCharge--;
        Debug.Log($"{Plugin.MOD_NAME}: Flare charge used up ({flareCharge}/{flareChargeMax}) left)");

        playerData.playerRef.TryGetTarget(out Player player);

        //stripe colour darkens
        Colour newColour = Colour.Lerp(playerData.tailPattern.PatternColour, player.room.game.cameras[0].currentPalette.blackColor, 0.5f);
        playerData.tailPattern.OldPatternColour = newColour; //for hypothermia and all
        playerData.tailPattern.RecolourTail(playerData.tailPattern.BodyColour, newColour);
    }

    #region nested classes
    public class Crafting_SecondItem
    {
        AbstractPhysicalObject item = null;
        public void Set(AbstractObjectType abstrType, EntityID id, int charge, World world, WorldCoordinate coord)
        {
            if (AbstractObjectType.Spear == abstrType)
            {
                item = new TaserAbstract(world, coord, id)
                {
                    electricCharge = charge
                };
            }
            else //if taser
            {
                item = new AbstractSpear(world, null, coord, id, false, true)
                {
                    electricCharge = charge
                };
            }
        }
        public AbstractPhysicalObject Get()
        {
            AbstractPhysicalObject val = item;
            item = null;
            return val;
        }
    }

    public class CustomController : Player.PlayerController
    {
        //thanks to Bro for the controller code
        //meant to override and force the player to jump

        public int likesFood;
        //private Player player;
        public CustomController()
        {
            //this.player = player;
            likesFood = 0;
        }
        public override Player.InputPackage GetInput()
        {
            if (likesFood > 0)
            {
                likesFood--;
                return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 0, 0, true, false, false, false, false);
            }
            return base.GetInput();
            //return player.input[0];
        }
    }
    #endregion
}
