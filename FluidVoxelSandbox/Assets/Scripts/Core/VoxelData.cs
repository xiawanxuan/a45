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
        Obsidian = 5,
        Glass = 6,
        Ice = 7,
        Mud = 8,
        Salt = 9,
        Water = 10,
        Oil = 11,
        Lava = 12,
        Acid = 13,
        Lye = 14,
        Alcohol = 15,
        Concrete = 16,
        Rust = 17,
        Ash = 18,
        Smoke = 20,
        Steam = 21,
        ToxicGas = 22,
        Fire = 23,
        Foam = 24,
        Crystal = 25
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
                case VoxelType.Obsidian:
                case VoxelType.Glass:
                case VoxelType.Ice:
                case VoxelType.Mud:
                case VoxelType.Salt:
                case VoxelType.Concrete:
                case VoxelType.Rust:
                case VoxelType.Ash:
                case VoxelType.Crystal:
                    return VoxelCategory.Solid;
                case VoxelType.Water:
                case VoxelType.Oil:
                case VoxelType.Lava:
                case VoxelType.Acid:
                case VoxelType.Lye:
                case VoxelType.Alcohol:
                    return VoxelCategory.Liquid;
                case VoxelType.Smoke:
                case VoxelType.Steam:
                case VoxelType.ToxicGas:
                case VoxelType.Fire:
                case VoxelType.Foam:
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
                case VoxelType.Obsidian: return 3.5f;
                case VoxelType.Glass: return 2.4f;
                case VoxelType.Ice: return 0.92f;
                case VoxelType.Mud: return 1.4f;
                case VoxelType.Salt: return 2.16f;
                case VoxelType.Concrete: return 2.8f;
                case VoxelType.Rust: return 2.5f;
                case VoxelType.Ash: return 0.6f;
                case VoxelType.Crystal: return 2.65f;
                case VoxelType.Water: return 1.0f;
                case VoxelType.Oil: return 0.8f;
                case VoxelType.Lava: return 2.5f;
                case VoxelType.Acid: return 1.2f;
                case VoxelType.Lye: return 1.1f;
                case VoxelType.Alcohol: return 0.79f;
                case VoxelType.Smoke: return 0.05f;
                case VoxelType.Steam: return 0.02f;
                case VoxelType.ToxicGas: return 0.08f;
                case VoxelType.Fire: return 0.01f;
                case VoxelType.Foam: return 0.05f;
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
                case VoxelType.Acid: return 0.55f;
                case VoxelType.Lye: return 0.65f;
                case VoxelType.Alcohol: return 0.4f;
                case VoxelType.Smoke: return 0.3f;
                case VoxelType.Steam: return 0.2f;
                case VoxelType.ToxicGas: return 0.4f;
                case VoxelType.Fire: return 0.1f;
                case VoxelType.Foam: return 0.5f;
                default: return 1f;
            }
        }

        public static float GetDefaultTemperature(VoxelType type)
        {
            switch (type)
            {
                case VoxelType.Lava: return 1200f;
                case VoxelType.Fire: return 600f;
                case VoxelType.Steam: return 150f;
                case VoxelType.Water: return 20f;
                case VoxelType.Alcohol: return 22f;
                case VoxelType.Acid: return 25f;
                case VoxelType.Lye: return 25f;
                case VoxelType.Ice: return -10f;
                case VoxelType.Obsidian: return 80f;
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
                case VoxelType.Fire: return 1.2f;
                case VoxelType.Foam: return 6f;
                case VoxelType.Mud: return float.MaxValue;
                case VoxelType.Crystal: return float.MaxValue;
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
                case VoxelType.Obsidian:
                    color = new Color32(30, 25, 45, alpha);
                    break;
                case VoxelType.Glass:
                    color = new Color32(200, 220, 230, 120);
                    color.a = alpha / 255f * 0.6f;
                    break;
                case VoxelType.Ice:
                    color = new Color32(180, 220, 255, 220);
                    color.a = alpha / 255f * 0.85f;
                    break;
                case VoxelType.Mud:
                    color = new Color32(90, 65, 40, alpha);
                    break;
                case VoxelType.Salt:
                    color = new Color32(245, 245, 250, alpha);
                    break;
                case VoxelType.Concrete:
                    color = new Color32(160, 160, 150, alpha);
                    break;
                case VoxelType.Rust:
                    color = new Color32(183, 65, 14, alpha);
                    break;
                case VoxelType.Ash:
                    color = new Color32(70, 65, 60, alpha);
                    break;
                case VoxelType.Crystal:
                    color = new Color32(160, 200, 255, 230);
                    color.a = alpha / 255f * 0.9f;
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
                case VoxelType.Acid:
                    color = new Color32(192, 255, 80, alpha);
                    break;
                case VoxelType.Lye:
                    color = new Color32(160, 200, 230, alpha);
                    break;
                case VoxelType.Alcohol:
                    color = new Color32(220, 240, 255, 200);
                    color.a = alpha / 255f * 0.8f;
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
                case VoxelType.Fire:
                    color = new Color32(255, 200, 50, alpha);
                    break;
                case VoxelType.Foam:
                    color = new Color32(250, 250, 255, alpha);
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
