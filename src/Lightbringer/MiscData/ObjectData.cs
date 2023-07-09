﻿using MoreSlugcats;
using System;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Mathf = UnityEngine.Mathf;
using Custom = RWCustom.Custom;

namespace JourneysStart.Lightbringer.Data
{
    public class Flare : FlareBomb
    {
        public WeakReference<Player> playerRef;
        public Flare(Player player, AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject, player.room.world)
        {
            playerRef = new WeakReference<Player>(player);
            thrownBy = player; //sets kill tag
        }
        public override void Update(bool eu)
        {
            if (!playerRef.TryGetTarget(out Player player))
            {
                Destroy();
                return;
            }

            base.Update(eu);
            firstChunk.pos = player.bodyChunks[1].pos;

            foreach (AbstractCreature crit in room.abstractRoom.creatures)
            {
                if (crit == null || crit.realizedCreature == null)
                    continue;

                if (crit.realizedCreature == player)
                    continue;

                if (crit.realizedCreature is Player && !Custom.rainWorld.options.friendlyFire)
                    continue;

                if (!(Custom.DistLess(firstChunk.pos, crit.realizedCreature.mainBodyChunk.pos, LightIntensity * 600f)
                    || (Custom.DistLess(firstChunk.pos, crit.realizedCreature.mainBodyChunk.pos, LightIntensity * 1600f)
                    && room.VisualContact(firstChunk.pos, crit.realizedCreature.mainBodyChunk.pos))))
                {
                    continue;
                }

                if (crit.realizedCreature.grasps != null)
                {
                    for (int i = 0; i < crit.realizedCreature.grasps.Length; i++)
                    {
                        if (crit.realizedCreature.grasps[i]?.grabbed is Player playerBeingHeld && playerBeingHeld == player)
                        {
                            crit.realizedCreature.ReleaseGrasp(i); //drop player
                            player.room.AddObject(new CreatureSpasmer(crit.realizedCreature, false, 40));
                            break;
                        }
                    }
                }

                crit.realizedCreature.Stun(Random.Range(20, 30));
            }
        }
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i != sLeaser.sprites.Length - 1)
                    sLeaser.sprites[i].isVisible = false;
            } //falsing sprites[2].isVisible makes the entire light dim
        }
    }

    public class GlowingSlimeMold : SlimeMold, IProvideWarmth
    {
        //surgery-ing lantern's lightsource onto slime mold
        public LightSource lightSource;
        public float[,] flicker;
        public GlowingSlimeMold(AbstractPhysicalObject abstr) : base(abstr)
        {
            flicker = new float[2, 3];
            for (int i = 0; i < flicker.GetLength(0); i++)
            {
                flicker[i, 0] = 1f;
                flicker[i, 1] = 1f;
                flicker[i, 2] = 1f;
            }
        }
        public override void Update(bool eu)
        {
            base.Update(eu);

            for (int i = 0; i < flicker.GetLength(0); i++)
            {
                flicker[i, 1] = flicker[i, 0];
                flicker[i, 0] += Mathf.Pow(Random.value, 3f) * 0.1f * ((Random.value < 0.5f) ? -1f : 1f);
                flicker[i, 0] = Custom.LerpAndTick(flicker[i, 0], flicker[i, 2], 0.05f, 0.033333335f);
                if (Random.value < 0.2f)
                {
                    flicker[i, 2] = 1f + Mathf.Pow(Random.value, 3f) * 0.2f * ((Random.value < 0.5f) ? -1f : 1f);
                }
                flicker[i, 2] = Mathf.Lerp(flicker[i, 2], 1f, 0.01f);
            }
            if (lightSource == null)
            {
                lightSource = new(firstChunk.pos, false, Custom.hexToColor("D65B19"), this)
                {
                    affectedByPaletteDarkness = 0.5f
                };
                room.AddObject(lightSource);
            }
            else
            {
                lightSource.setPos = firstChunk.pos;
                lightSource.setRad = 250f * flicker[0, 0];
                lightSource.setAlpha = 1f;
                if (lightSource.slatedForDeletetion || lightSource.room != room)
                {
                    lightSource = null;
                }
            }
        }
        public Room loadedRoom { get { return room; } }
        public float warmth { get { return RainWorldGame.DefaultHeatSourceWarmth - (RainWorldGame.DefaultHeatSourceWarmth * 0.1f); } }
        public float range { get { return 150f; } }
        public Vector2 Position() { return firstChunk.pos; }
    }

    public class ResilientFlarebomb : FlareBomb
    {
        public const int timeTillExplodeValid = 60 * 40;
        public int timer = 0;
        public ResilientFlarebomb(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject, abstractPhysicalObject.world) { }
        public override void Update(bool eu)
        {
            base.Update(eu);
            if (timer <= timeTillExplodeValid)
                timer++;
        }
        public override void HitByExplosion(float hitFac, Explosion explosion, int hitChunk)
        {
            if (timer > timeTillExplodeValid)
                base.HitByExplosion(hitFac, explosion, hitChunk);
        }
    }
}
