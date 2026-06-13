using UnityEngine;
using System.Collections.Generic;
using FluidVoxelSandbox.Core;
using FluidVoxelSandbox.Wind;

namespace FluidVoxelSandbox.Physics
{
    public class FluidSimulation : MonoBehaviour
    {
        private VoxelMap _map;
        private WindField _windField;
        private WindController _windController;

        [Header("Liquid Settings")]
        public float gravity = 9.8f;
        public float liquidMaxSpeed = 3.0f;
        public float liquidSpreadChance = 0.6f;
        public int maxLiquidSpreadDistance = 3;

        [Header("Gas Settings")]
        public float gasDiffusionRate = 0.8f;
        public float gasBuoyancy = 0.3f;
        public float gasFadeRate = 0.5f;
        public float windInfluence = 0.7f;

        [Header("Performance")]
        public int chunkSize = 32;
        public bool multiThreaded = false;

        private bool[,] _activeChunks;
        private List<Vector2Int> _liquidUpdates = new List<Vector2Int>();
        private List<Vector2Int> _gasUpdates = new List<Vector2Int>();
        private int _chunkCols;
        private int _chunkRows;

        [Header("Stats")]
        public int activeLiquidCells;
        public int activeGasCells;

        public void Initialize(VoxelMap map, WindField windField, WindController windController)
        {
            _map = map;
            _windField = windField;
            _windController = windController;

            _chunkCols = Mathf.CeilToInt((float)map.Width / chunkSize);
            _chunkRows = Mathf.CeilToInt((float)map.Height / chunkSize);
            _activeChunks = new bool[_chunkCols, _chunkRows];
            MarkAllChunksActive();
        }

        public void Step(float dt)
        {
            if (_map == null) return;
            if (_windField != null && _windField.Width != _map.Width)
            {
                _windField.Initialize(_map.Width, _map.Height);
            }

            _liquidUpdates.Clear();
            _gasUpdates.Clear();

            ScanActiveChunks(dt);
            ProcessLiquids(dt);
            ProcessGases(dt);
            UpdateLifeTimes(dt);
            UpdateGasAlpha(dt);

            activeLiquidCells = _liquidUpdates.Count;
            activeGasCells = _gasUpdates.Count;
        }

        private void MarkAllChunksActive()
        {
            for (int cx = 0; cx < _chunkCols; cx++)
                for (int cy = 0; cy < _chunkRows; cy++)
                    _activeChunks[cx, cy] = true;
        }

        private void MarkChunkActive(int x, int y)
        {
            int cx = x / chunkSize;
            int cy = y / chunkSize;
            cx = Mathf.Clamp(cx, 0, _chunkCols - 1);
            cy = Mathf.Clamp(cy, 0, _chunkRows - 1);
            _activeChunks[cx, cy] = true;
        }

        private void ScanActiveChunks(float dt)
        {
            for (int cx = 0; cx < _chunkCols; cx++)
            {
                for (int cy = 0; cy < _chunkRows; cy++)
                {
                    if (!_activeChunks[cx, cy]) continue;
                    _activeChunks[cx, cy] = false;

                    int startX = cx * chunkSize;
                    int startY = cy * chunkSize;
                    int endX = Mathf.Min(startX + chunkSize, _map.Width);
                    int endY = Mathf.Min(startY + chunkSize, _map.Height);

                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            Voxel v = _map.GetVoxel(x, y);
                            if (v.IsLiquid) _liquidUpdates.Add(new Vector2Int(x, y));
                            if (v.IsGas) _gasUpdates.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
        }

        private void ProcessLiquids(float dt)
        {
            if (_liquidUpdates.Count == 0) return;

            for (int i = 0; i < _liquidUpdates.Count; i++)
            {
                Vector2Int pos = _liquidUpdates[i];
                UpdateLiquid(pos.x, pos.y, dt);
            }
        }

        private void UpdateLiquid(int x, int y, float dt)
        {
            Voxel voxel = _map.GetVoxel(x, y);
            if (!voxel.IsLiquid) return;

            VoxelType liquidType = voxel.Type;
            float viscosity = VoxelProperties.GetViscosity(liquidType);
            float moveChance = (1f - viscosity) * dt * liquidMaxSpeed;

            if (GameManager.Instance != null && !GameManager.Instance.enableGravity)
            {
                UpdateLiquidFreeFloat(x, y, dt, moveChance);
                return;
            }

            int belowY = y - 1;
            if (_map.InBounds(x, belowY))
            {
                Voxel below = _map.GetVoxel(x, belowY);

                if (below.IsEmpty)
                {
                    TryMoveLiquid(x, y, x, belowY);
                    return;
                }

                if (below.IsLiquid && below.Type != liquidType)
                {
                    float belowDensity = below.Density;
                    float thisDensity = voxel.Density;
                    if (thisDensity > belowDensity)
                    {
                        _map.SwapVoxels(x, y, x, belowY);
                        MarkChunkActive(x, y);
                        MarkChunkActive(x, belowY);
                        return;
                    }
                }
            }

            if (Random.value > moveChance) return;

            int[] dirs = Random.value > 0.5f ? new int[] { 1, -1 } : new int[] { -1, 1 };
            foreach (int dir in dirs)
            {
                int nx = x + dir;
                if (!_map.InBounds(nx, y)) continue;

                Voxel neighbor = _map.GetVoxel(nx, y);
                if (neighbor.IsEmpty)
                {
                    if (Random.value < liquidSpreadChance)
                    {
                        TryMoveLiquid(x, y, nx, y);
                        return;
                    }
                }
                else if (neighbor.IsLiquid && neighbor.Type != liquidType)
                {
                    if (voxel.Density > neighbor.Density)
                    {
                        _map.SwapVoxels(x, y, nx, y);
                        MarkChunkActive(x, y);
                        MarkChunkActive(nx, y);
                        return;
                    }
                }
            }

            foreach (int dir in dirs)
            {
                int nx = x + dir;
                int ny = y - 1;
                if (!_map.InBounds(nx, ny)) continue;

                Voxel diag = _map.GetVoxel(nx, ny);
                if (diag.IsEmpty)
                {
                    if (Random.value < moveChance * 0.5f)
                    {
                        TryMoveLiquid(x, y, nx, ny);
                        return;
                    }
                }
            }
        }

        private void UpdateLiquidFreeFloat(int x, int y, float dt, float moveChance)
        {
            if (_windField == null || GameManager.Instance == null || !GameManager.Instance.enableWind) return;

            Vector2 wind = _windField.GetWindAt(x, y) * windInfluence;
            wind.x += Random.Range(-0.3f, 0.3f);
            wind.y += Random.Range(-0.3f, 0.3f);

            int dx = 0, dy = 0;
            if (Mathf.Abs(wind.x) > 0.3f) dx = (int)Mathf.Sign(wind.x);
            if (Mathf.Abs(wind.y) > 0.3f) dy = (int)Mathf.Sign(wind.y);

            if (dx == 0 && dy == 0) return;

            int nx = x + dx;
            int ny = y + dy;

            if (_map.InBounds(nx, ny))
            {
                Voxel target = _map.GetVoxel(nx, ny);
                if (target.IsEmpty)
                {
                    _map.SetVoxel(nx, ny, _map.GetVoxelType(x, y), false);
                    _map.SetVoxel(x, y, VoxelType.Empty, false);
                    MarkChunkActive(x, y);
                    MarkChunkActive(nx, ny);
                }
            }
        }

        private void TryMoveLiquid(int fromX, int fromY, int toX, int toY)
        {
            VoxelType type = _map.GetVoxelType(fromX, fromY);
            _map.SetVoxel(toX, toY, type, false);
            _map.SetVoxel(fromX, fromY, VoxelType.Empty, false);
            MarkChunkActive(fromX, fromY);
            MarkChunkActive(toX, toY);
        }

        private void ProcessGases(float dt)
        {
            if (_gasUpdates.Count == 0) return;

            for (int i = 0; i < _gasUpdates.Count; i++)
            {
                Vector2Int pos = _gasUpdates[i];
                UpdateGas(pos.x, pos.y, dt);
            }
        }

        private void UpdateGas(int x, int y, float dt)
        {
            Voxel voxel = _map.GetVoxel(x, y);
            if (!voxel.IsGas) return;

            VoxelType gasType = voxel.Type;
            float density = voxel.Density;

            Vector2 windEffect = Vector2.zero;
            if (_windField != null && GameManager.Instance != null && GameManager.Instance.enableWind)
            {
                windEffect = _windField.GetWindWithBuoyancy(x, y, density) * windInfluence;
            }

            windEffect.y += (1f - density * 8f) * gasBuoyancy;
            windEffect += new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)) * gasDiffusionRate;

            float totalMag = windEffect.magnitude;
            if (totalMag < 0.1f) return;

            int dx = 0, dy = 0;

            float threshold = Random.value * totalMag;
            float cumulative = 0f;

            cumulative += Mathf.Abs(windEffect.x);
            if (threshold < cumulative)
            {
                dx = (int)Mathf.Sign(windEffect.x);
            }
            else
            {
                cumulative += Mathf.Abs(windEffect.y);
                if (threshold < cumulative)
                {
                    dy = (int)Mathf.Sign(windEffect.y);
                }
                else
                {
                    float r = Random.value;
                    if (r < 0.25f) dx = 1;
                    else if (r < 0.5f) dx = -1;
                    else if (r < 0.75f) dy = 1;
                    else dy = -1;
                }
            }

            int nx = x + dx;
            int ny = y + dy;

            if (!_map.InBounds(nx, ny)) return;

            Voxel target = _map.GetVoxel(nx, ny);

            if (target.IsEmpty)
            {
                TryMoveGas(x, y, nx, ny, gasType);
            }
            else if (target.IsGas)
            {
                if (density < target.Density)
                {
                    _map.SwapVoxels(x, y, nx, ny);
                    MarkChunkActive(x, y);
                    MarkChunkActive(nx, ny);
                }
                else
                {
                    TryDiffuseGas(x, y, gasType, dt);
                }
            }
            else if (target.IsLiquid)
            {
                if (density < target.Density * 0.1f)
                {
                    int tx = x;
                    int ty = y + 1;
                    if (_map.InBounds(tx, ty))
                    {
                        Voxel above = _map.GetVoxel(tx, ty);
                        if (above.IsEmpty)
                        {
                            TryMoveGas(x, y, tx, ty, gasType);
                        }
                    }
                }
            }
        }

        private void TryMoveGas(int fromX, int fromY, int toX, int toY, VoxelType type)
        {
            _map.SetVoxel(toX, toY, type, false);
            _map.SetVoxel(fromX, fromY, VoxelType.Empty, false);
            MarkChunkActive(fromX, fromY);
            MarkChunkActive(toX, toY);
        }

        private void TryDiffuseGas(int x, int y, VoxelType type, float dt)
        {
            if (Random.value > gasDiffusionRate * dt) return;

            int[] dirsX = { 1, -1, 0, 0 };
            int[] dirsY = { 0, 0, 1, -1 };
            int startIdx = Random.Range(0, 4);

            for (int i = 0; i < 4; i++)
            {
                int idx = (startIdx + i) % 4;
                int nx = x + dirsX[idx];
                int ny = y + dirsY[idx];
                if (!_map.InBounds(nx, ny)) continue;

                Voxel neighbor = _map.GetVoxel(nx, ny);
                if (neighbor.IsEmpty)
                {
                    TryMoveGas(x, y, nx, ny, type);
                    return;
                }
            }
        }

        private void UpdateLifeTimes(float dt)
        {
            for (int i = 0; i < _gasUpdates.Count; i++)
            {
                Vector2Int pos = _gasUpdates[i];
                Voxel v = _map.GetVoxel(pos.x, pos.y);
                if (v.IsGas && v.LifeTime != float.MaxValue)
                {
                    _map.ReduceLifeTime(pos.x, pos.y, dt);
                    if (_map.GetVoxel(pos.x, pos.y).IsEmpty)
                    {
                        MarkChunkActive(pos.x, pos.y);
                    }
                }
            }
        }

        private void UpdateGasAlpha(float dt)
        {
            for (int i = 0; i < _gasUpdates.Count; i++)
            {
                Vector2Int pos = _gasUpdates[i];
                Voxel v = _map.GetVoxel(pos.x, pos.y);
                if (v.IsGas && v.LifeTime != float.MaxValue)
                {
                    float maxLife = VoxelProperties.GetMaxLifeTime(v.Type);
                    float lifeRatio = Mathf.Clamp01(v.LifeTime / maxLife);
                    byte targetAlpha = (byte)(120f * lifeRatio + 40f);
                    _map.SetVoxelAlpha(pos.x, pos.y, targetAlpha);
                }
            }
        }
    }
}
