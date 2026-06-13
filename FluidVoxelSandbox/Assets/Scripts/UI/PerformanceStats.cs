using UnityEngine;
using System.Collections.Generic;
using FluidVoxelSandbox.Core;

namespace FluidVoxelSandbox.UI
{
    public class PerformanceStats : MonoBehaviour
    {
        public static PerformanceStats Instance { get; private set; }

        [Header("Settings")]
        public bool showStats = true;
        public float updateInterval = 0.5f;
        public int fontSize = 14;

        [Header("Display")]
        public Vector2 position = new Vector2(10f, 10f);

        private float _fps;
        private float _deltaTime;
        private int _frameCount;
        private float _accumulator;
        private float _lastUpdateTime;

        private string _statsText;
        private GUIStyle _style;
        private bool _styleInitialized;

        public float FPS => _fps;
        public float DeltaTime => _deltaTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            _frameCount++;
            _accumulator += Time.unscaledDeltaTime;

            if (Time.unscaledTime - _lastUpdateTime >= updateInterval)
            {
                _fps = _frameCount / _accumulator;
                _deltaTime = _accumulator / _frameCount;
                _frameCount = 0;
                _accumulator = 0f;
                _lastUpdateTime = Time.unscaledTime;
                UpdateStatsText();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                showStats = !showStats;
            }
        }

        private void UpdateStatsText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine($"FPS: {_fps:F1} ({_deltaTime * 1000f:F2}ms)");

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.FluidSimulation != null)
                {
                    sb.AppendLine($"Liquid cells: {GameManager.Instance.FluidSimulation.activeLiquidCells}");
                    sb.AppendLine($"Gas cells: {GameManager.Instance.FluidSimulation.activeGasCells}");
                }

                if (GameManager.Instance.WindField != null)
                {
                    Vector2 wind = GameManager.Instance.WindField.GetGlobalWind();
                    sb.AppendLine($"Wind: ({wind.x:F2}, {wind.y:F2}) speed: {wind.magnitude:F2}");
                }

                if (GameManager.Instance.VoxelMap != null)
                {
                    sb.AppendLine($"Map size: {GameManager.Instance.VoxelMap.Width}x{GameManager.Instance.VoxelMap.Height}");
                }
            }

            _statsText = sb.ToString();
        }

        private void OnGUI()
        {
            if (!showStats) return;

            if (!_styleInitialized)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = fontSize;
                _style.normal.textColor = Color.white;
                _style.fontStyle = FontStyle.Bold;
                _styleInitialized = true;
            }

            float width = 250f;
            float height = 200f;
            Rect bgRect = new Rect(position.x, position.y, width, height);

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(bgRect, GUIContent.none);
            GUI.backgroundColor = oldBg;

            Rect textRect = new Rect(position.x + 10f, position.y + 10f, width - 20f, height - 20f);
            GUI.Label(textRect, _statsText, _style);
        }
    }
}
