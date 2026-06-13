using UnityEngine;
using FluidVoxelSandbox.Core;

namespace FluidVoxelSandbox.Terrain
{
    public class TerrainGenerator : MonoBehaviour
    {
        public enum TerrainType
        {
            Flat,
            Hills,
            Mountains,
            Islands,
            Canyons
        }

        [Header("Terrain Settings")]
        public TerrainType terrainType = TerrainType.Hills;
        public int terrainSeed = 12345;
        public float terrainScale = 0.02f;
        public int baseHeight = 64;
        public int amplitude = 48;
        public int octaves = 4;
        public float persistence = 0.5f;
        public float lacunarity = 2.0f;

        [Header("Layer Settings")]
        public int dirtThickness = 4;
        public int stoneStartDepth = 8;
        public int sandLevel = 72;

        [Header("Water")]
        public int waterLevel = 80;
        public bool generateWater = true;

        [Header("Caves")]
        public bool generateCaves = true;
        public float caveScale = 0.035f;
        public float caveThreshold = 0.5f;
        public int caveStartY = 20;

        [Header("Trees")]
        public bool generateTrees = true;
        public float treeDensity = 0.015f;
        public int treeMinHeight = 3;
        public int treeMaxHeight = 6;
        public int leavesRadius = 2;

        private System.Random _random;

        public void Generate(VoxelMap map)
        {
            _random = new System.Random(terrainSeed);
            float offsetX = (float)_random.NextDouble() * 10000f;
            float offsetY = (float)_random.NextDouble() * 10000f;

            int[] heightMap = GenerateHeightMap(map.Width, offsetX);
            int[,] caveMap = generateCaves ? GenerateCaveMap(map.Width, map.Height, offsetX, offsetY) : null;

            for (int x = 0; x < map.Width; x++)
            {
                int surfaceY = heightMap[x];

                for (int y = 0; y < map.Height; y++)
                {
                    if (generateCaves && y <= surfaceY && y >= caveStartY)
                    {
                        if (caveMap != null && caveMap[x, y] > caveThreshold)
                        {
                            continue;
                        }
                    }

                    if (y > surfaceY)
                    {
                        if (generateWater && y <= waterLevel)
                        {
                            map.SetVoxel(x, y, VoxelType.Water, false);
                        }
                        continue;
                    }

                    int depth = surfaceY - y;

                    if (depth == 0)
                    {
                        if (y <= sandLevel)
                            map.SetVoxel(x, y, VoxelType.Sand, false);
                        else
                            map.SetVoxel(x, y, VoxelType.Grass, false);
                    }
                    else if (depth <= dirtThickness)
                    {
                        if (y <= sandLevel + 2)
                            map.SetVoxel(x, y, VoxelType.Sand, false);
                        else
                            map.SetVoxel(x, y, VoxelType.Dirt, false);
                    }
                    else if (depth <= stoneStartDepth)
                    {
                        map.SetVoxel(x, y, VoxelType.Dirt, false);
                    }
                    else
                    {
                        map.SetVoxel(x, y, VoxelType.Stone, false);
                    }
                }
            }

            if (generateTrees)
            {
                GenerateTrees(map, heightMap);
            }
        }

        private int[] GenerateHeightMap(int width, float offsetX)
        {
            int[] heightMap = new int[width];

            for (int x = 0; x < width; x++)
            {
                float heightValue = 0f;
                float amplitude = this.amplitude;
                float frequency = terrainScale;
                float maxAmplitude = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float sampleX = (x + offsetX) * frequency;
                    float noiseValue = Mathf.PerlinNoise(sampleX, 0f);

                    switch (terrainType)
                    {
                        case TerrainType.Flat:
                            noiseValue = 0.5f + (noiseValue - 0.5f) * 0.2f;
                            break;
                        case TerrainType.Hills:
                            noiseValue = Mathf.Pow(noiseValue, 1.2f);
                            break;
                        case TerrainType.Mountains:
                            noiseValue = Mathf.Pow(noiseValue, 0.7f);
                            break;
                        case TerrainType.Islands:
                            float dist = Mathf.Abs(x - width * 0.5f) / (width * 0.5f);
                            float falloff = Mathf.SmoothStep(1f, 0f, dist);
                            noiseValue = noiseValue * falloff;
                            break;
                        case TerrainType.Canyons:
                            float valleyNoise = Mathf.PerlinNoise(sampleX * 0.5f, 50f);
                            noiseValue = noiseValue * (1f - valleyNoise * 0.7f);
                            break;
                    }

                    heightValue += noiseValue * amplitude;
                    maxAmplitude += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                heightValue = (heightValue / maxAmplitude) * this.amplitude;
                heightMap[x] = Mathf.Clamp(baseHeight + Mathf.FloorToInt(heightValue), 0, 250);
            }

            return heightMap;
        }

        private int[,] GenerateCaveMap(int width, int height, float offsetX, float offsetY)
        {
            int[,] caveMap = new int[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float sampleX = (x + offsetX) * caveScale;
                    float sampleY = (y + offsetY) * caveScale;

                    float noise1 = Mathf.PerlinNoise(sampleX, sampleY);
                    float noise2 = Mathf.PerlinNoise(sampleX * 2f + 100f, sampleY * 2f + 100f);
                    float noise3 = Mathf.PerlinNoise(sampleX * 0.5f - 50f, sampleY * 0.5f - 50f);

                    float combined = (noise1 + noise2 * 0.5f + noise3 * 0.25f) / 1.75f;
                    caveMap[x, y] = (int)(combined * 255);
                }
            }

            return caveMap;
        }

        private void GenerateTrees(VoxelMap map, int[] heightMap)
        {
            for (int x = 5; x < map.Width - 5; x += 2)
            {
                if (_random.NextDouble() > treeDensity * 100f)
                    continue;

                int surfaceY = heightMap[x];

                if (surfaceY <= sandLevel + 1)
                    continue;
                if (surfaceY >= waterLevel)
                    continue;
                if (map.GetVoxelType(x, surfaceY) != VoxelType.Grass)
                    continue;

                int treeHeight = _random.Next(treeMinHeight, treeMaxHeight + 1);
                int trunkBase = surfaceY + 1;
                int trunkTop = trunkBase + treeHeight - 1;

                if (trunkTop >= map.Height) continue;

                bool canPlace = true;
                for (int ty = trunkBase; ty <= trunkTop; ty++)
                {
                    if (!map.GetVoxel(x, ty).IsEmpty && map.GetVoxelType(x, ty) != VoxelType.Water)
                    {
                        canPlace = false;
                        break;
                    }
                }
                if (!canPlace) continue;

                for (int ty = trunkBase; ty <= trunkTop; ty++)
                {
                    map.SetVoxel(x, ty, VoxelType.Dirt, false);
                }

                for (int dy = -leavesRadius; dy <= leavesRadius; dy++)
                {
                    for (int dx = -leavesRadius; dx <= leavesRadius; dx++)
                    {
                        int lx = x + dx;
                        int ly = trunkTop + dy;

                        if (!map.InBounds(lx, ly)) continue;
                        if (!map.GetVoxel(lx, ly).IsEmpty) continue;

                        int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                        if (dist > leavesRadius + 1) continue;
                        if (dist > leavesRadius && _random.NextDouble() > 0.3f) continue;

                        map.SetVoxel(lx, ly, VoxelType.Grass, false);
                    }
                }
            }
        }

        public void RandomizeSeed()
        {
            terrainSeed = Random.Range(int.MinValue, int.MaxValue);
        }
    }
}
