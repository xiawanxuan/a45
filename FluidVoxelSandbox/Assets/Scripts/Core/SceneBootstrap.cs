using UnityEngine;
using System.Collections;
using FluidVoxelSandbox.Core;
using FluidVoxelSandbox.Rendering;
using FluidVoxelSandbox.Terrain;
using FluidVoxelSandbox.Wind;
using FluidVoxelSandbox.Physics;
using FluidVoxelSandbox.Input;

namespace FluidVoxelSandbox.Core
{
    public class SceneBootstrap : MonoBehaviour
    {
        public static SceneBootstrap Instance { get; private set; }

        [Header("References")]
        public Camera mainCamera;
        public Material solidMaterial;
        public Material liquidMaterial;
        public Material gasMaterial;
        public Material debugMaterial;

        [Header("Settings")]
        public bool autoInitialize = true;
        public bool generateTerrainOnStart = true;
        public float cameraZoom = 100f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (autoInitialize)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            StartCoroutine(InitializeCoroutine());
        }

        private IEnumerator InitializeCoroutine()
        {
            yield return null;

            SetupCamera();
            yield return null;

            SetupGameManager();
            yield return null;

            SetupVoxelRenderer();
            yield return null;

            SetupWindSystem();
            yield return null;

            SetupPhysics();
            yield return null;

            SetupInput();
            yield return null;

            GenerateTerrain();
            yield return null;

            Debug.Log("Scene bootstrap complete!");
        }

        private void SetupCamera()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = cameraZoom;
            mainCamera.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.transform.position = new Vector3(128f, 128f, -10f);
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;
        }

        private void SetupGameManager()
        {
            if (GameManager.Instance == null)
            {
                GameObject gmObj = GameObject.Find("GameManager");
                if (gmObj == null)
                {
                    gmObj = new GameObject("GameManager");
                    gmObj.AddComponent<GameManager>();
                }
            }
        }

        private void SetupVoxelRenderer()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.VoxelRenderer == null)
            {
                GameObject rendererObj = new GameObject("VoxelRenderer");
                rendererObj.transform.SetParent(GameManager.Instance.transform);
                VoxelRenderer voxelRenderer = rendererObj.AddComponent<VoxelRenderer>();

                if (solidMaterial != null) voxelRenderer.solidMaterial = solidMaterial;
                if (liquidMaterial != null) voxelRenderer.liquidMaterial = liquidMaterial;
                if (gasMaterial != null) voxelRenderer.gasMaterial = gasMaterial;
                if (debugMaterial != null) voxelRenderer.debugMaterial = debugMaterial;
            }
        }

        private void SetupWindSystem()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.WindField == null || GameManager.Instance.WindController == null)
            {
                GameObject windObj = new GameObject("WindSystem");
                windObj.transform.SetParent(GameManager.Instance.transform);
                WindField windField = windObj.AddComponent<WindField>();
                WindController windController = windObj.AddComponent<WindController>();
            }
        }

        private void SetupPhysics()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.FluidSimulation == null || GameManager.Instance.VoxelCollision == null)
            {
                GameObject physicsObj = new GameObject("PhysicsSystem");
                physicsObj.transform.SetParent(GameManager.Instance.transform);
                FluidSimulation fluidSim = physicsObj.AddComponent<FluidSimulation>();
                VoxelCollision collision = physicsObj.AddComponent<VoxelCollision>();
            }
        }

        private void SetupInput()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.PlayerController == null)
            {
                GameObject inputObj = new GameObject("PlayerInput");
                inputObj.transform.SetParent(GameManager.Instance.transform);
                PlayerController playerCtrl = inputObj.AddComponent<PlayerController>();
            }
        }

        private void GenerateTerrain()
        {
            if (!generateTerrainOnStart) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.TerrainGenerator == null)
            {
                TerrainGenerator terrainGen = GameManager.Instance.gameObject.GetComponent<TerrainGenerator>();
                if (terrainGen == null)
                {
                    terrainGen = GameManager.Instance.gameObject.AddComponent<TerrainGenerator>();
                }
            }

            GameManager.Instance.RegenerateTerrain();
        }
    }
}
