using UnityEngine;
using System.IO;
using System.Collections.Generic;
using static JourneysStart.Outgrowth.PlayerStuff.OutgrowthData;
using RWCustom;
using Colour = UnityEngine.Color;

namespace JourneysStart.Shared.PlayerStuff.PlayerGraf;

public static class PlayerGrafMethods
{
    #region
    public static void TailTextureFilePath(ref Texture2D TailTexture, string fileName)
    {
        TailTexture = new Texture2D(150, 75, TextureFormat.ARGB32, false);
        string path = AssetManager.ResolveFilePath("textures/" + fileName + ".png");
        if (File.Exists(path))
        {
            byte[] data = File.ReadAllBytes(path);
            TailTexture.LoadImage(data);
        }
    }
    public static void UVMapTail(Player self, RoomCamera.SpriteLeaser sLeaser)
    {
        if (!Plugin.PlayerDataCWT.TryGetValue(self, out PlayerData playerData))
            return;

        if (!(sLeaser.sprites[2] is TriangleMesh tail && playerData.tailPattern.TailAtlas.elements?.Count > 0))
            return;

        tail.element = playerData.tailPattern.TailAtlas.elements[0];
        for (int i = tail.vertices.Length - 1; i >= 0; i--)
        {
            float perc = i / 2 / (float)(tail.vertices.Length / 2);

            Vector2 uv;
            if (i % 2 == 0)
                uv = new Vector2(perc, 0f);
            else if (i < tail.vertices.Length - 1)
                uv = new Vector2(perc, 1f);
            else
                uv = new Vector2(1f, 0f);

            // Map UV values to the element
            uv.x = Mathf.Lerp(tail.element.uvBottomLeft.x, tail.element.uvTopRight.x, uv.x);
            uv.y = Mathf.Lerp(tail.element.uvBottomLeft.y, tail.element.uvTopRight.y, uv.y);

            tail.UVvertices[i] = uv;
        }
    }

    public static void AddNewSpritesToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, int index)
    {
        //makes it so body stripes dont go on top of every creature
        rCam.ReturnFContainer("Foreground").RemoveChild(sLeaser.sprites[index]);
        rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[index]);

        sLeaser.sprites[index].MoveBehindOtherNode(sLeaser.sprites[9]); //stripes and everything behind face
    }
    #endregion

    public static class RopeMethods
    {
        public static void MSCUpdate(PlayerGraphics self)
        {
            //if (!(Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData) && pData.IsSproutcat))
            //    return;

            if (Plugin.sproutcat != self.player.SlugCatClass)
                return;

            self.lastStretch = self.stretch;
            self.stretch = self.RopeStretchFac;

            List<Vector2> list = new();
            for (int j = self.player.tongue.rope.TotalPositions - 1; j > 0; j--)
            {
                list.Add(self.player.tongue.rope.GetPosition(j));
            }
            list.Add(self.player.mainBodyChunk.pos);
            float num = 0f;
            for (int k = 1; k < list.Count; k++)
            {
                num += Vector2.Distance(list[k - 1], list[k]);
            }
            float num2 = 0f;
            for (int l = 0; l < list.Count; l++)
            {
                if (l > 0)
                {
                    num2 += Vector2.Distance(list[l - 1], list[l]);
                }
                self.AlignRope(num2 / num, list[l]);
            }
            for (int i = 0; i < self.ropeSegments.Length; i++)
            {
                self.ropeSegments[i].Update();
                //IndexOutOfRangeException: Index was outside the bounds of the array
                //if i put ConnectRopeSegments here
            }
            for (int n = 1; n < self.ropeSegments.Length; n++)
            {
                self.ConnectRopeSegments(n, n - 1);
            }
            for (int num3 = 0; num3 < self.ropeSegments.Length; num3++)
            {
                self.ropeSegments[num3].claimedForBend = false;
            }
        }
        public static void AddToContainer(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser)
        {
            Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData pData);
            sLeaser.sprites[pData.Sproutcat.spriteIndexes[ROPE_INDEX]].MoveBehindOtherNode(sLeaser.sprites[0]); //move rope behind head
        }
        public static void DrawSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, float timeStacker, Vector2 camPos)
        {
            if (null == self.player.room)
                return;

            Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData playerData);

            int ropeIndex = playerData.Sproutcat.spriteIndexes[ROPE_INDEX];
            float b = Mathf.Lerp(self.lastStretch, self.stretch, timeStacker);
            Vector2 vector = Vector2.Lerp(self.ropeSegments[0].lastPos, self.ropeSegments[0].pos, timeStacker);
            Vector2 vector2;

            vector += Custom.DirVec(Vector2.Lerp(self.ropeSegments[1].lastPos, self.ropeSegments[1].pos, timeStacker), vector) * 1f;
            float num7 = 0f;
            for (int k = 1; k < self.ropeSegments.Length; k++)
            {
                float num8 = k / (self.ropeSegments.Length - 1);
                if (k >= self.ropeSegments.Length - 2)
                {
                    vector2 = new(sLeaser.sprites[9].x + camPos.x, sLeaser.sprites[9].y + camPos.y);
                }
                else
                {
                    vector2 = Vector2.Lerp(self.ropeSegments[k].lastPos, self.ropeSegments[k].pos, timeStacker);
                }
                Vector2 a2 = Custom.PerpendicularVector((vector - vector2).normalized);
                float d4 = 0.2f + 1.6f * Mathf.Lerp(1f, b, Mathf.Pow(Mathf.Sin(num8 * 3.1415927f), 0.7f));
                Vector2 vector11 = vector - a2 * d4;
                Vector2 vector12 = vector2 + a2 * d4;
                float num9 = Mathf.Sqrt(Mathf.Pow(vector11.x - vector12.x, 2f) + Mathf.Pow(vector11.y - vector12.y, 2f));
                if (!float.IsNaN(num9))
                {
                    num7 += num9;
                }
                //yeah you need all these below otherwise its invisible
                (sLeaser.sprites[ropeIndex] as TriangleMesh).MoveVertice((k - 1) * 4, vector11 - camPos);
                (sLeaser.sprites[ropeIndex] as TriangleMesh).MoveVertice((k - 1) * 4 + 1, vector + a2 * d4 - camPos);
                (sLeaser.sprites[ropeIndex] as TriangleMesh).MoveVertice((k - 1) * 4 + 2, vector2 - a2 * d4 - camPos);
                (sLeaser.sprites[ropeIndex] as TriangleMesh).MoveVertice((k - 1) * 4 + 3, vector12 - camPos);
                vector = vector2;
            }
            if (self.player.tongue.Free || self.player.tongue.Attached)
            {
                sLeaser.sprites[ropeIndex].isVisible = true;
            }
            else
            {
                sLeaser.sprites[ropeIndex].isVisible = false;
            }
        }

        public static void ApplyPalette(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, Colour colour)
        {
            Plugin.PlayerDataCWT.TryGetValue(self.player, out PlayerData playerData);

            int ropeIndex = playerData.Sproutcat.spriteIndexes[ROPE_INDEX];
            for (int i = 0; i < (sLeaser.sprites[ropeIndex] as TriangleMesh).verticeColors.Length; i++)
            {
                (sLeaser.sprites[ropeIndex] as TriangleMesh).verticeColors[i] = colour;
            }
        }
    }
}
