using RWCustom;
using System;
using Debug = UnityEngine.Debug;
using Mathf = UnityEngine.Mathf;
using Vector2 = UnityEngine.Vector2;

namespace JourneysStart.Slugcats.Outgrowth.PlayerStuff.PlayerGraf;

public class CheekFluff
{
    public WeakReference<Player> playerRef;

    public const string spriteName = "LizardScaleA2";

    public readonly int startIndex;
    public readonly int endIndex;
    public readonly int lowerFluffIndex;

    public Vector2[] scalePos;
    public PlayerGraphics.AxolotlScale[] scales;

    public CheekFluff(PlayerGraphics pGraf, int startIndex = 13)
    {
        playerRef = new WeakReference<Player>(pGraf.player);
        this.startIndex = startIndex;

        scalePos = new Vector2[4];
        scales = new PlayerGraphics.AxolotlScale[scalePos.Length];

        for (int i = 0; i < scales.Length; i++)
        {
            scales[i] = new PlayerGraphics.AxolotlScale(pGraf);
            scalePos[i] = new Vector2(i < scales.Length / 2 ? 0.7f : -0.7f, 0.28f);
        }

        endIndex = startIndex + scalePos.Length;
        lowerFluffIndex = startIndex + scalePos.Length / 2;
    }

    public void Update()
    {
        if (!playerRef.TryGetTarget(out Player player))
            return;

        for (int i = startIndex; i < endIndex; i++)
        {
            int index = i - startIndex;
            Vector2 pos = player.bodyChunks[0].pos;
            Vector2 pos2 = player.bodyChunks[1].pos;
            int num3 = index % (scales.Length / 2);
            float num4 = 200f / (scales.Length / 2);

            if (i % 2 != 0)
            {
                pos.x += 2f;
            }
            else
            {
                pos.x -= 2f;
            }

            Vector2 a = Custom.rotateVectorDeg(Custom.DegToVec(0f), num3 * num4 - 90f / 92f);
            Vector2 vector = Custom.rotateVectorDeg(Custom.DegToVec(0f), num3 * num4 - 90f / 2f);
            //note: Custom.DegToVec(0f) = (sin0, cos0) = (0, 1)
            Vector2 a2 = Vector2.Lerp(vector, Custom.DirVec(pos2, pos), Custom.VecToDeg((player.graphicsModule as PlayerGraphics).lookDirection));

            if (scalePos[index].y < 0.2f)
            {
                a2 -= a * Mathf.Pow(Mathf.InverseLerp(0.2f, 0f, scalePos[index].y), 2f) * 2f;
            }
            a2 = Vector2.Lerp(a2, vector, Mathf.Pow(0.0875f, 1f)).normalized;
            Vector2 vector2 = pos + a2 * scales.Length;

            if (!Custom.DistLess(scales[index].pos, vector2, scales[index].length / 2f))
            {
                Vector2 a3 = Custom.DirVec(scales[index].pos, vector2);
                float num5 = Vector2.Distance(scales[index].pos, vector2);
                float num6 = scales[index].length / 2f;
                scales[index].pos += a3 * (num5 - num6);
                scales[index].vel += a3 * (num5 - num6);
            }

            scales[index].vel += Vector2.ClampMagnitude(vector2 - scales[index].pos, 10f) / Mathf.Lerp(5f, 1.5f, 0.5873646f);
            scales[index].vel *= Mathf.Lerp(1f, 0.8f, 0.5873646f);
            scales[index].ConnectToPoint(pos, scales[index].length, true, 0f, new Vector2(0f, 0f), 0f, 0f);

            scales[index].Update();
        }
    }

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser)
    {
        //Debug.Log($"{Plugin.MOD_NAME}: Initiating cheek fluff sprites");
        float whiskerScaleY = 10f / Futile.atlasManager.GetElementWithName(spriteName).sourcePixelSize.y;
        for (int i = startIndex; i < endIndex; i++)
        {
            //Debug.Log($"\tCreating new sprite at index {i}");
            sLeaser.sprites[i] = new(spriteName)
            {
                scaleX = 1f,
                scaleY = whiskerScaleY,
                anchorY = 0.1f
            };
        }
    }
    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, FContainer container)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            container.AddChild(sLeaser.sprites[i]);
            sLeaser.sprites[i].MoveBehindOtherNode(sLeaser.sprites[9]); //move behind face
        }
    }
    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, float timeStacker, Vector2 camPos)
    {
        if (!playerRef.TryGetTarget(out Player player))
            return;

        for (int i = startIndex; i < endIndex; i++)
        {
            Vector2 vector = new(sLeaser.sprites[9].x + camPos.x, sLeaser.sprites[9].y + camPos.y);
            vector.y += 1.5f;

            float xIncr = 2f;
            float rotationAngle = 160f; //higher = goes up

            if (i >= lowerFluffIndex)
            {
                xIncr -= 1.5f;
                rotationAngle -= 45f;
                vector.y -= 1f;
                //vector.x += 0.5f; //idk? try ig
            }

            if (i % 2 != 0) //left side/blue
            {
                vector.x -= xIncr;
            }
            else //right side/red
            {
                rotationAngle *= -1;
                vector.x += xIncr;
            }

            if (Plugin.Sprout_Debug_CheekFluffColours.TryGet(player, out bool yheah) && yheah)
            {
                if (i >= lowerFluffIndex)
                    sLeaser.sprites[i].color = UnityEngine.Color.gray;
                else if (i % 2 == 0)
                    sLeaser.sprites[i].color = UnityEngine.Color.red;
                else
                    sLeaser.sprites[i].color = UnityEngine.Color.blue;
            }
            else
                sLeaser.sprites[i].color = sLeaser.sprites[1].color; //actual colour

            int index = i - startIndex; //for the scale array

            float rotation = Custom.AimFromOneVectorToAnother(vector, Vector2.Lerp(scales[index].lastPos, scales[index].pos, timeStacker)) + rotationAngle;
            if (i % 2 != 0) //clamp so he doesnt go bald when hanging around
                rotation = Mathf.Clamp(rotation, 90f, 360f);
            else
                rotation = Mathf.Clamp(rotation, -360f, 90f);

            sLeaser.sprites[i].x = vector.x - camPos.x;
            sLeaser.sprites[i].y = vector.y - camPos.y;
            sLeaser.sprites[i].rotation = rotation;
        }
    }
    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            sLeaser.sprites[i].color = sLeaser.sprites[1].color;
        }
    }
}