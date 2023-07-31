using RWCustom;
using Vector2 = UnityEngine.Vector2;
using MoreSlugcats;
using Debug = UnityEngine.Debug;
using UnityEngine;
using static JourneysStart.Slugcats.Outgrowth.PlayerStuff.OutgrowthData;

namespace JourneysStart.Slugcats.Outgrowth.Rope;

public static class VineCombat
{
    //public static readonly Player.Tongue.Mode CombatHit = new("SproutcatVineCombatHit", true);
    public static readonly Player.Tongue.Mode CombatShootingOut = new("SproutcatVineCombatShootingOut", true);
    public static readonly Player.Tongue.Mode CombatWaveInAir = new("SproutcatVineCombatWaveInAir", true);

    public static void Hook()
    {
        //also hook to Player.ThrowToGetFree
        On.Player.Tongue.Update += Tongue_Update;
    }

    #region not hooks
    public static Vector2 FindNearestCreaturePos(Player self)
    {
        if (self.room == null)
            return Vector2.zero;

        if (!Plugin.PlayerDataCWT.TryGetValue(self, out var cwt) && cwt.IsSproutcat)
            return Vector2.zero;

        Vector2 nearestVector = Vector2.zero;

        foreach (AbstractCreature abstrCrit in self.room.abstractRoom.creatures)
        {
            if (Custom.DistLess(self.abstractCreature.pos, abstrCrit.pos, 60f)
                && nearestVector.magnitude > (self.abstractCreature.pos.Tile.ToVector2() - abstrCrit.pos.Tile.ToVector2()).magnitude)
            {
                nearestVector = self.abstractCreature.pos.Tile.ToVector2() - abstrCrit.pos.Tile.ToVector2();
                cwt.Sproutcat.foundNearestCreature = true;
            }
        }

        return nearestVector;
    }
    public static void CombatShoot(this Player.Tongue self, Vector2 dir)
    {
        if (Plugin.sproutcat != self.player.slugcatStats.name)
            return;

        self.resetRopeLength();

        if (ModManager.Expedition && self.player.room.game.rainWorld.ExpeditionMode
            && Expedition.ExpeditionGame.activeUnlocks.Contains("unl-explosivejump") && (self.player.input[0].pckp || self.player.input[1].pckp))
        {
            return;
        }

        if (self.Attached || self.mode != Player.Tongue.Mode.Retracted)
            return;

        self.mode = CombatShootingOut;
        //also play sound

        self.pos = self.baseChunk.pos + dir * 5f;
        self.vel = dir * 70f;
        self.elastic = 1f;
        self.requestedRopeLength = 140f;
        self.returning = false;

        Debug.Log($"{Plugin.MOD_NAME}: (VineCombat) Shooting vine");
    }
    public static void CombatHitCreature(this Player.Tongue self, SharedPhysics.CollisionResult result)
    {
        Creature crit = result.chunk.owner as Creature;

        if (crit.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.GarbageWorm
            || crit.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Fly
            || crit.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Overseer)
        {
            return;
        }

        self.pos = result.chunk.pos;
        //self.mode = CombatHit;
        //play sound
        self.attachedChunk = result.chunk;
        self.Attatch(); //who misspelled attach

        crit.Violence(self.baseChunk, new Vector2?(self.vel * self.player.mainBodyChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, 1.25f, 20f);
        //1.25f from Player.ThrownSpear, rest from Spear.HitSomething

        Debug.Log($"{Plugin.MOD_NAME}: (VineCombat) Hit creature {crit.abstractCreature.creatureTemplate.type}");
    }
    #endregion

    private static void Tongue_Update(On.Player.Tongue.orig_Update orig, Player.Tongue self)
    {
        orig(self);

        if (Plugin.PlayerDataCWT.TryGetValue(self.player, out var cwt) && cwt.IsSproutcat)
        {
            if (self.mode == CombatWaveInAir)
            {
                //self.Elasticity();
                cwt.Sproutcat.vineInAir++;
                if (self.Attached && self.attachedTime > VINE_IN_AIR_MAX || self.attachedChunk == null && cwt.Sproutcat.vineInAir > VINE_IN_AIR_MAX)
                {
                    //attached briefly for attacking crits
                    cwt.Sproutcat.vineInAir = 0;
                    self.Release();
                }
            }
            else if (self.mode == CombatShootingOut)
            {
                self.requestedRopeLength = Mathf.Max(0f, self.requestedRopeLength - 4f);

                if (cwt.Sproutcat.foundNearestCreature)
                {
                    Vector2 vector2 = self.pos + self.vel;
                    SharedPhysics.CollisionResult collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(null, self.player.room, self.pos, ref vector2, 1f, 1, self.baseChunk.owner, false);

                    if (collisionResult.chunk != null)
                    {
                        self.CombatHitCreature(collisionResult);
                    }
                }
                else
                {
                    self.mode = CombatWaveInAir;
                }

                cwt.Sproutcat.foundNearestCreature = false;
            }

        }
    }
}
