using System;
using UnityEngine;

namespace FluidVoxelSandbox.Core
{
    public class VoxelMap
    {
        private Voxel[,] _voxels;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float VoxelSize { get; private set; }

        public event Action<int, int, VoxelType> OnVoxelChanged;
        public event Action OnMapResized;

        public VoxelMap(int width, int height, float voxelSize = 1f)
        {
            Width = width;
            Height = height;
            VoxelSize = voxelSize;
            _voxels = new Voxel[width, height];
            InitializeEmpty();
        }

        private void InitializeEmpty()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _voxels[x, y] = new Voxel(VoxelType.Empty);
                }
            }
        }

        public void Clear()
        {
            InitializeEmpty();
        }

        public void Resize(int newWidth, int newHeight)
        {
            var newVoxels = new Voxel[newWidth, newHeight];
            int copyWidth = Math.Min(Width, newWidth);
            int copyHeight = Math.Min(Height, newHeight);

            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    if (x < copyWidth && y < copyHeight)
                        newVoxels[x, y] = _voxels[x, y];
                    else
                        newVoxels[x, y] = new Voxel(VoxelType.Empty);
                }
            }

            _voxels = newVoxels;
            Width = newWidth;
            Height = newHeight;
            OnMapResized?.Invoke();
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public Voxel GetVoxel(int x, int y)
        {
            if (!InBounds(x, y)) return new Voxel(VoxelType.Stone);
            return _voxels[x, y];
        }

        public VoxelType GetVoxelType(int x, int y)
        {
            if (!InBounds(x, y)) return VoxelType.Stone;
            return _voxels[x, y].Type;
        }

        public void SetVoxel(int x, int y, VoxelType type, bool notify = true)
        {
            if (!InBounds(x, y)) return;

            VoxelCategory currentCat = _voxels[x, y].Category;
            VoxelCategory newCat = VoxelProperties.GetCategory(type);

            if (!VoxelProperties.CanBeReplaced(_voxels[x, y].Type, type) && type != VoxelType.Empty)
            {
                if (currentCat == VoxelCategory.Solid && newCat == VoxelCategory.Solid)
                {
                    if (type != VoxelType.Empty) return;
                }
            }

            Voxel oldVoxel = _voxels[x, y];
            if (type == VoxelType.Empty)
            {
                _voxels[x, y] = new Voxel(VoxelType.Empty);
            }
            else
            {
                _voxels[x, y] = new Voxel(type);
            }

            if (notify && oldVoxel.Type != type)
            {
                OnVoxelChanged?.Invoke(x, y, type);
            }
        }

        public void SetVoxel(int x, int y, Voxel voxel, bool notify = true)
        {
            if (!InBounds(x, y)) return;
            VoxelType oldType = _voxels[x, y].Type;
            _voxels[x, y] = voxel;
            if (notify && oldType != voxel.Type)
            {
                OnVoxelChanged?.Invoke(x, y, voxel.Type);
            }
        }

        public void SwapVoxels(int x1, int y1, int x2, int y2, bool notify = true)
        {
            if (!InBounds(x1, y1) || !InBounds(x2, y2)) return;

            Voxel temp = _voxels[x1, y1];
            _voxels[x1, y1] = _voxels[x2, y2];
            _voxels[x2, y2] = temp;

            if (notify)
            {
                OnVoxelChanged?.Invoke(x1, y1, _voxels[x1, y1].Type);
                OnVoxelChanged?.Invoke(x2, y2, _voxels[x2, y2].Type);
            }
        }

        public bool WorldToVoxel(Vector2 worldPos, out int voxelX, out int voxelY)
        {
            voxelX = Mathf.FloorToInt(worldPos.x / VoxelSize);
            voxelY = Mathf.FloorToInt(worldPos.y / VoxelSize);
            return InBounds(voxelX, voxelY);
        }

        public Vector2 VoxelToWorld(int voxelX, int voxelY)
        {
            return new Vector2(voxelX * VoxelSize + VoxelSize * 0.5f, voxelY * VoxelSize + VoxelSize * 0.5f);
        }

        public void SetVoxelVelocity(int x, int y, Vector2 velocity)
        {
            if (!InBounds(x, y)) return;
            Voxel v = _voxels[x, y];
            v.Velocity = velocity;
            _voxels[x, y] = v;
        }

        public void SetVoxelAlpha(int x, int y, byte alpha)
        {
            if (!InBounds(x, y)) return;
            Voxel v = _voxels[x, y];
            v.Alpha = alpha;
            _voxels[x, y] = v;
        }

        public void SetVoxelState(int x, int y, VoxelState state)
        {
            if (!InBounds(x, y)) return;
            Voxel v = _voxels[x, y];
            v.State = state;
            _voxels[x, y] = v;
        }

        public void ReduceLifeTime(int x, int y, float dt)
        {
            if (!InBounds(x, y)) return;
            Voxel v = _voxels[x, y];
            v.LifeTime -= dt;
            if (v.LifeTime <= 0f)
            {
                v.Type = VoxelType.Empty;
                v.LifeTime = 0f;
            }
            _voxels[x, y] = v;
        }

        public Voxel[,] GetAllVoxels()
        {
            return _voxels;
        }

        public byte[] Serialize()
        {
            int size = Width * Height * 2;
            byte[] data = new byte[size];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int idx = (y * Width + x) * 2;
                    data[idx] = (byte)_voxels[x, y].Type;
                    data[idx + 1] = _voxels[x, y].Alpha;
                }
            }
            return data;
        }

        public void Deserialize(byte[] data)
        {
            int expectedSize = Width * Height * 2;
            if (data.Length != expectedSize)
            {
                Debug.LogWarning($"Serialized data size mismatch: expected {expectedSize}, got {data.Length}");
                return;
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int idx = (y * Width + x) * 2;
                    VoxelType type = (VoxelType)data[idx];
                    byte alpha = data[idx + 1];
                    _voxels[x, y] = new Voxel(type);
                    if (type != VoxelType.Empty)
                    {
                        Voxel v = _voxels[x, y];
                        v.Alpha = alpha;
                        _voxels[x, y] = v;
                    }
                    OnVoxelChanged?.Invoke(x, y, type);
                }
            }
        }
    }
}
