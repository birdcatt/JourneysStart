using Fisobs.Core;
using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Colour = UnityEngine.Color;
using Custom = RWCustom.Custom;
using MoreSlugcats;
using static JourneysStart.Utility;

namespace JourneysStart.FisobsItems.Taser;

public class Taser : Rock
{
    Colour electricColour;
    public readonly float[] fluxTimers;
    public readonly float[] fluxSpeeds;
    public bool exploded;
    public int destroyCounter;

    public Taser(TaserAbstract abstr) : base(abstr, abstr.world)
    {
        Random.InitState(abstractPhysicalObject.ID.RandomSeed);
        electricColour = Custom.HSL2RGB(Random.Range(0.55f, 0.7f), Random.Range(0.8f, 1f), Random.Range(0.3f, 0.6f));

        fluxTimers = new float[4];
        fluxSpeeds = new float[4];
        for (int i = 0; i < fluxSpeeds.Length; i++)
            ResetFluxSpeed(i);

        destroyCounter = 0;
    }

    public TaserAbstract AbstractTaser
    {
        get
        {
            return abstractPhysicalObject as TaserAbstract;
        }
    }

    public override void Update(bool eu)
    {
        base.Update(eu);

        //from electric spear
        if (exploded)
        {
            destroyCounter++;
            for (int j = 0; j < 2; j++)
                room.AddObject(new ExplosiveSpear.SpearFragment(firstChunk.pos, Custom.RNV() * Mathf.Lerp(20f, 40f, Random.value)));
            if (destroyCounter > 4)
            {
                room.PlaySound(SoundID.Zapper_Zap, firstChunk.pos, 1f, 0.4f + 0.25f * Random.value);
                Destroy();
            }
        }
        for (int i = 0; i < fluxSpeeds.Length; i++)
        {
            fluxTimers[i] += fluxSpeeds[i];
            if (fluxTimers[i] > 6.2831855f)
                ResetFluxSpeed(i);
        }
        if (Random.value < (0.025f * (0.33f * AbstractTaser.electricCharge)))
            Spark();
    }

    public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
    {
        //rock
        if (result.obj == null)
            return false;

        if (thrownBy is Scavenger scav && scav.AI != null)
            scav.AI.HitAnObjectWithWeapon(this, result.obj);

        vibrate = 20;
        ChangeMode(Mode.Free);

        if (result.obj is Creature crit && crit != thrownBy)
        {
            //mix of jellyfish collide and electric spear electrocute
            bool critIsElec = CheckElectricCreature(crit);
            bool isUnderwater = false;
            bool recharged = false;

            if (critIsElec)
            {
                if (AbstractTaser.electricCharge <= 0)
                {
                    Recharge();
                    recharged = true;
                }
                //now its rock
                HitSomethingRock(result, crit);
            }
            else if (AbstractTaser.electricCharge > 0)
            {
                if (crit is not BigEel)
                {
                    crit.Violence(firstChunk, new Vector2?(firstChunk.vel * firstChunk.mass), result.chunk, result.onAppendagePos, Creature.DamageType.Electric, 0.1f, (crit is Player) ? 140f : (320f * Mathf.Lerp(crit.Template.baseStunResistance, 1f, 0.5f)));
                    room.AddObject(new CreatureSpasmer(crit, false, crit.stun));
                }
                if (Submersion <= 0.5f && crit.Submersion > 0.5f)
                {
                    room.AddObject(new UnderwaterShock(room, null, result.chunk.pos, 10, 800f, 2f, thrownBy, new Colour(0.8f, 0.8f, 1f)));
                    isUnderwater = true;
                }
                room.PlaySound(SoundID.Zapper_Zap, firstChunk.pos, 1f, 1.5f + Random.value * 1.5f);
                room.AddObject(new Explosion.ExplosionLight(firstChunk.pos, 200f, 1f, 4, new Colour(0.7f, 1f, 1f)));
                if (--AbstractTaser.electricCharge == 0)
                    ShortCircuit();
            }
            else //electric charge is 0
                HitSomethingRock(result, crit);

            if (!recharged && critIsElec && Random.value < (critIsElec ? 0.4f : ((AbstractTaser.electricCharge == 1) ? 0.2f : 0f)) || isUnderwater)
            {
                if (critIsElec)
                    ExplosiveShortCircuit();
                else
                    ShortCircuit();
            }
        } //back to rock
        else if (result.chunk != null)
            result.chunk.vel += firstChunk.vel * firstChunk.mass / result.chunk.mass;
        else if (result.onAppendagePos != null)
            (result.obj as IHaveAppendages).ApplyForceOnAppendage(result.onAppendagePos, firstChunk.vel * firstChunk.mass);

        firstChunk.vel = firstChunk.vel * -0.5f + Custom.DegToVec(Random.value * 360f) * Mathf.Lerp(0.1f, 0.4f, Random.value) * firstChunk.vel.magnitude;
        if (result.chunk != null)
            room.AddObject(new ExplosionSpikes(room, result.chunk.pos + Custom.DirVec(result.chunk.pos, result.collisionPoint) * result.chunk.rad, 5, 2f, 4f, 4.5f, 30f, new Colour(1f, 1f, 1f, 0.5f)));
        SetRandomSpin();

        return true;
    }

    public override void Thrown(Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, RWCustom.IntVector2 throwDir, float frc, bool eu)
    {
        base.Thrown(thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
        Spark();
    }
    public override void WeaponDeflect(Vector2 inbetweenPos, Vector2 deflectDir, float bounceSpeed)
    {
        base.WeaponDeflect(inbetweenPos, deflectDir, bounceSpeed);
        Zap();
    }

    public void HitSomethingRock(SharedPhysics.CollisionResult result, Creature crit)
    {
        float stunBonus = 45f / 2;
        if (ModManager.MMF && MMF.cfgIncreaseStuns.Value && (result.obj is Cicada || result.obj is LanternMouse || ModManager.MSC && result.obj is Yeek))
            stunBonus = 90f / 2;
        else if (ModManager.MSC && room.game.IsArenaSession && room.game.GetArenaGameSession.chMeta != null)
            stunBonus = 90f / 2;
        crit.Violence(firstChunk, new Vector2?(firstChunk.vel * firstChunk.mass), result.chunk, result.onAppendagePos, Creature.DamageType.Blunt, 0.01f, stunBonus);
        room.PlaySound(SoundID.Rock_Hit_Creature, firstChunk);
    }
    #region sprites
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        Random.InitState(abstractPhysicalObject.ID.RandomSeed);
        sLeaser.sprites = new FSprite[4];
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            FSprite fSprite;

            if (i == 0)
                fSprite = new FSprite("ShortcutArrow", true);
            else if (i == sLeaser.sprites.Length - 1)
            {
                if (Random.value < 0.5f)
                    fSprite = new FSprite("Pebble10", true);
                else
                    fSprite = new FSprite("Pebble9", true);
            }
            else
                fSprite = new FSprite("Pebble" + Random.Range(1, 12).ToString(), true);

            sLeaser.sprites[i] = fSprite;
            sLeaser.sprites[i].scaleX = AbstractTaser.scaleX;
            sLeaser.sprites[i].scaleY = AbstractTaser.scaleY;
        }
        AddToContainer(sLeaser, rCam, null);
    }
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (blink > 0)
        {
            if (blink > 1 && Random.value < 0.5f)
                sLeaser.sprites[0].color = new Colour(1f, 1f, 1f);
            else
                sLeaser.sprites[0].color = color;
        }
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            Vector2 vector = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;

            sLeaser.sprites[i].x = vector.x;
            sLeaser.sprites[i].y = vector.y;
            sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(Vector2.zero, Vector3.Slerp(lastRotation, rotation, timeStacker));

            sLeaser.sprites[i].color = Colour.Lerp(electricColour, Colour.white, Mathf.Abs(Mathf.Sin(fluxTimers[i]))); //the fluxTimer is why it's a for and not foreach
            if (AbstractTaser.electricCharge < 3)
                SetChargeDependantElectricColour(sLeaser, rCam, i, AbstractTaser.electricCharge);
        }

        if (slatedForDeletetion || room != rCam.room)
            sLeaser.CleanSpritesAndRemove();
    }
    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        newContainer ??= rCam.ReturnFContainer("Items");

        foreach (FSprite fsprite in sLeaser.sprites)
            newContainer.AddChild(fsprite);
    }
    #endregion

    #region electric things
    public bool CheckElectricCreature(Creature otherObject)
    {
        return otherObject is Centipede || otherObject is BigJellyFish || otherObject is Inspector;
    }
    public void ExplosiveShortCircuit()
    {
        ShortCircuit();
        exploded = true;
    }
    public void ShortCircuit()
    {
        if (AbstractTaser.electricCharge == 0)
            return;

        Vector2 pos = firstChunk.pos;
        room.AddObject(new Explosion.ExplosionLight(pos, 40f, 1f, 2, electricColour));

        for (int i = 0; i < 8; i++)
        {
            Vector2 a = Custom.RNV();
            room.AddObject(new Spark(pos + a * Random.value * 10f, a * Mathf.Lerp(6f, 18f, Random.value), electricColour, null, 4, 18));
        }

        room.AddObject(new ShockWave(pos, 30f, 0.035f, 2, false));
        room.PlaySound(SoundID.Fire_Spear_Pop, pos);
        room.PlaySound(SoundID.Firecracker_Bang, pos);
        room.InGameNoise(new Noise.InGameNoise(pos, 800f, this, 1f));
        vibrate = Math.Max(vibrate, 6);
        AbstractTaser.electricCharge = 0;
    }
    public void Recharge()
    {
        AbstractTaser.electricCharge = 3;
        room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, firstChunk.pos);
        room.AddObject(new Explosion.ExplosionLight(firstChunk.pos, 200f, 1f, 4, new Colour(0.7f, 1f, 1f)));
        Spark();
        Zap();
        room.AddObject(new ZapCoil.ZapFlash(firstChunk.pos, 25f));
    }
    public void ResetFluxSpeed(int ind)
    {
        fluxSpeeds[ind] = Random.value * 0.2f + 0.025f;
        while (fluxTimers[ind] > 6.2831855f)
            fluxTimers[ind] -= 6.2831855f;
    }
    public void Spark()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 a = Custom.RNV();
            room.AddObject(new Spark(firstChunk.pos + a * Random.value * 20f, a * Mathf.Lerp(4f, 10f, Random.value), Colour.white, null, 4, 18));
        }
    }
    public void Zap()
    {
        if (AbstractTaser.electricCharge == 0)
            return;
        room.AddObject(new ZapCoil.ZapFlash(firstChunk.pos, 10f));
        room.PlaySound(SoundID.Zapper_Zap, firstChunk.pos, 1f, 1.5f + Random.value * 1.5f);
        if (Submersion > 0.5f)
            room.AddObject(new UnderwaterShock(room, null, firstChunk.pos, 10, 800f, 2f, thrownBy, new Colour(0.8f, 0.8f, 1f)));
    }
    #endregion
}