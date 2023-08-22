using Colour = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Mathf = UnityEngine.Mathf;
using UnityEngine;
using RWCustom;
//using Smoke;

namespace JourneysStart.FisobsItems.Seed;

public class Seed : Weapon, IPlayerEdible
{
    public Seed(SeedAbstract abstr) : base(abstr, abstr.world)
    {
        bodyChunks = new BodyChunk[1];
        bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 5f, 0.12f);
        bodyChunkConnections = new BodyChunkConnection[0];
        canBeHitByWeapons = false;
        airFriction = 0.999f;
        gravity = 0.9f;
        bounce = 0.2f;
        surfaceFriction = 0.7f;
        collisionLayer = 1;
        waterFriction = 0.95f;
        buoyancy = 1.1f;
    }

    #region as abstract
    public AbstractConsumable AbstrConsumable
    {
        get
        {
            return abstractPhysicalObject as AbstractConsumable;
        }
    }
    public SeedAbstract AbstrSeed
    {
        get
        {
            return abstractPhysicalObject as SeedAbstract;
        }
    }
    #endregion

    public override void Update(bool eu)
    {
        base.Update(eu);

        if (room.game.devToolsActive && Input.GetKey("b"))
        {
            firstChunk.vel += Custom.DirVec(firstChunk.pos, Futile.mousePosition) * 3f;
        }

        if (grabbedBy.Count > 0)
        {
            if (!AbstrConsumable.isConsumed)
            {
                AbstrConsumable.Consume();
            }
        }

        if (mode == Mode.Thrown && (firstChunk.ContactPoint.x != 0 || firstChunk.ContactPoint.y != 0))
        {
            Explode();
        }
    }

    public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
    {
        bool val = base.HitSomething(result, eu);
        if (val)
        {
            Explode();
        }
        return val;
    }

    #region sprites
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[2];
        sLeaser.sprites[0] = new FSprite("JetFishEyeA", true);
        sLeaser.sprites[1] = new FSprite("tinyStar", true);
        AddToContainer(sLeaser, rCam, null);
    }
    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        newContatiner ??= rCam.ReturnFContainer("Items");
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].RemoveFromContainer();
            newContatiner.AddChild(sLeaser.sprites[i]);
        }
    }
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        Vector2 vector = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
        for (int i = 0;  i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].x = vector.x - camPos.x;
            sLeaser.sprites[i].y = vector.y - camPos.y;
        }
    }
    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        sLeaser.sprites[0].color = AbstrSeed.outerColour;
        sLeaser.sprites[1].color = AbstrSeed.innerColour;

        //Colour colour = Colour.Lerp(new Colour(0.9f, 0.83f, 0.5f), palette.blackColor, 0.18f + 0.7f * rCam.PaletteDarkness());
        //sLeaser.sprites[0].color = colour;
        //sLeaser.sprites[1].color = colour + new Colour(0.3f, 0.3f, 0.3f) * Mathf.Lerp(1f, 0.15f, rCam.PaletteDarkness());
        //sLeaser.sprites[2].color = Colour.Lerp(new Colour(1f, 0f, 0f), palette.blackColor, 0.3f);
    }
    #endregion

    public void Explode()
    {
        if (slatedForDeletetion)
        {
            return;
        }
        InsectCoordinator smallInsects = null;
        for (int i = 0; i < room.updateList.Count; i++)
        {
            if (room.updateList[i] is InsectCoordinator insectCoordinator)
            {
                smallInsects = insectCoordinator;
                break;
            }
        }
        for (int j = 0; j < 70; j++)
        {
            room.AddObject(new SporeCloud(firstChunk.pos, Custom.RNV() * Random.value * 10f, AbstrSeed.innerColour, 1f, thrownBy?.abstractCreature, j % 20, smallInsects));
        }
        room.AddObject(new SporePuffVisionObscurer(firstChunk.pos));
        for (int k = 0; k < 7; k++)
        {
            room.AddObject(new PuffBallSkin(firstChunk.pos, Custom.RNV() * Random.value * 16f, color, Colour.Lerp(color, AbstrSeed.outerColour, 0.5f)));
        }
        room.PlaySound(SoundID.Puffball_Eplode, firstChunk);
        Destroy();
    }

    public void BitByPlayer(Creature.Grasp grasp, bool eu)
    {
        room.PlaySound(SoundID.Slugcat_Eat_Slime_Mold, firstChunk.pos);
        firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
        (grasp.grabber as Player).ObjectEaten(this);
        grasp.Release();
        Destroy();
    }

    public bool Edible { get { return true; } }
    public int BitesLeft { get { return 1; } }
    public int FoodPoints { get { return 0; } }
    public bool AutomaticPickUp { get { return true; } }
    public void ThrowByPlayer() { }
}

//public class SeedSpores
//{
//    //SporeCloud but kill them all
//}