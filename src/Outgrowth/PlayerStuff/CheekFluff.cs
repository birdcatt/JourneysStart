using Vector2 = UnityEngine.Vector2;
using Mathf = UnityEngine.Mathf;
using Debug = UnityEngine.Debug;
using RWCustom;
using System;

namespace JourneysStart.Outgrowth.PlayerStuff;

public class CheekFluff
{
    public WeakReference<Player> playerRef;

    public string spriteName = "LizardScaleA0";

    public readonly int startIndex;
    public readonly int endIndex;

    public Vector2[] scalePos;
    public PlayerGraphics.AxolotlScale[] scales;

    public CheekFluff(PlayerGraphics pGraf, int startIndex = 13)
    {
        playerRef = new WeakReference<Player>(pGraf.player);
        this.startIndex = startIndex;

        scalePos = new Vector2[6];
        scales = new PlayerGraphics.AxolotlScale[scalePos.Length];

        for (int i = 0; i < scales.Length; i++)
        {
            scales[i] = new PlayerGraphics.AxolotlScale(pGraf);
            scalePos[i] = new Vector2(i < scales.Length / 2 ? 0.7f : -0.7f, 0.28f);
        }

        endIndex = startIndex + scalePos.Length;
    }

    public void Update()
    {
        if (!playerRef.TryGetTarget(out Player player))
            return;

        PlayerGraphics pGraf = player.graphicsModule as PlayerGraphics;
        
        int index = 0;
        for (int i = startIndex; i < endIndex; i++)
        {
            Vector2 pos = player.bodyChunks[0].pos;
            Vector2 pos2 = player.bodyChunks[1].pos;
            float num = 0f;
            float num2 = 90f;
            int num3 = index % (scales.Length / 2);
            float num4 = num2 / (scales.Length / 2);

            if (i % 2 == 0)
            {
                pos.x += 5f;
            }
            else
            {
                pos.x -= 5f;
            }

            Vector2 a = Custom.rotateVectorDeg(Custom.DegToVec(0f), num3 * num4 - num2 / 2f + num + 90f);
            float f = Custom.VecToDeg(pGraf.lookDirection);
            Vector2 vector = Custom.rotateVectorDeg(Custom.DegToVec(0f), num3 * num4 - num2 / 2f + num);
            Vector2 a2 = Vector2.Lerp(vector, Custom.DirVec(pos2, pos), Mathf.Abs(f));

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

            index++;
        }
    }

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser)
    {
        Debug.Log($"{Plugin.MOD_NAME}: Initiating cheek fluff sprites");
        for (int i = startIndex; i < endIndex; i++)
        {
            Debug.Log($"\tCreating new sprite at index {i}");
            sLeaser.sprites[i] = new(spriteName)
            {
                scaleX = 1f,
                scaleY = 5f / Futile.atlasManager.GetElementWithName(spriteName).sourcePixelSize.y,
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
        int index = 0;
        for (int i = startIndex; i < endIndex; i++)
        {
            Vector2 vector = new(sLeaser.sprites[9].x + camPos.x, sLeaser.sprites[9].y + camPos.y);
            float num = 0f;

            if (i % 2 == 0)
            {
                vector.x += 5f;
            }
            else
            {
                vector.x -= 5f;
            }

            if (i != 0)
                num = 180f;

            sLeaser.sprites[i].x = vector.x - camPos.x;
            sLeaser.sprites[i].y = vector.y - camPos.y;
            sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(vector, Vector2.Lerp(scales[index].lastPos, scales[index].pos, timeStacker)) + num;
            sLeaser.sprites[i].color = /*sLeaser.sprites[1].color*/ UnityEngine.Color.red;

            index++;
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