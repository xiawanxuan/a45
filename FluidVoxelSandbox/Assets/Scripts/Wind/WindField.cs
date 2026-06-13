using UnityEngine;
using FluidVoxelSandbox.Core;

namespace FluidVoxelSandbox.Wind
{
    public class WindField : MonoBehaviour
    {
        private Vector2[,] _windVectors;
        private float[,] _windStrength;
        private Vector2[,] _turbulence;

        public int Width { get; private set; }
        public int Height { get; private set; }

        [Header("Global Wind")]
        public Vector2 globalDirection = new Vector2(1f, 0f);
        public float globalSpeed = 0.5f;

        [Header("Turbulence")]
        public float turbulenceScale = 0.05f;
        public float turbulenceStrength = 0.3f;
        public float turbulenceSpeed = 0.5f;

        [Header("Vertical")]
        public float verticalWindFactor = 0.1f;
        public float buoyancyStrength = 0.2f;

        [Header("Noise")]
        public float noiseScale = 0.03f;
        public float noiseSpeed = 0.2f;

        private float _turbulenceTime;
        private float _noiseTime;
        private System.Random _random;

        public void Initialize(int width, int height)
        {
            Width = width;
            Height = height;
            _windVectors = new Vector2[width, height];
            _windStrength = new float[width, height];
            _turbulence = new Vector2[width, height];
            _random = new System.Random(42);
            UpdateAllWindVectors();
        }

        public void UpdateWindField(float dt)
        {
            if (_windVectors == null) return;

            _turbulenceTime += dt * turbulenceSpeed;
            _noiseTime += dt * noiseSpeed;

            UpdateAllWindVectors();
        }

        private void UpdateAllWindVectors()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Vector2 wind = CalculateWindAt(x, y);
                    _windVectors[x, y] = wind;
                    _windStrength[x, y] = wind.magnitude;
                }
            }
        }

        private Vector2 CalculateWindAt(int x, int y)
        {
            Vector2 result = Vector2.zero;

            result += globalDirection.normalized * globalSpeed;

            float heightFactor = (float)y / Height;
            float heightWind = Mathf.Pow(heightFactor, 0.7f);
            result *= (0.5f + heightWind * 0.7f);

            float noiseX = Mathf.PerlinNoise(x * noiseScale + _noiseTime, y * noiseScale * 0.5f);
            float noiseY = Mathf.PerlinNoise(x * noiseScale * 0.5f + 100f, y * noiseScale + _noiseTime);
            Vector2 noiseWind = new Vector2(noiseX - 0.5f, noiseY - 0.5f) * 2f * turbulenceStrength;
            result += noiseWind;

            float turbX = Mathf.PerlinNoise(x * turbulenceScale + _turbulenceTime + 500f, y * turbulenceScale);
            float turbY = Mathf.PerlinNoise(x * turbulenceScale, y * turbulenceScale + _turbulenceTime + 500f);
            Vector2 turbWind = new Vector2(turbX - 0.5f, turbY - 0.5f) * 2f * turbulenceStrength * 0.5f;
            result += turbWind;

            float verticalBias = Mathf.PerlinNoise(x * 0.02f + _noiseTime * 0.3f, 123.456f) - 0.5f;
            result.y += verticalBias * verticalWindFactor;

            return result;
        }

        public Vector2 GetWindAt(int x, int y)
        {
            if (_windVectors == null) return globalDirection * globalSpeed;
            x = Mathf.Clamp(x, 0, Width - 1);
            y = Mathf.Clamp(y, 0, Height - 1);
            return _windVectors[x, y];
        }

        public float GetWindStrength(int x, int y)
        {
            if (_windStrength == null) return globalSpeed;
            x = Mathf.Clamp(x, 0, Width - 1);
            y = Mathf.Clamp(y, 0, Height - 1);
            return _windStrength[x, y];
        }

        public Vector2 GetWindWithBuoyancy(int x, int y, float density)
        {
            Vector2 wind = GetWindAt(x, y);
            float heightFactor = (float)y / Height;
            float buoyancy = (1f - density) * buoyancyStrength * (0.5f + heightFactor);
            wind.y += buoyancy;
            return wind;
        }

        public Vector2 GetInterpolatedWind(Vector2 worldPos, float voxelSize)
        {
            if (_windVectors == null) return globalDirection * globalSpeed;

            float fx = worldPos.x / voxelSize;
            float fy = worldPos.y / voxelSize;

            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            x0 = Mathf.Clamp(x0, 0, Width - 1);
            y0 = Mathf.Clamp(y0, 0, Height - 1);
            x1 = Mathf.Clamp(x1, 0, Width - 1);
            y1 = Mathf.Clamp(y1, 0, Height - 1);

            float tx = fx - Mathf.Floor(fx);
            float ty = fy - Mathf.Floor(fy);

            Vector2 c00 = _windVectors[x0, y0];
            Vector2 c10 = _windVectors[x1, y0];
            Vector2 c01 = _windVectors[x0, y1];
            Vector2 c11 = _windVectors[x1, y1];

            Vector2 c0 = Vector2.Lerp(c00, c10, tx);
            Vector2 c1 = Vector2.Lerp(c01, c11, tx);
            return Vector2.Lerp(c0, c1, ty);
        }

        public void ApplyLocalWindDisturbance(Vector2 worldPos, float voxelSize, Vector2 force, float radius)
        {
            if (_windVectors == null) return;

            int cx = Mathf.FloorToInt(worldPos.x / voxelSize);
            int cy = Mathf.FloorToInt(worldPos.y / voxelSize);
            int r = Mathf.CeilToInt(radius / voxelSize);

            for (int x = cx - r; x <= cx + r; x++)
            {
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (!InBounds(x, y)) continue;
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > r) continue;
                    float falloff = 1f - (dist / r);
                    falloff = Mathf.Pow(falloff, 1.5f);
                    _windVectors[x, y] += force * falloff;
                }
            }
        }

        public void SetGlobalWind(Vector2 direction, float speed)
        {
            globalDirection = direction.normalized;
            globalSpeed = Mathf.Clamp01(speed);
        }

        public Vector2 GetGlobalWind()
        {
            return globalDirection * globalSpeed;
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public byte[] Serialize()
        {
            return new byte[] {
                (byte)(globalDirection.x * 127 + 128),
                (byte)(globalDirection.y * 127 + 128),
                (byte)(globalSpeed * 255),
                (byte)(turbulenceStrength * 255)
            };
        }

        public void Deserialize(byte[] data)
        {
            if (data.Length < 4) return;
            globalDirection.x = (data[0] - 128) / 127f;
            globalDirection.y = (data[1] - 128) / 127f;
            globalDirection.Normalize();
            globalSpeed = data[2] / 255f;
            turbulenceStrength = data[3] / 255f;
        }
    }
}
