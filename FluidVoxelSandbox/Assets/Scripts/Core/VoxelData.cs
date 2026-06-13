using UnityEngine;

namespace FluidVoxelSandbox.Core
{
    public enum VoxelType : byte
    {
        Empty = 0,
        Stone = 1,
        Dirt = 2,
        Grass = 3,
        Sand = 4,
        Water = 10,
        Oil = 11,
        Lava = 12,
        Smoke = 20,
        Steam = 21,
        ToxicGas = 22
    }

    public enum VoxelState : byte
    {
        Static = 0,
        Falling = 1,
        Flowing = 2,
        Diffusing = 3
    }

    public enum VoxelCategory : byte
    {
        Empty = 0,
        Solid = 1,
        Liquid = 2,
        Gas = 3
    }

    public struct Voxel
    {
        public VoxelType Type;
        public VoxelState State;
        public float Density;
        public float Temperature;
        public Vector2 Velocity;
        public float LifeTime;
        public byte Alpha;

        public Voxel(VoxelType type)
        {
            Type = type;
            State = VoxelState.Static;
            Density = VoxelProperties.GetDensity(type);
            Temperature = VoxelProperties.GetDefaultTemperature(type);
            Velocity = Vector2.zero;
            LifeTime = VoxelProperties.GetMaxLifeTime(type);
            Alpha = 255;
        }

        public bool IsEmpty => Type == VoxelType.Empty;
        public bool IsSolid => VoxelProperties.GetCategory(Type) == VoxelCategory.Solid;
        public bool IsLiquid => VoxelProperties.GetCategory(Type) == VoxelCategory.Liquid;
        public bool IsGas => VoxelProperties.GetCategory(Type) == VoxelCategory.Gas;
        public VoxelCategory Category => VoxelProperties.GetCategory(Type);
    }

    public static class VoxelProperties
    {
        public static VoxelCategory GetCategory(VoxelType type)
        {
            switch (type)
            {
                case VoxelType.Empty:
                    return VoxelCategory.Empty;
                case VoxelType.Stone:
                case VoxelType.Dirt:
                case VoxelType.Grass:
                case VoxelType.Sand:
                    return VoxelCategory.Solid;
                case VoxelType.Water:
                case VoxelType.Oil:
                case VoxelType.Lava:
                    return VoxelCategory.Liquid;
                case VoxelType.Smoke:
                case VoxelType.Steam:
                case VoxelType.ToxicGas:
                    return VoxelCategory.Gas;
                default:
                    return VoxelCategory.Empty;
            }
        }

        public static float GetDensity(VoxelType type)
        {
            switch (type)
            {
                case VoxelType.Stone: return 3.0f;
                case VoxelType.Dirt: return 1.5f;
                case VoxelType.Grass: return 1.2f;
                case VoxelType.Sand: return 1.6f;
                case VoxelType.Water: return 1.0f;
                case VoxelType.Oil: return 0.8f;
                case VoxelType.Lava: return 2.5f;
                case VoxelType.Smoke: return 0.05f;
                case VoxelType.Steam: return 0.02f;
                case VoxelType.ToxicGas: return 0.08f;
                default: return 0f;
            }
        }

        public static float GetViscosity(VoxelType type)
        {
            switch (type)
            {
                case VoxelType.Water: return 0.6f;
                case VoxelType.Oil: return 0.9f;
                case VoxelType.Lava: return 0.98f;
                case VoxelType.Smoke: return 0.3f;
                case VoxelType.Steam: return 0.2f;
                case VoxelType.ToxicGas: return 0.4f;
                default: return 1f;
            }
        }

        public static float GetDefaultTemperature(VoxelType type)
        {
            switch (type)
            {
                case VoxelType.Lava: return 1200f;
                case VoxelType.Steam: return 150f;
                case VoxelType.Water: return 20f;
                default: return 25f;
            }
        }

        public static float GetMaxLifeTime(VoxelType type)
        {
            switch (type)
            {
                case VoxelType.Smoke: return 8f;
                case VoxelType.Steam: return 5f;
                case VoxelType.ToxicGas: return 15f;
                default: return float.MaxValue;
            }
        }

        public static Color GetColor(VoxelType type, byte alpha = 255)
        {
            Color color;
            switch (type)
            {
                case VoxelType.Stone:
                    color = new Color32(128, 128, 135, alpha);
                    break;
                case VoxelType.Dirt:
                    color = new Color32(139, 90, 43, alpha);
                    break;
                case VoxelType.Grass:
                    color = new Color32(76, 175, 80, alpha);
                    break;
                case VoxelType.Sand:
                    color = new Color32(237, 201, 145, alpha);
                    break;
                case VoxelType.Water:
                    color = new Color32(33, 150, 243, alpha);
                    break;
                case VoxelType.Oil:
                    color = new Color32(93, 64, 55, alpha);
                    break;
                case VoxelType.Lava:
                    color = new Color32(255, 87, 34, alpha);
                    break;
                case VoxelType.Smoke:
                    color = new Color32(100, 100, 100, alpha);
                    break;
                case VoxelType.Steam:
                    color = new Color32(220, 220, 230, alpha);
                    break;
                case VoxelType.ToxicGas:
                    color = new Color32(156, 204, 101, alpha);
                    break;
                default:
                    color = Color.clear;
                    break;
            }
            return color;
        }

        public static bool CanBeReplaced(VoxelType currentType, VoxelType newType)
        {
            if (currentType == VoxelType.Empty) return true;
            VoxelCategory currentCat = GetCategory(currentType);
            VoxelCategory newCat = GetCategory(newType);

            if (newCat == VoxelCategory.Solid && currentCat == VoxelCategory.Empty) return true;
            if (newCat == VoxelCategory.Liquid && currentCat == VoxelCategory.Empty) return true;
            if (newCat == VoxelCategory.Liquid && currentCat == VoxelCategory.Gas) return true;
            if (newCat == VoxelCategory.Gas && currentCat == VoxelCategory.Empty) return true;
            if (newCat == VoxelCategory.Solid && currentCat == VoxelCategory.Liquid) return false;
            if (newCat == VoxelCategory.Solid && currentCat == VoxelCategory.Gas) return true;

            return false;
        }
    }
}
