using UnityEngine;
//using RWCustom;
//using Smoke;

namespace JourneysStart.FisobsItems.Seed;

public class Seed : PlayerCarryableItem, IDrawable, IPlayerEdible
{
    public Seed(SeedAbstract abstr) : base(abstr)
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
    public AbstractConsumable AbstrSeed
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
    }

    public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[3];
        sLeaser.sprites[0] = new FSprite("JetFishEyeA", true);
        sLeaser.sprites[1] = new FSprite("JetFishEyeA", true);
        sLeaser.sprites[2] = new FSprite("tinyStar", true);
        AddToContainer(sLeaser, rCam, null);
    }
    public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        Vector2 vector = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
        for (int i = 0;  i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].x = vector.x - camPos.x;
            sLeaser.sprites[i].y = vector.y - camPos.y;
        }
    }
    public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        Color color = Color.Lerp(new Color(0.9f, 0.83f, 0.5f), palette.blackColor, 0.18f + 0.7f * rCam.PaletteDarkness());
        sLeaser.sprites[0].color = color;
        sLeaser.sprites[1].color = color + new Color(0.3f, 0.3f, 0.3f) * Mathf.Lerp(1f, 0.15f, rCam.PaletteDarkness());
        sLeaser.sprites[2].color = Color.Lerp(new Color(1f, 0f, 0f), palette.blackColor, 0.3f);
    }
    public virtual void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        newContatiner ??= rCam.ReturnFContainer("Items");
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].RemoveFromContainer();
            newContatiner.AddChild(sLeaser.sprites[i]);
        }
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
