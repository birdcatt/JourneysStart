using MonoMod.Cil;
using Mono.Cecil.Cil;
using JourneysStart.Shared.PlayerStuff;
using UnityEngine;
using System;
using RWCustom;
using Random = UnityEngine.Random;
using System.Collections.Generic;

namespace JourneysStart.Slugcats.Lightbringer.PlayerStuff;

public static class ElecResist
{
    public static void Hook()
    {
        On.ZapCoil.Update += ZapCoil_Update;
        IL.Player.Die += Player_Die;
        IL.Centipede.Shock += Centipede_Shock;
    }

    public static void ZapCoil_Update(On.ZapCoil.orig_Update orig, ZapCoil self, bool eu)
    {
        foreach (List<PhysicalObject> physObjList in self.room.physicalObjects)
        {
            foreach (PhysicalObject physObj in physObjList)
            {
                if (!(physObj is Player player && Plugin.PlayerDataCWT.TryGetValue(player, out PlayerData pData)
                    && pData.IsLightpup && pData.Lightpup.flareCharge > 0))
                    continue;

                foreach (BodyChunk bodyChunk in physObj.bodyChunks)
                {
                    if (self.horizontalAlignment && bodyChunk.ContactPoint.y != 0
                        || !self.horizontalAlignment && bodyChunk.ContactPoint.x != 0)
                    {
                        pData.Lightpup.hitByZapcoil = true;
                        break;
                    }
                }
            }
        }
        orig(self, eu); //calls Creature.Die in here
    }
    public static void Player_Die(ILContext il)
    {
        //another place where i need to not call orig
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player self) =>
        {
            return Plugin.PlayerDataCWT.TryGetValue(self, out PlayerData pData) && pData.IsLightpup && pData.Lightpup.hitByZapcoil;
        });
        c.Emit(OpCodes.Brfalse, label);

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Player self) =>
        {
            Plugin.PlayerDataCWT.TryGetValue(self, out PlayerData pData);

            pData.Lightpup.hitByZapcoil = false;
            pData.Lightpup.RemoveFlareCharge();
            self.room.PlaySound(SoundID.Fire_Spear_Pop, self.firstChunk.pos);
            (self.graphicsModule as PlayerGraphics).blink = 0;
            self.Blink(90);
            //dont stun, player still needs movement to not crash into a 2nd zapcoil in 0g
        });
        c.Emit(OpCodes.Ret);

        c.MarkLabel(label);
    }

    public static void Centipede_Shock(ILContext il)
    {
        //another instance of i need to not call orig and i also need to go in the middle too
        ILCursor c = new(il);
        ILLabel label = il.DefineLabel();

        c.GotoNext(MoveType.Before, i => i.MatchLdarg(0), i => i.MatchCallOrCallvirt<Centipede>("get_AquaCenti"), i => i.Match(OpCodes.Brfalse));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.EmitDelegate((Centipede self, PhysicalObject shockObj) =>
        {
            return !self.Small
            && shockObj is Player p && Plugin.PlayerDataCWT.TryGetValue(p, out PlayerData pData)
            && pData.IsLightpup && pData.Lightpup.flareCharge > 0;
            //lightpup has flarecharge remaining to take a hit & its not a small centi
        });
        c.Emit(OpCodes.Brfalse, label);

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.EmitDelegate((Centipede self, PhysicalObject shockObj) =>
        {
            Player player = shockObj as Player;
            Plugin.PlayerDataCWT.TryGetValue(player, out PlayerData pData);

            pData.Lightpup.RemoveFlareCharge();
            Utility.AddCraftingLight(player);
            self.room.PlaySound(SoundID.Zapper_Zap, player.firstChunk.pos, 1f, 1.5f + Random.value * 1.5f);
            self.room.PlaySound(SoundID.Fire_Spear_Pop, player.firstChunk.pos);

            if (shockObj.Submersion > 0f)
            {
                if (self.AquaCenti)
                {
                    self.room.AddObject(new UnderwaterShock(self.room, self, self.HeadChunk.pos, 14, 80f, 1f, self, new Color(0.7f, 0.7f, 1f)));
                }
                else
                    self.room.AddObject(new UnderwaterShock(self.room, self, self.HeadChunk.pos, 14, Mathf.Lerp(ModManager.MMF ? 0f : 200f, 1200f, self.size), 0.2f + 1.9f * self.size, self, new Color(0.7f, 0.7f, 1f)));
            }

            if (!self.AquaCenti)
            {
                if (shockObj.TotalMass < self.TotalMass)
                {
                    player.Stun(120);
                }
                else
                {
                    player.Stun((int)Custom.LerpMap(shockObj.TotalMass, 0f, self.TotalMass * 2f, 300f, 30f));
                    self.room.AddObject(new CreatureSpasmer(player, false, player.stun));
                    player.LoseAllGrasps();
                    self.Stun(6);
                    self.shockGiveUpCounter = Math.Max(self.shockGiveUpCounter, 30);
                    self.AI.annoyingCollisions = Math.Min(self.AI.annoyingCollisions / 2, 150);
                }
            }

            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i]?.grabbed is Player playerInThisLoop && playerInThisLoop == shockObj)
                {
                    self.ReleaseGrasp(i); //drop player
                    break;
                }
            }
            self.Stun(90);
        });
        c.Emit(OpCodes.Ret); //ok get outta here, no more orig

        c.MarkLabel(label);
    }
}
