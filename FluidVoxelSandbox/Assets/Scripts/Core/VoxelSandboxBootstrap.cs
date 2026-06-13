using UnityEngine;
using System.Collections;
using FluidVoxelSandbox.Core;
using FluidVoxelSandbox.Rendering;
using FluidVoxelSandbox.Terrain;
using FluidVoxelSandbox.Wind;
using FluidVoxelSandbox.Physics;
using FluidVoxelSandbox.Input;
using FluidVoxelSandbox.UI;

namespace FluidVoxelSandbox.Core
{
    [AddComponentMenu("Voxel Sandbox/Voxel Sandbox Bootstrap")]
    public class VoxelSandboxBootstrap : MonoBehaviour
    {
        [Header("Map Settings")]
        public int mapWidth = 256;
        public int mapHeight = 256;
        public float voxelSize = 1f;

        [Header("Simulation")]
        public bool enableGravity = true;
        public bool enableWind = true;
        public float simulationSpeed = 1f;

        [Header("Terrain")]
        public bool generateTerrainOnStart = true;
        public TerrainGenerator.TerrainType terrainType = TerrainGenerator.TerrainType.Hills;
        public int terrainSeed = 12345;

        [Header("Camera")]
        public float cameraZoom = 100f;
        public Color backgroundColor = new Color(0.08f, 0.08f, 0.12f);

        [Header("UI")]
        public bool showControlPanel = true;
        public bool showPerformanceStats = true;

        [Header("Materials")]
        public Material solidMaterial;
        public Material liquidMaterial;
        public Material gasMaterial;
        public Material debugMaterial;

        private void Start()
        {
            StartCoroutine(SetupCoroutine());
        }

        private IEnumerator SetupCoroutine()
        {
            yield return null;
            SetupCamera();
            yield return null;
            SetupGameManager();
            yield return null;
            SetupRendering();
            yield return null;
            SetupWind();
            yield return null;
            SetupPhysics();
            yield return null;
            SetupInput();
            yield return null;
            SetupUI();
            yield return null;
            SetupTerrain();
            yield return null;
            InitializeAllSystems();
            yield return null;
            Debug.Log("Voxel Sandbox initialized successfully!");
            Debug.Log("Press Tab to toggle control panel, F3 for performance stats");
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = cameraZoom;
            cam.backgroundColor = backgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(mapWidth * voxelSize * 0.5f, mapHeight * voxelSize * 0.5f, -10f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
        }

        private void SetupGameManager()
        {
            if (GameManager.Instance != null) return;

            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj == null)
            {
                gmObj = new GameObject("GameManager");
            }

            GameManager gm = gmObj.GetComponent<GameManager>();
            if (gm == null)
            {
                gm = gmObj.AddComponent<GameManager>();
            }

            gm.mapWidth = mapWidth;
            gm.mapHeight = mapHeight;
            gm.voxelSize = voxelSize;
            gm.enableGravity = enableGravity;
            gm.enableWind = enableWind;
            gm.simulationSpeed = simulationSpeed;
        }

        private void SetupRendering()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.VoxelRenderer != null) return;

            GameObject rendererObj = new GameObject("VoxelRenderer");
            rendererObj.transform.SetParent(GameManager.Instance.transform);
            VoxelRenderer voxelRenderer = rendererObj.AddComponent<VoxelRenderer>();

            voxelRenderer.solidMaterial = solidMaterial;
            voxelRenderer.liquidMaterial = liquidMaterial;
            voxelRenderer.gasMaterial = gasMaterial;
            voxelRenderer.debugMaterial = debugMaterial;
            voxelRenderer.chunkSize = 32;
            voxelRenderer.rebuildOnChange = true;
            voxelRenderer.rebuildInterval = 0.05f;
        }

        private void SetupWind()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.WindField != null && GameManager.Instance.WindController != null) return;

            GameObject windObj = new GameObject("WindSystem");
            windObj.transform.SetParent(GameManager.Instance.transform);
            WindField windField = windObj.AddComponent<WindField>();
            WindController windController = windObj.AddComponent<WindController>();
        }

        private void SetupPhysics()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.FluidSimulation != null && GameManager.Instance.VoxelCollision != null) return;

            GameObject physicsObj = new GameObject("PhysicsSystem");
            physicsObj.transform.SetParent(GameManager.Instance.transform);
            FluidSimulation fluidSim = physicsObj.AddComponent<FluidSimulation>();
            VoxelCollision collision = physicsObj.AddComponent<VoxelCollision>();
        }

        private void SetupInput()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.PlayerController != null) return;

            GameObject inputObj = new GameObject("PlayerInput");
            inputObj.transform.SetParent(GameManager.Instance.transform);
            PlayerController playerCtrl = inputObj.AddComponent<PlayerController>();
        }

        private void SetupUI()
        {
            if (GameManager.Instance == null) return;

            GameObject uiObj = new GameObject("UISystem");
            uiObj.transform.SetParent(GameManager.Instance.transform);

            PerformanceStats stats = uiObj.AddComponent<PerformanceStats>();
            stats.showStats = showPerformanceStats;

            ControlPanel panel = uiObj.AddComponent<ControlPanel>();
            panel.showPanel = showControlPanel;
        }

        private void SetupTerrain()
        {
            if (GameManager.Instance == null) return;

            TerrainGenerator terrainGen = GameManager.Instance.gameObject.GetComponent<TerrainGenerator>();
            if (terrainGen == null)
            {
                terrainGen = GameManager.Instance.gameObject.AddComponent<TerrainGenerator>();
            }

            terrainGen.terrainType = terrainType;
            terrainGen.terrainSeed = terrainSeed;
        }

        private void InitializeAllSystems()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.VoxelMap == null) return;

            int w = GameManager.Instance.mapWidth;
            int h = GameManager.Instance.mapHeight;

            if (GameManager.Instance.WindField != null)
            {
                GameManager.Instance.WindField.Initialize(w, h);
            }

            if (GameManager.Instance.WindController != null && GameManager.Instance.WindField != null)
            {
                GameManager.Instance.WindController.Initialize(GameManager.Instance.WindField);
            }

            if (GameManager.Instance.FluidSimulation != null)
            {
                GameManager.Instance.FluidSimulation.Initialize(
                    GameManager.Instance.VoxelMap,
                    GameManager.Instance.WindField,
                    GameManager.Instance.WindController);
            }

            if (GameManager.Instance.VoxelCollision != null)
            {
                GameManager.Instance.VoxelCollision.Initialize(GameManager.Instance.VoxelMap);
            }

            if (GameManager.Instance.VoxelChemistry != null)
            {
                GameManager.Instance.VoxelChemistry.Initialize(
                    GameManager.Instance.VoxelMap,
                    GameManager.Instance.VoxelCollision);
            }

            if (GameManager.Instance.VoxelRenderer != null)
            {
                GameManager.Instance.VoxelRenderer.Initialize(GameManager.Instance.VoxelMap);
            }

            if (GameManager.Instance.PlayerController != null)
            {
                GameManager.Instance.PlayerController.Initialize(
                    GameManager.Instance.VoxelMap,
                    Camera.main);
            }

            if (generateTerrainOnStart && GameManager.Instance.TerrainGenerator != null)
            {
                GameManager.Instance.TerrainGenerator.Generate(GameManager.Instance.VoxelMap);
                GameManager.Instance.VoxelRenderer?.RebuildAllMeshes();
            }
        }
    }
}
