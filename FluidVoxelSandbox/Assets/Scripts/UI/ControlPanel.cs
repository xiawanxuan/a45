using UnityEngine;
using FluidVoxelSandbox.Core;

namespace FluidVoxelSandbox.UI
{
    public class ControlPanel : MonoBehaviour
    {
        public static ControlPanel Instance { get; private set; }

        [Header("Settings")]
        public bool showPanel = true;
        public float panelWidth = 280f;

        [Header("Brush")]
        private VoxelType _selectedType = VoxelType.Stone;
        private int _brushRadius = 2;
        private bool _eraseMode = false;

        [Header("Wind")]
        private float _windSpeed = 0.5f;
        private float _windDirection = 0f;

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;
        private Vector2 _scrollPosition;

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
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                showPanel = !showPanel;
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 16;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = Color.white;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 12;
            _labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 11;

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!showPanel) return;

            InitStyles();

            float panelX = Screen.width - panelWidth - 10f;
            float panelY = 10f;
            float panelHeight = Screen.height - 20f;

            Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.92f);
            GUI.Box(panelRect, GUIContent.none);
            GUI.backgroundColor = oldBg;

            float contentX = panelX + 10f;
            float contentWidth = panelWidth - 20f;
            float y = panelY + 10f;

            GUI.Label(new Rect(contentX, y, contentWidth, 25f), "=== 流体沙盒控制面板 ===", _titleStyle);
            y += 35f;

            _scrollPosition = GUI.BeginScrollView(
                new Rect(contentX, y, contentWidth, panelHeight - 20f),
                _scrollPosition,
                new Rect(0, 0, contentWidth - 20f, 1200f));

            float localY = 0f;

            GUI.Label(new Rect(0, localY, contentWidth, 20f), "【绘制工具】", _titleStyle);
            localY += 25f;

            VoxelType[] solidTypes = { VoxelType.Stone, VoxelType.Dirt, VoxelType.Grass, VoxelType.Sand };
            GUI.Label(new Rect(0, localY, contentWidth, 18f), "固体:", _labelStyle);
            localY += 20f;
            for (int i = 0; i < solidTypes.Length; i++)
            {
                if (GUI.Button(new Rect(i * 60f, localY, 55f, 25f), solidTypes[i].ToString(), _buttonStyle))
                {
                    _selectedType = solidTypes[i];
                    _eraseMode = false;
                    UpdatePlayerController();
                }
            }
            localY += 30f;

            VoxelType[] solidReactiveTypes = { VoxelType.Obsidian, VoxelType.Glass, VoxelType.Ice, VoxelType.Mud, VoxelType.Salt, VoxelType.Concrete, VoxelType.Rust, VoxelType.Ash, VoxelType.Crystal };
            GUI.Label(new Rect(0, localY, contentWidth, 18f), "反应固体:", _labelStyle);
            localY += 20f;
            for (int i = 0; i < solidReactiveTypes.Length; i++)
            {
                float bx = (i % 4) * 60f;
                float by = (i / 4) * 28f;
                if (GUI.Button(new Rect(bx, localY + by, 55f, 25f), solidReactiveTypes[i].ToString(), _buttonStyle))
                {
                    _selectedType = solidReactiveTypes[i];
                    _eraseMode = false;
                    UpdatePlayerController();
                }
            }
            localY += Mathf.Ceil((float)solidReactiveTypes.Length / 4f) * 28f + 5f;

            VoxelType[] liquidTypes = { VoxelType.Water, VoxelType.Oil, VoxelType.Lava };
            GUI.Label(new Rect(0, localY, contentWidth, 18f), "液体:", _labelStyle);
            localY += 20f;
            for (int i = 0; i < liquidTypes.Length; i++)
            {
                if (GUI.Button(new Rect(i * 60f, localY, 55f, 25f), liquidTypes[i].ToString(), _buttonStyle))
                {
                    _selectedType = liquidTypes[i];
                    _eraseMode = false;
                    UpdatePlayerController();
                }
            }
            localY += 30f;

            VoxelType[] liquidReactiveTypes = { VoxelType.Acid, VoxelType.Lye, VoxelType.Alcohol };
            GUI.Label(new Rect(0, localY, contentWidth, 18f), "反应液体:", _labelStyle);
            localY += 20f;
            for (int i = 0; i < liquidReactiveTypes.Length; i++)
            {
                if (GUI.Button(new Rect(i * 60f, localY, 55f, 25f), liquidReactiveTypes[i].ToString(), _buttonStyle))
                {
                    _selectedType = liquidReactiveTypes[i];
                    _eraseMode = false;
                    UpdatePlayerController();
                }
            }
            localY += 30f;

            VoxelType[] gasTypes = { VoxelType.Smoke, VoxelType.Steam, VoxelType.ToxicGas };
            GUI.Label(new Rect(0, localY, contentWidth, 18f), "气体:", _labelStyle);
            localY += 20f;
            for (int i = 0; i < gasTypes.Length; i++)
            {
                if (GUI.Button(new Rect(i * 60f, localY, 55f, 25f), gasTypes[i].ToString(), _buttonStyle))
                {
                    _selectedType = gasTypes[i];
                    _eraseMode = false;
                    UpdatePlayerController();
                }
            }
            localY += 30f;

            VoxelType[] gasReactiveTypes = { VoxelType.Fire, VoxelType.Foam };
            GUI.Label(new Rect(0, localY, contentWidth, 18f), "反应气体:", _labelStyle);
            localY += 20f;
            for (int i = 0; i < gasReactiveTypes.Length; i++)
            {
                if (GUI.Button(new Rect(i * 60f, localY, 55f, 25f), gasReactiveTypes[i].ToString(), _buttonStyle))
                {
                    _selectedType = gasReactiveTypes[i];
                    _eraseMode = false;
                    UpdatePlayerController();
                }
            }
            localY += 30f;

            GUI.Label(new Rect(0, localY, contentWidth, 18f), $"笔刷大小: {_brushRadius}", _labelStyle);
            localY += 20f;
            _brushRadius = (int)GUI.HorizontalSlider(new Rect(0, localY, contentWidth - 60f, 20f), _brushRadius, 1, 20);
            if (GUI.Button(new Rect(contentWidth - 55f, localY - 2f, 50f, 22f), "重置", _buttonStyle))
            {
                _brushRadius = 2;
            }
            localY += 30f;

            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = _eraseMode ? new Color(1f, 0.3f, 0.3f) : oldBg;
            if (GUI.Button(new Rect(0, localY, contentWidth, 28f), _eraseMode ? "橡皮擦模式 (开启)" : "橡皮擦模式 (关闭)", _buttonStyle))
            {
                _eraseMode = !_eraseMode;
                UpdatePlayerController();
            }
            GUI.backgroundColor = oldColor;
            localY += 35f;

            GUI.Label(new Rect(0, localY, contentWidth, 20f), "【风场控制】", _titleStyle);
            localY += 25f;

            GUI.Label(new Rect(0, localY, contentWidth, 18f), $"风速: {_windSpeed:F2}", _labelStyle);
            localY += 20f;
            _windSpeed = GUI.HorizontalSlider(new Rect(0, localY, contentWidth - 60f, 20f), _windSpeed, 0f, 1f);
            if (GUI.Button(new Rect(contentWidth - 55f, localY - 2f, 50f, 22f), "重置", _buttonStyle))
            {
                _windSpeed = 0.5f;
            }
            localY += 30f;

            GUI.Label(new Rect(0, localY, contentWidth, 18f), $"风向: {_windDirection:F0}°", _labelStyle);
            localY += 20f;
            _windDirection = GUI.HorizontalSlider(new Rect(0, localY, contentWidth - 60f, 20f), _windDirection, 0f, 360f);
            if (GUI.Button(new Rect(contentWidth - 55f, localY - 2f, 50f, 22f), "随机", _buttonStyle))
            {
                _windDirection = Random.Range(0f, 360f);
            }
            localY += 5f;

            if (GUI.Button(new Rect(0, localY + 25f, contentWidth, 25f), "应用风场设置", _buttonStyle))
            {
                ApplyWindSettings();
            }
            localY += 55f;

            GUI.Label(new Rect(0, localY, contentWidth, 20f), "【模拟控制】", _titleStyle);
            localY += 25f;

            if (GUI.Button(new Rect(0, localY, contentWidth * 0.48f, 28f), "暂停/继续 (P)", _buttonStyle))
            {
                GameManager.Instance?.TogglePause();
            }
            if (GUI.Button(new Rect(contentWidth * 0.52f, localY, contentWidth * 0.48f, 28f), "重新生成地形 (R)", _buttonStyle))
            {
                GameManager.Instance?.RegenerateTerrain();
            }
            localY += 35f;

            if (GUI.Button(new Rect(0, localY, contentWidth * 0.48f, 28f), "快速存档 (F5)", _buttonStyle))
            {
                GameManager.Instance?.QuickSave();
            }
            if (GUI.Button(new Rect(contentWidth * 0.52f, localY, contentWidth * 0.48f, 28f), "快速读档 (F9)", _buttonStyle))
            {
                GameManager.Instance?.QuickLoad();
            }
            localY += 35f;

            if (GUI.Button(new Rect(0, localY, contentWidth * 0.48f, 28f), "风场可视化 (H)", _buttonStyle))
            {
                if (GameManager.Instance?.VoxelRenderer != null)
                {
                    GameManager.Instance.VoxelRenderer.ToggleWindOverlay();
                }
            }
            if (GUI.Button(new Rect(contentWidth * 0.52f, localY, contentWidth * 0.48f, 28f), "性能面板 (F3)", _buttonStyle))
            {
                if (PerformanceStats.Instance != null)
                {
                    PerformanceStats.Instance.showStats = !PerformanceStats.Instance.showStats;
                }
            }
            localY += 35f;

            if (GUI.Button(new Rect(0, localY, contentWidth, 28f), "全屏切换 (F11)", _buttonStyle))
            {
                GameManager.Instance?.ToggleFullscreen();
            }
            localY += 35f;

            GUI.Label(new Rect(0, localY, contentWidth, 20f), "【化合反应】", _titleStyle);
            localY += 25f;
            if (GameManager.Instance != null && GameManager.Instance.VoxelChemistry != null)
            {
                Color oldC2 = GUI.backgroundColor;
                GUI.backgroundColor = GameManager.Instance.VoxelChemistry.enableReactions ? new Color(0.3f, 0.8f, 0.4f) : oldBg;
                if (GUI.Button(new Rect(0, localY, contentWidth * 0.48f, 28f),
                    GameManager.Instance.VoxelChemistry.enableReactions ? "反应系统: 开" : "反应系统: 关", _buttonStyle))
                {
                    GameManager.Instance.VoxelChemistry.enableReactions = !GameManager.Instance.VoxelChemistry.enableReactions;
                }
                GUI.backgroundColor = oldC2;
                if (GUI.Button(new Rect(contentWidth * 0.52f, localY, contentWidth * 0.48f, 28f), $"本步反应: {GameManager.Instance.VoxelChemistry.reactionsThisStep}", _buttonStyle)) { }
                localY += 35f;
                GUI.Label(new Rect(0, localY, contentWidth, 18f), $"累计反应数: {GameManager.Instance.VoxelChemistry.totalReactions}", _labelStyle);
                localY += 25f;
            }
            localY += 15f;

            GUI.Label(new Rect(0, localY, contentWidth, 20f), "【操作说明】", _titleStyle);
            localY += 25f;

            string helpText =
                "鼠标左键: 绘制体素\n" +
                "鼠标右键: 吸取材质\n" +
                "Shift+左键: 擦除\n" +
                "滚轮: 缩放视角\n" +
                "中键拖动: 平移视角\n" +
                "WASD/方向键: 移动视角\n" +
                "[ / ]: 减小/增大笔刷\n" +
                "1-9: 快速选择材质\n" +
                "G: 切换风向预设\n" +
                "+ / -: 调整风速\n" +
                "H: 风场可视化开关\n" +
                "Tab: 显示/隐藏面板\n" +
                "\n常用反应组合:\n" +
                "熔岩+水 → 黑曜石+蒸汽\n" +
                "熔岩+沙 → 玻璃\n" +
                "酸+碱 → 盐+水\n" +
                "火+草 → 灰烬+烟\n" +
                "火+油 → 燃烧+烟\n" +
                "火+酒精 → 燃烧扩散\n" +
                "水+土 → 淤泥\n" +
                "酸+石头 → 盐\n" +
                "碱+油 → 泡沫";

            GUI.Label(new Rect(0, localY, contentWidth, 250f), helpText, _labelStyle);
            localY += 260f;

            GUI.EndScrollView();
        }

        private void UpdatePlayerController()
        {
            if (GameManager.Instance != null && GameManager.Instance.PlayerController != null)
            {
                GameManager.Instance.PlayerController.selectedVoxelType = _selectedType;
                GameManager.Instance.PlayerController.eraseMode = _eraseMode;
                GameManager.Instance.PlayerController.brushRadius = _brushRadius;
            }
        }

        private void ApplyWindSettings()
        {
            if (GameManager.Instance != null && GameManager.Instance.WindField != null)
            {
                float rad = _windDirection * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                GameManager.Instance.WindField.SetGlobalWind(dir, _windSpeed);
            }
        }
    }
}
