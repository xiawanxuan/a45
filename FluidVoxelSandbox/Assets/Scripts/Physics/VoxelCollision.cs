using UnityEngine;
using System.Collections.Generic;
using FluidVoxelSandbox.Core;

namespace FluidVoxelSandbox.Physics
{
    public class VoxelCollision : MonoBehaviour
    {
        private VoxelMap _map;

        [Header("Falling Solids")]
        public bool enableFallingSolids = true;
        public float fallDelay = 0.05f;
        public HashSet<VoxelType> fallingTypes = new HashSet<VoxelType>
        {
            VoxelType.Sand
        };

        [Header("Thermal Reactions")]
        public bool enableThermalReactions = true;
        public float lavaWaterContactChance = 0.3f;
        public float lavaHeatTransferRate = 200f;
        public float waterBoilingTemp = 100f;

        [Header("Dissolution")]
        public bool enableDissolution = true;
        public float waterDissolveDirtRate = 0.01f;

        private Dictionary<Vector2Int, float> _fallTimers = new Dictionary<Vector2Int, float>();

        public void Initialize(VoxelMap map)
        {
            _map = map;
        }

        public void CheckCollisions(float dt)
        {
            if (_map == null) return;

            ProcessFallingSolids(dt);

            if (enableThermalReactions)
            {
                ProcessThermalReactions(dt);
            }

            if (enableDissolution)
            {
                ProcessDissolution(dt);
            }
        }

        private void ProcessFallingSolids(float dt)
        {
            if (!enableFallingSolids) return;

            List<Vector2Int> toProcess = new List<Vector2Int>();

            for (int y = 1; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    Voxel v = _map.GetVoxel(x, y);
                    if (fallingTypes.Contains(v.Type))
                    {
                        toProcess.Add(new Vector2Int(x, y));
                    }
                }
            }

            for (int i = 0; i < toProcess.Count; i++)
            {
                Vector2Int pos = toProcess[i];
                Voxel voxel = _map.GetVoxel(pos.x, pos.y);
                if (!fallingTypes.Contains(voxel.Type)) continue;

                int belowY = pos.y - 1;
                if (!_map.InBounds(pos.x, belowY)) continue;

                Voxel below = _map.GetVoxel(pos.x, belowY);

                if (below.IsEmpty || below.IsLiquid)
                {
                    Vector2Int key = new Vector2Int(pos.x, pos.y);
                    float timer;
                    if (!_fallTimers.TryGetValue(key, out timer))
                    {
                        timer = 0f;
                    }

                    timer += dt;
                    if (timer >= fallDelay)
                    {
                        if (below.IsEmpty)
                        {
                            _map.SetVoxel(pos.x, belowY, voxel.Type, false);
                            _map.SetVoxel(pos.x, pos.y, VoxelType.Empty, false);
                        }
                        else
                        {
                            _map.SwapVoxels(pos.x, pos.y, pos.x, belowY, false);
                        }
                        _fallTimers.Remove(key);
                    }
                    else
                    {
                        _fallTimers[key] = timer;
                    }
                }
                else if (below.IsSolid && !fallingTypes.Contains(below.Type))
                {
                    TryFallDiagonal(pos.x, pos.y, voxel.Type, dt);
                }
            }
        }

        private void TryFallDiagonal(int x, int y, VoxelType type, float dt)
        {
            int[] dirs = Random.value > 0.5f ? new int[] { 1, -1 } : new int[] { -1, 1 };

            foreach (int dir in dirs)
            {
                int nx = x + dir;
                int ny = y - 1;
                if (!_map.InBounds(nx, ny)) continue;

                Voxel diag = _map.GetVoxel(nx, ny);
                Voxel side = _map.GetVoxel(nx, y);

                if ((diag.IsEmpty || diag.IsLiquid) && (side.IsEmpty || side.IsLiquid))
                {
                    Vector2Int key = new Vector2Int(x, y);
                    float timer;
                    if (!_fallTimers.TryGetValue(key, out timer))
                    {
                        timer = 0f;
                    }

                    timer += dt;
                    if (timer >= fallDelay * 1.5f)
                    {
                        if (diag.IsEmpty)
                        {
                            _map.SetVoxel(nx, ny, type, false);
                            _map.SetVoxel(x, y, VoxelType.Empty, false);
                        }
                        else
                        {
                            _map.SwapVoxels(x, y, nx, ny, false);
                        }
                        _fallTimers.Remove(key);
                        return;
                    }
                    else
                    {
                        _fallTimers[key] = timer;
                    }
                }
            }
        }

        private void ProcessThermalReactions(float dt)
        {
            for (int x = 0; x < _map.Width; x++)
            {
                for (int y = 0; y < _map.Height; y++)
                {
                    Voxel voxel = _map.GetVoxel(x, y);
                    if (voxel.Type != VoxelType.Lava) continue;

                    CheckLavaNeighbor(x, y, x + 1, y);
                    CheckLavaNeighbor(x, y, x - 1, y);
                    CheckLavaNeighbor(x, y, x, y + 1);
                    CheckLavaNeighbor(x, y, x, y - 1);
                }
            }
        }

        private void CheckLavaNeighbor(int lavaX, int lavaY, int nx, int ny)
        {
            if (!_map.InBounds(nx, ny)) return;

            Voxel neighbor = _map.GetVoxel(nx, ny);
            if (neighbor.Type == VoxelType.Water)
            {
                if (Random.value < lavaWaterContactChance)
                {
                    if (Random.value < 0.6f)
                    {
                        _map.SetVoxel(nx, ny, VoxelType.Stone, false);
                    }
                    else
                    {
                        _map.SetVoxel(nx, ny, VoxelType.Steam, false);
                        if (Random.value < 0.3f)
                        {
                            _map.SetVoxel(lavaX, lavaY, VoxelType.Stone, false);
                        }
                    }
                }
            }
            else if (neighbor.Type == VoxelType.Empty)
            {
                if (Random.value < 0.005f)
                {
                    _map.SetVoxel(nx, ny, VoxelType.Smoke, false);
                }
            }
        }

        private void ProcessDissolution(float dt)
        {
            for (int x = 0; x < _map.Width; x++)
            {
                for (int y = 0; y < _map.Height; y++)
                {
                    Voxel voxel = _map.GetVoxel(x, y);
                    if (voxel.Type != VoxelType.Water) continue;

                    if (Random.value < waterDissolveDirtRate * dt)
                    {
                        TryDissolveNeighbor(x, y, x + 1, y);
                        TryDissolveNeighbor(x, y, x - 1, y);
                        TryDissolveNeighbor(x, y, x, y + 1);
                        TryDissolveNeighbor(x, y, x, y - 1);
                    }
                }
            }
        }

        private void TryDissolveNeighbor(int x, int y, int nx, int ny)
        {
            if (!_map.InBounds(nx, ny)) return;
            Voxel neighbor = _map.GetVoxel(nx, ny);
            if (neighbor.Type == VoxelType.Dirt || neighbor.Type == VoxelType.Grass)
            {
                if (Random.value < 0.5f)
                {
                    _map.SetVoxel(nx, ny, VoxelType.Sand, false);
                }
            }
        }

        public bool IsSolidBlock(int x, int y)
        {
            if (!_map.InBounds(x, y)) return true;
            Voxel v = _map.GetVoxel(x, y);
            return v.IsSolid;
        }

        public bool IsPassable(int x, int y)
        {
            if (!_map.InBounds(x, y)) return false;
            Voxel v = _map.GetVoxel(x, y);
            return v.IsEmpty || v.IsGas;
        }

        public bool HasSolidSupport(int x, int y)
        {
            int belowY = y - 1;
            return IsSolidBlock(x, belowY);
        }

        public Vector2Int FindGroundPosition(int x, int startY)
        {
            int y = startY;
            while (y > 0)
            {
                if (IsSolidBlock(x, y - 1))
                {
                    return new Vector2Int(x, y);
                }
                y--;
            }
            return new Vector2Int(x, 0);
        }

        public List<Vector2Int> GetConnectedVoxels(int startX, int startY, VoxelCategory category)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            Vector2Int start = new Vector2Int(startX, startY);
            Voxel startVoxel = _map.GetVoxel(startX, startY);
            if (startVoxel.Category != category) return result;

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                result.Add(current);

                Vector2Int[] neighbors = new Vector2Int[]
                {
                    new Vector2Int(current.x + 1, current.y),
                    new Vector2Int(current.x - 1, current.y),
                    new Vector2Int(current.x, current.y + 1),
                    new Vector2Int(current.x, current.y - 1)
                };

                foreach (Vector2Int neighbor in neighbors)
                {
                    if (visited.Contains(neighbor)) continue;
                    if (!_map.InBounds(neighbor.x, neighbor.y)) continue;

                    Voxel nv = _map.GetVoxel(neighbor.x, neighbor.y);
                    if (nv.Category == category)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return result;
        }
    }
}
