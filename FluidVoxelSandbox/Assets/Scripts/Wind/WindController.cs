using UnityEngine;
using FluidVoxelSandbox.Core;

namespace FluidVoxelSandbox.Wind
{
    public class WindController : MonoBehaviour
    {
        public WindField WindField { get; private set; }

        [Header("Display")]
        public bool showWindOverlay = false;
        public float overlayScale = 0.5f;
        public int overlayStep = 8;

        [Header("Direction Presets")]
        private Vector2[] _presets = new Vector2[]
        {
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
            new Vector2(-1, 1),
            new Vector2(-1, 0),
            new Vector2(-1, -1),
            new Vector2(0, -1),
            new Vector2(1, -1)
        };
        private int _currentPreset = 0;

        [Header("Keyboard Input")]
        public KeyCode cyclePresetKey = KeyCode.G;
        public KeyCode increaseSpeedKey = KeyCode.Equals;
        public KeyCode decreaseSpeedKey = KeyCode.Minus;
        public KeyCode toggleOverlayKey = KeyCode.H;
        public KeyCode randomWindKey = KeyCode.J;

        [Header("Gravity (for gas)")]
        public float gasGravityScale = -0.05f;
        public float liquidGravityScale = 1.0f;

        public void Initialize(WindField windField)
        {
            WindField = windField;
        }

        private void Update()
        {
            if (WindField == null) return;

            HandleKeyboardInput();
        }

        private void HandleKeyboardInput()
        {
            if (UnityEngine.Input.GetKeyDown(cyclePresetKey))
            {
                CyclePreset();
            }

            if (UnityEngine.Input.GetKey(increaseSpeedKey))
            {
                AdjustSpeed(0.5f * Time.deltaTime);
            }

            if (UnityEngine.Input.GetKey(decreaseSpeedKey))
            {
                AdjustSpeed(-0.5f * Time.deltaTime);
            }

            if (UnityEngine.Input.GetKeyDown(toggleOverlayKey))
            {
                showWindOverlay = !showWindOverlay;
            }

            if (UnityEngine.Input.GetKeyDown(randomWindKey))
            {
                SetRandomWind();
            }
        }

        public void SetWindDirection(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            WindField.globalDirection = dir;
        }

        public void SetWindSpeed(float speed)
        {
            WindField.globalSpeed = Mathf.Clamp01(speed);
        }

        public void AdjustSpeed(float delta)
        {
            WindField.globalSpeed = Mathf.Clamp01(WindField.globalSpeed + delta);
        }

        public void CyclePreset()
        {
            _currentPreset = (_currentPreset + 1) % _presets.Length;
            WindField.globalDirection = _presets[_currentPreset].normalized;
            Debug.Log($"Wind direction preset: {GetPresetName(_currentPreset)}");
        }

        public void SetRandomWind()
        {
            float angle = Random.value * 360f;
            SetWindDirection(angle);
            WindField.globalSpeed = Random.Range(0.2f, 0.8f);
            WindField.turbulenceStrength = Random.Range(0.1f, 0.5f);
            Debug.Log($"Random wind: {WindField.globalDirection} @ {WindField.globalSpeed:F2}");
        }

        private string GetPresetName(int index)
        {
            string[] names = { "East", "Northeast", "North", "Northwest",
                             "West", "Southwest", "South", "Southeast" };
            return index >= 0 && index < names.Length ? names[index] : "Unknown";
        }

        public void SetWindPreset(int index)
        {
            if (index >= 0 && index < _presets.Length)
            {
                _currentPreset = index;
                WindField.globalDirection = _presets[index].normalized;
            }
        }

        public int GetPresetCount()
        {
            return _presets.Length;
        }

        public float GetWindDirectionDegrees()
        {
            return Mathf.Atan2(WindField.globalDirection.y, WindField.globalDirection.x) * Mathf.Rad2Deg;
        }

        public byte[] Serialize()
        {
            float dir = GetWindDirectionDegrees();
            short dirShort = (short)dir;
            byte[] data = new byte[5];
            data[0] = (byte)(dirShort & 0xFF);
            data[1] = (byte)((dirShort >> 8) & 0xFF);
            data[2] = (byte)(WindField.globalSpeed * 255);
            data[3] = (byte)(WindField.turbulenceStrength * 255);
            data[4] = (byte)(gasGravityScale * 100 + 128);
            return data;
        }

        public void Deserialize(byte[] data)
        {
            if (data.Length < 5) return;
            short dirShort = (short)(data[0] | (data[1] << 8));
            SetWindDirection(dirShort);
            WindField.globalSpeed = data[2] / 255f;
            WindField.turbulenceStrength = data[3] / 255f;
            gasGravityScale = (data[4] - 128) / 100f;
        }
    }
}
