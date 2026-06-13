using UnityEngine;
using System.Collections.Generic;
using FluidVoxelSandbox.Core;

namespace FluidVoxelSandbox.Physics
{
    public enum ReactionType
    {
        Neutralization,
        Combustion,
        Crystallization,
        Thermal,
        Hydration,
        Dehydration,
        Dissolution,
        Precipitation,
        Oxidation,
        Eruption
    }

    public struct ReactionRule
    {
        public VoxelType ReagentA;
        public VoxelType ReagentB;
        public VoxelType PrimaryProduct;
        public VoxelType SecondaryProduct;
        public VoxelType GasProduct;

        public float ReactionChance;
        public float ConsumeARatio;
        public float ConsumeBRatio;
        public float HeatRelease;
        public float MinTemperature;
        public float MaxTemperature;

        public ReactionType Type;
    }

    public struct ActiveReaction
    {
        public int X;
        public int Y;
        public ReactionRule Rule;
        public float Intensity;
    }

    public class VoxelChemistry : MonoBehaviour
    {
        private VoxelMap _map;
        private VoxelCollision _collision;

        [Header("Reaction Control")]
        public bool enableReactions = true;
        public float reactionInterval = 0.08f;
        public int maxReactionsPerStep = 2000;
        public float heatTransferRange = 2;

        [Header("Combustion")]
        public float fireSpreadChance = 0.35f;
        public float fireLifetimeBoost = 0.8f;
        public int fireCheckRadius = 1;

        [Header("Stats")]
        public int reactionsThisStep;
        public int totalReactions;

        private List<ReactionRule> _reactionRules = new List<ReactionRule>();
        private List<ActiveReaction> _activeReactions = new List<ActiveReaction>();
        private Queue<Vector2Int> _reactionQueue = new Queue<Vector2Int>();
        private HashSet<Vector2Int> _processedCells = new HashSet<Vector2Int>();

        private float _reactionTimer;
        private int _chunkCols;
        private int _chunkRows;
        private int _chunkSize = 32;
        private bool[,] _dirtyChunks;

        public List<ReactionRule> ReactionRules => _reactionRules;

        public void Initialize(VoxelMap map, VoxelCollision collision)
        {
            _map = map;
            _collision = collision;

            _chunkCols = Mathf.CeilToInt((float)map.Width / _chunkSize);
            _chunkRows = Mathf.CeilToInt((float)map.Height / _chunkSize);
            _dirtyChunks = new bool[_chunkCols, _chunkRows];

            InitializeReactionRules();
            MarkAllChunksDirty();
        }

        private void InitializeReactionRules()
        {
            _reactionRules.Clear();

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Water,
                PrimaryProduct = VoxelType.Obsidian,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Steam,
                ReactionChance = 0.85f,
                ConsumeARatio = 0.7f,
                ConsumeBRatio = 1.0f,
                HeatRelease = -300f,
                MinTemperature = 0f,
                MaxTemperature = 5000f,
                Type = ReactionType.Thermal
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Sand,
                PrimaryProduct = VoxelType.Glass,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.4f,
                ConsumeARatio = 0.3f,
                ConsumeBRatio = 1.0f,
                HeatRelease = -50f,
                MinTemperature = 800f,
                MaxTemperature = 5000f,
                Type = ReactionType.Thermal
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Acid,
                ReagentB = VoxelType.Lye,
                PrimaryProduct = VoxelType.Salt,
                SecondaryProduct = VoxelType.Water,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.95f,
                ConsumeARatio = 1.0f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 150f,
                MinTemperature = -50f,
                MaxTemperature = 500f,
                Type = ReactionType.Neutralization
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Acid,
                ReagentB = VoxelType.Dirt,
                PrimaryProduct = VoxelType.Mud,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.3f,
                ConsumeARatio = 0.5f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 30f,
                MinTemperature = 0f,
                MaxTemperature = 200f,
                Type = ReactionType.Dissolution
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Acid,
                ReagentB = VoxelType.Grass,
                PrimaryProduct = VoxelType.Mud,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.ToxicGas,
                ReactionChance = 0.45f,
                ConsumeARatio = 0.6f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 40f,
                MinTemperature = 0f,
                MaxTemperature = 150f,
                Type = ReactionType.Dissolution
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Acid,
                ReagentB = VoxelType.Stone,
                PrimaryProduct = VoxelType.Salt,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.08f,
                ConsumeARatio = 0.8f,
                ConsumeBRatio = 0.3f,
                HeatRelease = 20f,
                MinTemperature = 0f,
                MaxTemperature = 200f,
                Type = ReactionType.Dissolution
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Water,
                ReagentB = VoxelType.Dirt,
                PrimaryProduct = VoxelType.Mud,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.25f,
                ConsumeARatio = 0.5f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 0f,
                MinTemperature = 0f,
                MaxTemperature = 100f,
                Type = ReactionType.Hydration
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Water,
                ReagentB = VoxelType.Mud,
                PrimaryProduct = VoxelType.Mud,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.1f,
                ConsumeARatio = 0.3f,
                ConsumeBRatio = 0f,
                HeatRelease = 0f,
                MinTemperature = 0f,
                MaxTemperature = 100f,
                Type = ReactionType.Hydration
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Water,
                ReagentB = VoxelType.Sand,
                PrimaryProduct = VoxelType.Mud,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.1f,
                ConsumeARatio = 0.7f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 0f,
                MinTemperature = 0f,
                MaxTemperature = 100f,
                Type = ReactionType.Hydration
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Fire,
                ReagentB = VoxelType.Grass,
                PrimaryProduct = VoxelType.Ash,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Smoke,
                ReactionChance = 0.7f,
                ConsumeARatio = 0.3f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 200f,
                MinTemperature = 200f,
                MaxTemperature = 5000f,
                Type = ReactionType.Combustion
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Fire,
                ReagentB = VoxelType.Dirt,
                PrimaryProduct = VoxelType.Ash,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Smoke,
                ReactionChance = 0.25f,
                ConsumeARatio = 0.4f,
                ConsumeBRatio = 0.5f,
                HeatRelease = 80f,
                MinTemperature = 200f,
                MaxTemperature = 5000f,
                Type = ReactionType.Combustion
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Fire,
                ReagentB = VoxelType.Oil,
                PrimaryProduct = VoxelType.Empty,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Smoke,
                ReactionChance = 1.0f,
                ConsumeARatio = 0.5f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 400f,
                MinTemperature = 100f,
                MaxTemperature = 5000f,
                Type = ReactionType.Combustion
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Fire,
                ReagentB = VoxelType.Alcohol,
                PrimaryProduct = VoxelType.Empty,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Fire,
                ReactionChance = 1.0f,
                ConsumeARatio = 0.2f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 350f,
                MinTemperature = 100f,
                MaxTemperature = 5000f,
                Type = ReactionType.Combustion
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Fire,
                ReagentB = VoxelType.Water,
                PrimaryProduct = VoxelType.Empty,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Steam,
                ReactionChance = 0.9f,
                ConsumeARatio = 1.0f,
                ConsumeBRatio = 0.6f,
                HeatRelease = -100f,
                MinTemperature = 0f,
                MaxTemperature = 5000f,
                Type = ReactionType.Thermal
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Grass,
                PrimaryProduct = VoxelType.Stone,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Smoke,
                ReactionChance = 0.8f,
                ConsumeARatio = 0.5f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 100f,
                MinTemperature = 500f,
                MaxTemperature = 5000f,
                Type = ReactionType.Combustion
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Dirt,
                PrimaryProduct = VoxelType.Stone,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Smoke,
                ReactionChance = 0.6f,
                ConsumeARatio = 0.4f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 50f,
                MinTemperature = 500f,
                MaxTemperature = 5000f,
                Type = ReactionType.Thermal
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Oil,
                PrimaryProduct = VoxelType.Stone,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Smoke,
                ReactionChance = 0.95f,
                ConsumeARatio = 0.3f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 300f,
                MinTemperature = 500f,
                MaxTemperature = 5000f,
                Type = ReactionType.Combustion
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Alcohol,
                PrimaryProduct = VoxelType.Stone,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Fire,
                ReactionChance = 1.0f,
                ConsumeARatio = 0.2f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 280f,
                MinTemperature = 500f,
                MaxTemperature = 5000f,
                Type = ReactionType.Combustion
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lye,
                ReagentB = VoxelType.Dirt,
                PrimaryProduct = VoxelType.Mud,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.35f,
                ConsumeARatio = 0.6f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 20f,
                MinTemperature = 0f,
                MaxTemperature = 150f,
                Type = ReactionType.Dissolution
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lye,
                ReagentB = VoxelType.Oil,
                PrimaryProduct = VoxelType.Foam,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.5f,
                ConsumeARatio = 0.8f,
                ConsumeBRatio = 0.8f,
                HeatRelease = 30f,
                MinTemperature = 20f,
                MaxTemperature = 100f,
                Type = ReactionType.Neutralization
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Steam,
                ReagentB = VoxelType.Sand,
                PrimaryProduct = VoxelType.Crystal,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.05f,
                ConsumeARatio = 1.0f,
                ConsumeBRatio = 1.0f,
                HeatRelease = -50f,
                MinTemperature = 100f,
                MaxTemperature = 300f,
                Type = ReactionType.Crystallization
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Steam,
                ReagentB = VoxelType.Salt,
                PrimaryProduct = VoxelType.Crystal,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.15f,
                ConsumeARatio = 1.0f,
                ConsumeBRatio = 1.0f,
                HeatRelease = -40f,
                MinTemperature = 80f,
                MaxTemperature = 250f,
                Type = ReactionType.Crystallization
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Water,
                ReagentB = VoxelType.Salt,
                PrimaryProduct = VoxelType.Empty,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.2f,
                ConsumeARatio = 1.0f,
                ConsumeBRatio = 0.6f,
                HeatRelease = 0f,
                MinTemperature = 0f,
                MaxTemperature = 100f,
                Type = ReactionType.Dissolution
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Acid,
                ReagentB = VoxelType.Concrete,
                PrimaryProduct = VoxelType.Sand,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.2f,
                ConsumeARatio = 0.9f,
                ConsumeBRatio = 0.4f,
                HeatRelease = 25f,
                MinTemperature = 0f,
                MaxTemperature = 150f,
                Type = ReactionType.Dissolution
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Concrete,
                PrimaryProduct = VoxelType.Obsidian,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.3f,
                ConsumeARatio = 0.6f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 30f,
                MinTemperature = 800f,
                MaxTemperature = 5000f,
                Type = ReactionType.Thermal
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Water,
                ReagentB = VoxelType.Concrete,
                PrimaryProduct = VoxelType.Concrete,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Empty,
                ReactionChance = 0.05f,
                ConsumeARatio = 1.0f,
                ConsumeBRatio = 0f,
                HeatRelease = 10f,
                MinTemperature = 0f,
                MaxTemperature = 100f,
                Type = ReactionType.Hydration
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Ice,
                PrimaryProduct = VoxelType.Stone,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Steam,
                ReactionChance = 0.9f,
                ConsumeARatio = 0.5f,
                ConsumeBRatio = 1.0f,
                HeatRelease = -200f,
                MinTemperature = 500f,
                MaxTemperature = 5000f,
                Type = ReactionType.Thermal
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Acid,
                PrimaryProduct = VoxelType.Obsidian,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.ToxicGas,
                ReactionChance = 0.7f,
                ConsumeARatio = 0.5f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 80f,
                MinTemperature = 500f,
                MaxTemperature = 5000f,
                Type = ReactionType.Thermal
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Lye,
                PrimaryProduct = VoxelType.Stone,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Steam,
                ReactionChance = 0.65f,
                ConsumeARatio = 0.5f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 50f,
                MinTemperature = 500f,
                MaxTemperature = 5000f,
                Type = ReactionType.Thermal
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Fire,
                ReagentB = VoxelType.Mud,
                PrimaryProduct = VoxelType.Dirt,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Steam,
                ReactionChance = 0.4f,
                ConsumeARatio = 0.6f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 60f,
                MinTemperature = 100f,
                MaxTemperature = 5000f,
                Type = ReactionType.Dehydration
            });

            AddReaction(new ReactionRule
            {
                ReagentA = VoxelType.Lava,
                ReagentB = VoxelType.Mud,
                PrimaryProduct = VoxelType.Stone,
                SecondaryProduct = VoxelType.Empty,
                GasProduct = VoxelType.Steam,
                ReactionChance = 0.75f,
                ConsumeARatio = 0.4f,
                ConsumeBRatio = 1.0f,
                HeatRelease = 80f,
                MinTemperature = 500f,
                MaxTemperature = 5000f,
                Type = ReactionType.Dehydration
            });
        }

        private void AddReaction(ReactionRule rule)
        {
            _reactionRules.Add(rule);
        }

        public void StepChemistry(float dt)
        {
            if (!enableReactions || _map == null) return;

            reactionsThisStep = 0;

            _reactionTimer += dt;
            if (_reactionTimer < reactionInterval) return;
            _reactionTimer = 0f;

            ProcessFireSpread(dt);
            ScanAndProcessReactions(dt);
        }

        private void ProcessFireSpread(float dt)
        {
            List<Vector2Int> fireCells = new List<Vector2Int>();

            for (int x = 0; x < _map.Width; x++)
            {
                for (int y = 0; y < _map.Height; y++)
                {
                    Voxel v = _map.GetVoxel(x, y);
                    if (v.Type == VoxelType.Fire)
                    {
                        fireCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            for (int i = 0; i < fireCells.Count; i++)
            {
                Vector2Int fire = fireCells[i];
                if (!_map.InBounds(fire.x, fire.y)) continue;
                Voxel fireVoxel = _map.GetVoxel(fire.x, fire.y);
                if (fireVoxel.Type != VoxelType.Fire) continue;

                int[] dx = { -1, 0, 1, 0, -1, 1, -1, 1 };
                int[] dy = { 0, -1, 0, 1, -1, -1, 1, 1 };

                for (int d = 0; d < dx.Length; d++)
                {
                    int nx = fire.x + dx[d];
                    int ny = fire.y + dy[d];
                    if (!_map.InBounds(nx, ny)) continue;

                    Voxel neighbor = _map.GetVoxel(nx, ny);
                    if (IsFlammable(neighbor.Type))
                    {
                        if (Random.value < fireSpreadChance * dt * 12f)
                        {
                            TriggerCombustion(nx, ny);
                        }
                    }
                }
            }
        }

        private bool IsFlammable(VoxelType type)
        {
            switch (type)
            {
                case VoxelType.Grass:
                case VoxelType.Dirt:
                case VoxelType.Oil:
                case VoxelType.Alcohol:
                case VoxelType.Mud:
                    return true;
                default:
                    return false;
            }
        }

        private void TriggerCombustion(int x, int y)
        {
            if (!_map.InBounds(x, y)) return;
            Voxel v = _map.GetVoxel(x, y);
            if (v.Type == VoxelType.Empty) return;

            if (Random.value < 0.5f)
            {
                _map.SetVoxel(x, y, VoxelType.Fire, false);
                _map.SetVoxelTemperature(x, y, 600f);
            }

            MarkDirty(x, y);
        }

        private void MarkAllChunksDirty()
        {
            for (int cx = 0; cx < _chunkCols; cx++)
                for (int cy = 0; cy < _chunkRows; cy++)
                    _dirtyChunks[cx, cy] = true;
        }

        private void MarkDirty(int x, int y)
        {
            int cx = x / _chunkSize;
            int cy = y / _chunkSize;
            cx = Mathf.Clamp(cx, 0, _chunkCols - 1);
            cy = Mathf.Clamp(cy, 0, _chunkRows - 1);
            _dirtyChunks[cx, cy] = true;
        }

        private void ScanAndProcessReactions(float dt)
        {
            _processedCells.Clear();
            _reactionQueue.Clear();

            for (int cx = 0; cx < _chunkCols; cx++)
            {
                for (int cy = 0; cy < _chunkRows; cy++)
                {
                    if (!_dirtyChunks[cx, cy]) continue;
                    _dirtyChunks[cx, cy] = false;

                    int startX = cx * _chunkSize;
                    int startY = cy * _chunkSize;
                    int endX = Mathf.Min(startX + _chunkSize, _map.Width);
                    int endY = Mathf.Min(startY + _chunkSize, _map.Height);

                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            Voxel v = _map.GetVoxel(x, y);
                            if (v.IsEmpty) continue;
                            if (v.Type == VoxelType.Fire) continue;

                            Vector2Int key = new Vector2Int(x, y);
                            if (_processedCells.Contains(key)) continue;

                            _reactionQueue.Enqueue(key);
                            _processedCells.Add(key);

                            while (_reactionQueue.Count > 0 && reactionsThisStep < maxReactionsPerStep)
                            {
                                Vector2Int cell = _reactionQueue.Dequeue();
                                CheckCellReactions(cell.x, cell.y, dt);
                            }
                        }
                    }
                }
            }

            totalReactions += reactionsThisStep;
        }

        private void CheckCellReactions(int x, int y, float dt)
        {
            if (!_map.InBounds(x, y)) return;
            Voxel cell = _map.GetVoxel(x, y);
            if (cell.IsEmpty) return;

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            for (int d = 0; d < dx.Length; d++)
            {
                int nx = x + dx[d];
                int ny = y + dy[d];
                if (!_map.InBounds(nx, ny)) continue;

                Voxel neighbor = _map.GetVoxel(nx, ny);
                if (neighbor.IsEmpty) continue;

                TryTriggerReaction(x, y, nx, ny, dt);
            }
        }

        private void TryTriggerReaction(int ax, int ay, int bx, int by, float dt)
        {
            if (!_map.InBounds(ax, ay) || !_map.InBounds(bx, by)) return;

            Voxel a = _map.GetVoxel(ax, ay);
            Voxel b = _map.GetVoxel(bx, by);

            if (a.IsEmpty || b.IsEmpty) return;
            if (a.Type == b.Type) return;

            float avgTemp = (a.Temperature + b.Temperature) * 0.5f;

            ReactionRule rule;
            bool ruleFound = FindReactionRule(a.Type, b.Type, avgTemp, out rule);
            if (!ruleFound) return;

            float chance = rule.ReactionChance * dt / reactionInterval;
            if (Random.value >= chance) return;

            ExecuteReaction(ax, ay, bx, by, ref a, ref b, rule);
            reactionsThisStep++;

            MarkDirty(ax, ay);
            MarkDirty(bx, by);
        }

        private bool FindReactionRule(VoxelType a, VoxelType b, float temp, out ReactionRule result)
        {
            for (int i = 0; i < _reactionRules.Count; i++)
            {
                ReactionRule rule = _reactionRules[i];
                bool matchA = (rule.ReagentA == a && rule.ReagentB == b);
                bool matchB = (rule.ReagentA == b && rule.ReagentB == a);
                if ((matchA || matchB) && temp >= rule.MinTemperature && temp <= rule.MaxTemperature)
                {
                    result = rule;
                    return true;
                }
            }
            result = default(ReactionRule);
            return false;
        }

        private void ExecuteReaction(int ax, int ay, int bx, int by,
            ref Voxel a, ref Voxel b, ReactionRule rule)
        {
            bool swapAB = (rule.ReagentA == b.Type && rule.ReagentB == a.Type);
            int reagentAX = swapAB ? bx : ax;
            int reagentAY = swapAB ? by : ay;
            int reagentBX = swapAB ? ax : bx;
            int reagentBY = swapAB ? ay : by;

            bool consumeA = Random.value < rule.ConsumeARatio;
            bool consumeB = Random.value < rule.ConsumeBRatio;

            if (consumeA)
            {
                _map.SetVoxel(reagentAX, reagentAY, VoxelType.Empty, false);
            }
            if (consumeB)
            {
                _map.SetVoxel(reagentBX, reagentBY, VoxelType.Empty, false);
            }

            if (rule.PrimaryProduct != VoxelType.Empty)
            {
                int targetX, targetY;
                if (!consumeA && _map.GetVoxel(reagentAX, reagentAY).Type != VoxelType.Empty)
                {
                    targetX = consumeB ? reagentBX : reagentAX;
                    targetY = consumeB ? reagentBY : reagentAY;
                }
                else if (!consumeB && _map.GetVoxel(reagentBX, reagentBY).Type != VoxelType.Empty)
                {
                    targetX = consumeA ? reagentAX : reagentBX;
                    targetY = consumeA ? reagentAY : reagentBY;
                }
                else
                {
                    targetX = reagentAX;
                    targetY = reagentAY;
                }

                if (_map.InBounds(targetX, targetY))
                {
                    _map.SetVoxel(targetX, targetY, rule.PrimaryProduct, false);
                    _map.SetVoxelTemperature(targetX, targetY, a.Temperature + rule.HeatRelease * 0.3f);
                }
            }

            if (rule.SecondaryProduct != VoxelType.Empty)
            {
                int sx, sy;
                if (rule.PrimaryProduct == VoxelType.Empty)
                {
                    sx = reagentAX;
                    sy = reagentAY;
                }
                else
                {
                    int tryX = reagentBX;
                    int tryY = reagentBY;
                    if (_map.InBounds(tryX, tryY) && _map.GetVoxel(tryX, tryY).IsEmpty)
                    {
                        sx = tryX;
                        sy = tryY;
                    }
                    else
                    {
                        sx = FindEmptyNeighbor(reagentAX, reagentAY);
                        sy = -1;
                        if (sx == -1)
                        {
                            sx = FindEmptyNeighbor(reagentBX, reagentBY);
                        }
                    }
                }

                if (sx >= 0 && sy >= 0 && _map.InBounds(sx, sy))
                {
                    VoxelCategory secCat = VoxelProperties.GetCategory(rule.SecondaryProduct);
                    Voxel current = _map.GetVoxel(sx, sy);
                    if (current.IsEmpty || (secCat == VoxelCategory.Liquid && current.IsGas) || (secCat == VoxelCategory.Solid && current.IsGas))
                    {
                        _map.SetVoxel(sx, sy, rule.SecondaryProduct, false);
                        _map.SetVoxelTemperature(sx, sy, a.Temperature + rule.HeatRelease * 0.2f);
                    }
                }
            }

            if (rule.GasProduct != VoxelType.Empty)
            {
                SpawnGasNearby(reagentAX, reagentAY, rule.GasProduct, a.Temperature + rule.HeatRelease * 0.5f);
            }

            if (rule.Type == ReactionType.Combustion)
            {
                IgniteNeighbors(reagentAX, reagentAY);
            }

            TransferHeat(reagentAX, reagentAY, rule.HeatRelease);

            RecordActiveReaction(reagentAX, reagentAY, rule);
        }

        private int FindEmptyNeighbor(int x, int y)
        {
            int[] dx = { 0, 0, 1, -1, 1, -1, 1, -1 };
            int[] dy = { 1, -1, 0, 0, 1, 1, -1, -1 };
            for (int i = 0; i < dx.Length; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (_map.InBounds(nx, ny) && _map.GetVoxel(nx, ny).IsEmpty)
                {
                    return nx;
                }
            }
            return -1;
        }

        private void SpawnGasNearby(int x, int y, VoxelType gasType, float temperature)
        {
            int[] dx = { 0, 0, 1, -1, 0, 1, -1, 1, -1 };
            int[] dy = { 1, -1, 0, 0, 0, 1, 1, -1, -1 };

            int startIdx = Random.Range(0, 4);
            for (int i = 0; i < dx.Length; i++)
            {
                int idx = (startIdx + i) % dx.Length;
                int nx = x + dx[idx];
                int ny = y + dy[idx];
                if (!_map.InBounds(nx, ny)) continue;

                Voxel v = _map.GetVoxel(nx, ny);
                if (v.IsEmpty || v.IsGas)
                {
                    _map.SetVoxel(nx, ny, gasType, false);
                    _map.SetVoxelTemperature(nx, ny, temperature);
                    return;
                }
            }
        }

        private void IgniteNeighbors(int x, int y)
        {
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (!_map.InBounds(nx, ny)) continue;

                Voxel v = _map.GetVoxel(nx, ny);
                if (IsFlammable(v.Type) && Random.value < 0.25f)
                {
                    TriggerCombustion(nx, ny);
                }
            }
        }

        private void TransferHeat(int x, int y, float delta)
        {
            if (Mathf.Abs(delta) < 1f) return;

            int range = (int)heatTransferRange;
            for (int ox = -range; ox <= range; ox++)
            {
                for (int oy = -range; oy <= range; oy++)
                {
                    if (ox == 0 && oy == 0) continue;
                    int nx = x + ox;
                    int ny = y + oy;
                    if (!_map.InBounds(nx, ny)) continue;
                    Voxel v = _map.GetVoxel(nx, ny);
                    if (v.IsEmpty) continue;
                    float dist = Mathf.Sqrt(ox * ox + oy * oy);
                    float amount = delta / (dist * dist * 2f);
                    _map.ChangeVoxelTemperature(nx, ny, amount);
                }
            }
        }

        private void RecordActiveReaction(int x, int y, ReactionRule rule)
        {
            ActiveReaction ar = new ActiveReaction
            {
                X = x,
                Y = y,
                Rule = rule,
                Intensity = Mathf.Abs(rule.HeatRelease) > 0 ? Mathf.Clamp01(Mathf.Abs(rule.HeatRelease) / 400f) : 0.3f
            };
            _activeReactions.Add(ar);
            if (_activeReactions.Count > 500)
            {
                _activeReactions.RemoveAt(0);
            }
        }

        public List<ActiveReaction> GetRecentActiveReactions()
        {
            return _activeReactions;
        }
    }
}
