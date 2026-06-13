using UnityEngine;
using System.Collections.Generic;
using FluidVoxelSandbox.Core;

namespace FluidVoxelSandbox.Rendering
{
    public static class VoxelMeshBuilder
    {
        public struct MeshData
        {
            public List<Vector3> vertices;
            public List<int> triangles;
            public List<Color32> colors;
            public List<Vector2> uvs;

            public void Init()
            {
                vertices = new List<Vector3>(1024);
                triangles = new List<int>(2048);
                colors = new List<Color32>(1024);
                uvs = new List<Vector2>(1024);
            }

            public void Clear()
            {
                vertices.Clear();
                triangles.Clear();
                colors.Clear();
                uvs.Clear();
            }

            public void TrimExcess()
            {
                vertices.TrimExcess();
                triangles.TrimExcess();
                colors.TrimExcess();
                uvs.TrimExcess();
            }
        }

        private static readonly Vector3[] FaceVertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0)
        };

        private static readonly int[] FaceTriangles = new int[] { 0, 2, 1, 0, 3, 2 };

        private static readonly Vector2[] FaceUVs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        public static void BuildSolidMesh(VoxelMap map, MeshData meshData, int startX, int startY, int endX, int endY)
        {
            float voxelSize = map.VoxelSize;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    Voxel voxel = map.GetVoxel(x, y);
                    if (!voxel.IsSolid) continue;

                    AddVoxelFaces(map, x, y, voxel, meshData, voxelSize, true, true, true, true);
                }
            }
        }

        public static void BuildLiquidMesh(VoxelMap map, MeshData meshData, int startX, int startY, int endX, int endY, float time)
        {
            float voxelSize = map.VoxelSize;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    Voxel voxel = map.GetVoxel(x, y);
                    if (!voxel.IsLiquid) continue;

                    float heightOffset = CalculateLiquidHeightOffset(map, x, y, time);
                    Color32 color = VoxelProperties.GetColor(voxel.Type, voxel.Alpha);
                    color.a = 180;

                    AddVoxelFaces(map, x, y, voxel, meshData, voxelSize, true, true, true, true, heightOffset, color);
                }
            }
        }

        public static void BuildGasMesh(VoxelMap map, MeshData meshData, int startX, int startY, int endX, int endY, float time)
        {
            float voxelSize = map.VoxelSize;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    Voxel voxel = map.GetVoxel(x, y);
                    if (!voxel.IsGas) continue;

                    float pulseOffset = Mathf.Sin(time * 2f + x * 0.3f + y * 0.2f) * 0.05f;
                    Color32 color = VoxelProperties.GetColor(voxel.Type, voxel.Alpha);

                    AddVoxelFaces(map, x, y, voxel, meshData, voxelSize, false, false, false, false, pulseOffset, color);
                }
            }
        }

        public static void BuildDebugBorderMesh(VoxelMap map, MeshData meshData)
        {
            float voxelSize = map.VoxelSize;
            Vector3 offset = Vector3.zero;

            AddQuad(meshData, new Vector3(0, 0, 0), new Vector3(map.Width * voxelSize, 0, 0),
                    new Vector3(map.Width * voxelSize, map.Height * voxelSize, 0), new Vector3(0, map.Height * voxelSize, 0),
                    new Color32(50, 50, 50, 60));
        }

        private static void AddVoxelFaces(VoxelMap map, int x, int y, Voxel voxel,
            MeshData meshData, float voxelSize,
            bool checkLeft, bool checkRight, bool checkTop, bool checkBottom,
            float heightOffset = 0f, Color32? customColor = null)
        {
            Color32 color = customColor ?? VoxelProperties.GetColor(voxel.Type, voxel.Alpha);
            Vector3 offset = new Vector3(x * voxelSize, y * voxelSize, 0);

            bool hasLeft = x > 0 && IsSameCategory(map.GetVoxel(x - 1, y), voxel);
            bool hasRight = x < map.Width - 1 && IsSameCategory(map.GetVoxel(x + 1, y), voxel);
            bool hasBottom = y > 0 && IsSameCategory(map.GetVoxel(x, y - 1), voxel);
            bool hasTop = y < map.Height - 1 && IsSameCategory(map.GetVoxel(x, y + 1), voxel);

            if (voxel.IsSolid)
            {
                if (!hasLeft) AddEdgeFace(meshData, offset, voxelSize, -1, color);
                if (!hasRight) AddEdgeFace(meshData, offset, voxelSize, 1, color);
                if (!hasBottom) AddBottomFace(meshData, offset, voxelSize, color);
                if (!hasTop) AddTopFace(meshData, offset, voxelSize, color, heightOffset);
                AddCenterFace(meshData, offset, voxelSize, color, heightOffset);
            }
            else
            {
                AddCenterFace(meshData, offset, voxelSize, color, heightOffset);
            }
        }

        private static bool IsSameCategory(Voxel a, Voxel b)
        {
            if (a.IsEmpty && b.IsEmpty) return true;
            if (a.IsSolid && b.IsSolid) return true;
            if (a.IsLiquid && b.IsLiquid) return true;
            if (a.IsGas && b.IsGas) return true;
            return false;
        }

        private static void AddQuad(MeshData meshData, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color32 color)
        {
            int startIdx = meshData.vertices.Count;

            meshData.vertices.Add(v0);
            meshData.vertices.Add(v1);
            meshData.vertices.Add(v2);
            meshData.vertices.Add(v3);

            meshData.triangles.Add(startIdx);
            meshData.triangles.Add(startIdx + 2);
            meshData.triangles.Add(startIdx + 1);
            meshData.triangles.Add(startIdx);
            meshData.triangles.Add(startIdx + 3);
            meshData.triangles.Add(startIdx + 2);

            meshData.colors.Add(color);
            meshData.colors.Add(color);
            meshData.colors.Add(color);
            meshData.colors.Add(color);

            meshData.uvs.Add(FaceUVs[0]);
            meshData.uvs.Add(FaceUVs[1]);
            meshData.uvs.Add(FaceUVs[2]);
            meshData.uvs.Add(FaceUVs[3]);
        }

        private static void AddCenterFace(MeshData meshData, Vector3 offset, float size, Color32 color, float heightOffset)
        {
            Vector3 v0 = offset + new Vector3(0, 0, 0.01f);
            Vector3 v1 = offset + new Vector3(size, 0, 0.01f);
            Vector3 v2 = offset + new Vector3(size, size + heightOffset, 0.01f);
            Vector3 v3 = offset + new Vector3(0, size + heightOffset, 0.01f);
            AddQuad(meshData, v0, v1, v2, v3, color);
        }

        private static void AddTopFace(MeshData meshData, Vector3 offset, float size, Color32 color, float heightOffset)
        {
            Vector3 v0 = offset + new Vector3(0, size + heightOffset, -0.01f);
            Vector3 v1 = offset + new Vector3(size, size + heightOffset, -0.01f);
            Vector3 v2 = offset + new Vector3(size, size, -0.05f);
            Vector3 v3 = offset + new Vector3(0, size, -0.05f);

            Color32 topColor = LightenColor(color, 15);
            AddQuad(meshData, v0, v1, v2, v3, topColor);
        }

        private static void AddBottomFace(MeshData meshData, Vector3 offset, float size, Color32 color)
        {
            Vector3 v0 = offset + new Vector3(0, 0, -0.05f);
            Vector3 v1 = offset + new Vector3(size, 0, -0.05f);
            Vector3 v2 = offset + new Vector3(size, 0, -0.01f);
            Vector3 v3 = offset + new Vector3(0, 0, -0.01f);

            Color32 botColor = DarkenColor(color, 25);
            AddQuad(meshData, v0, v1, v2, v3, botColor);
        }

        private static void AddEdgeFace(MeshData meshData, Vector3 offset, float size, int dir, Color32 color)
        {
            float edgeWidth = 0.05f * dir;
            Vector3 v0, v1, v2, v3;

            if (dir > 0)
            {
                v0 = offset + new Vector3(size, 0, -0.05f);
                v1 = offset + new Vector3(size + edgeWidth, 0, -0.01f);
                v2 = offset + new Vector3(size + edgeWidth, size, -0.01f);
                v3 = offset + new Vector3(size, size, -0.05f);
            }
            else
            {
                v0 = offset + new Vector3(0, 0, -0.01f);
                v1 = offset + new Vector3(edgeWidth, 0, -0.05f);
                v2 = offset + new Vector3(edgeWidth, size, -0.05f);
                v3 = offset + new Vector3(0, size, -0.01f);
            }

            Color32 edgeColor = DarkenColor(color, 15);
            AddQuad(meshData, v0, v1, v2, v3, edgeColor);
        }

        private static float CalculateLiquidHeightOffset(VoxelMap map, int x, int y, float time)
        {
            bool hasTop = y < map.Height - 1 && map.GetVoxel(x, y + 1).IsLiquid;
            if (hasTop) return 0f;

            float baseOffset = -0.15f;
            float wave = Mathf.Sin(time * 3f + x * 0.5f) * 0.03f;
            float neighborAvg = 0f;
            int count = 0;

            if (x > 0 && map.GetVoxel(x - 1, y).IsLiquid) { neighborAvg += -0.12f; count++; }
            if (x < map.Width - 1 && map.GetVoxel(x + 1, y).IsLiquid) { neighborAvg += -0.12f; count++; }

            if (count > 0) baseOffset = neighborAvg / count;

            return baseOffset + wave;
        }

        private static Color32 LightenColor(Color32 c, byte amount)
        {
            return new Color32(
                (byte)Mathf.Min(255, c.r + amount),
                (byte)Mathf.Min(255, c.g + amount),
                (byte)Mathf.Min(255, c.b + amount),
                c.a);
        }

        private static Color32 DarkenColor(Color32 c, byte amount)
        {
            return new Color32(
                (byte)Mathf.Max(0, c.r - amount),
                (byte)Mathf.Max(0, c.g - amount),
                (byte)Mathf.Max(0, c.b - amount),
                c.a);
        }

        public static Mesh CreateMeshFromData(MeshData data)
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            if (data.vertices.Count == 0)
            {
                mesh.vertices = new Vector3[0];
                mesh.triangles = new int[0];
                mesh.colors32 = new Color32[0];
                mesh.uv = new Vector2[0];
                return mesh;
            }

            mesh.SetVertices(data.vertices);
            mesh.SetTriangles(data.triangles, 0);
            mesh.SetColors(data.colors);
            mesh.SetUVs(0, data.uvs);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.Optimize();
            return mesh;
        }
    }
}
