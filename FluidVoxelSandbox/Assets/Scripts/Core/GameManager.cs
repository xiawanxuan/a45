using UnityEngine;
using FluidVoxelSandbox.Terrain;
using FluidVoxelSandbox.Wind;
using FluidVoxelSandbox.Physics;
using FluidVoxelSandbox.Rendering;
using FluidVoxelSandbox.IO;
using FluidVoxelSandbox.Input;
using FluidVoxelSandbox.UI;

namespace FluidVoxelSandbox.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Map Settings")]
        public int mapWidth = 256;
        public int mapHeight = 256;
        public float voxelSize = 1f;

        [Header("Simulation Settings")]
        public float simulationSpeed = 1f;
        public int physicsStepsPerFrame = 2;
        public bool enableGravity = true;
        public bool enableWind = true;

        [Header("Performance")]
        public bool limitFPS = true;
        public int targetFPS = 60;

        public VoxelMap VoxelMap { get; private set; }
        public TerrainGenerator TerrainGenerator { get; private set; }
        public WindField WindField { get; private set; }
        public WindController WindController { get; private set; }
        public FluidSimulation FluidSimulation { get; private set; }
        public VoxelCollision VoxelCollision { get; private set; }
        public VoxelChemistry VoxelChemistry { get; private set; }
        public SaveSystem SaveSystem { get; private set; }
        public PlayerController PlayerController { get; private set; }
        public VoxelRenderer VoxelRenderer { get; private set; }
        public PerformanceStats PerformanceStats { get; private set; }
        public ControlPanel ControlPanel { get; private set; }

        public bool IsPaused { get; private set; }
        public float DeltaTime { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (limitFPS)
            {
                Application.targetFrameRate = targetFPS;
                QualitySettings.vSyncCount = 0;
            }
        }

        private void Start()
        {
            InitializeSystems();
            StartCoroutine(InitializeSystemsCoroutine());
        }

        private System.Collections.IEnumerator InitializeSystemsCoroutine()
        {
            yield return null;

            if (WindField != null)
            {
                WindField.Initialize(mapWidth, mapHeight);
            }

            if (WindController != null && WindField != null)
            {
                WindController.Initialize(WindField);
            }

            if (FluidSimulation != null)
            {
                FluidSimulation.Initialize(VoxelMap, WindField, WindController);
            }

            if (VoxelCollision != null)
            {
                VoxelCollision.Initialize(VoxelMap);
            }

            if (VoxelChemistry != null)
            {
                VoxelChemistry.Initialize(VoxelMap, VoxelCollision);
            }

            if (VoxelRenderer != null)
            {
                VoxelRenderer.Initialize(VoxelMap);
            }

            if (PlayerController != null)
            {
                PlayerController.Initialize(VoxelMap, Camera.main);
            }

            if (TerrainGenerator != null)
            {
                TerrainGenerator.Generate(VoxelMap);
                VoxelRenderer?.RebuildAllMeshes();
            }

            Debug.Log("All systems initialized");
        }

        private void InitializeSystems()
        {
            VoxelMap = new VoxelMap(mapWidth, mapHeight, voxelSize);

            TerrainGenerator = gameObject.GetComponent<TerrainGenerator>()
                ?? gameObject.AddComponent<TerrainGenerator>();

            Transform windParent = transform.Find("WindSystem");
            if (windParent == null)
            {
                GameObject windObj = new GameObject("WindSystem");
                windObj.transform.SetParent(transform);
                windParent = windObj.transform;
            }
            WindField = windParent.GetComponent<WindField>() ?? windParent.gameObject.AddComponent<WindField>();
            WindController = windParent.GetComponent<WindController>() ?? windParent.gameObject.AddComponent<WindController>();

            Transform physicsParent = transform.Find("PhysicsSystem");
            if (physicsParent == null)
            {
                GameObject physicsObj = new GameObject("PhysicsSystem");
                physicsObj.transform.SetParent(transform);
                physicsParent = physicsObj.transform;
            }
            FluidSimulation = physicsParent.GetComponent<FluidSimulation>() ?? physicsParent.gameObject.AddComponent<FluidSimulation>();
            VoxelCollision = physicsParent.GetComponent<VoxelCollision>() ?? physicsParent.gameObject.AddComponent<VoxelCollision>();
            VoxelChemistry = physicsParent.GetComponent<VoxelChemistry>() ?? physicsParent.gameObject.AddComponent<VoxelChemistry>();

            Transform rendererParent = transform.Find("VoxelRenderer");
            if (rendererParent == null)
            {
                GameObject rendererObj = new GameObject("VoxelRenderer");
                rendererObj.transform.SetParent(transform);
                rendererParent = rendererObj.transform;
            }
            VoxelRenderer = rendererParent.GetComponent<VoxelRenderer>() ?? rendererParent.gameObject.AddComponent<VoxelRenderer>();

            Transform inputParent = transform.Find("PlayerInput");
            if (inputParent == null)
            {
                GameObject inputObj = new GameObject("PlayerInput");
                inputObj.transform.SetParent(transform);
                inputParent = inputObj.transform;
            }
            PlayerController = inputParent.GetComponent<PlayerController>() ?? inputParent.gameObject.AddComponent<PlayerController>();

            Transform uiParent = transform.Find("UISystem");
            if (uiParent == null)
            {
                GameObject uiObj = new GameObject("UISystem");
                uiObj.transform.SetParent(transform);
                uiParent = uiObj.transform;
            }
            PerformanceStats = uiParent.GetComponent<PerformanceStats>() ?? uiParent.gameObject.AddComponent<PerformanceStats>();
            ControlPanel = uiParent.GetComponent<ControlPanel>() ?? uiParent.gameObject.AddComponent<ControlPanel>();

            SaveSystem = new SaveSystem();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                ToggleFullscreen();
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                QuickSave();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                QuickLoad();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RegenerateTerrain();
            }

            DeltaTime = Time.deltaTime * simulationSpeed;
        }

        private void FixedUpdate()
        {
            if (IsPaused) return;

            float stepDt = DeltaTime / physicsStepsPerFrame;
            for (int i = 0; i < physicsStepsPerFrame; i++)
            {
                WindField?.UpdateWindField(stepDt);
                FluidSimulation?.Step(stepDt);
                VoxelCollision?.CheckCollisions(stepDt);
                VoxelChemistry?.StepChemistry(stepDt);
            }
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            Debug.Log($"Simulation {(IsPaused ? "Paused" : "Resumed")}");
        }

        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            Debug.Log($"Fullscreen: {Screen.fullScreen}");
        }

        public void SetSimulationSpeed(float speed)
        {
            simulationSpeed = Mathf.Clamp(speed, 0f, 5f);
            Debug.Log($"Simulation Speed: {simulationSpeed:F1}x");
        }

        public void RegenerateTerrain()
        {
            VoxelMap.Clear();
            TerrainGenerator?.Generate(VoxelMap);
            VoxelRenderer?.RebuildAllMeshes();
            Debug.Log("Terrain regenerated");
        }

        public void ClearMap()
        {
            VoxelMap.Clear();
            VoxelRenderer?.RebuildAllMeshes();
            Debug.Log("Map cleared");
        }

        public void QuickSave()
        {
            string path = SaveSystem.Save("quicksave", VoxelMap, WindField, WindController);
            Debug.Log($"Quick save: {path}");
        }

        public void QuickLoad()
        {
            if (SaveSystem.Load("quicksave", VoxelMap, WindField, WindController))
            {
                VoxelRenderer?.RebuildAllMeshes();
                Debug.Log("Quick load complete");
            }
            else
            {
                Debug.LogWarning("No quick save found");
            }
        }

        public void SaveGame(string saveName)
        {
            string path = SaveSystem.Save(saveName, VoxelMap, WindField, WindController);
            Debug.Log($"Saved: {path}");
        }

        public bool LoadGame(string saveName)
        {
            if (SaveSystem.Load(saveName, VoxelMap, WindField, WindController))
            {
                VoxelRenderer?.RebuildAllMeshes();
                Debug.Log($"Loaded: {saveName}");
                return true;
            }
            Debug.LogWarning($"Save not found: {saveName}");
            return false;
        }

        public string[] GetSaveList()
        {
            return SaveSystem.GetSaveList();
        }

        public void DeleteSave(string saveName)
        {
            SaveSystem.DeleteSave(saveName);
        }
    }
}
