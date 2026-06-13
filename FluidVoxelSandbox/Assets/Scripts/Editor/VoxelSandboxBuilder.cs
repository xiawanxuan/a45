#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using FluidVoxelSandbox.Core;
using FluidVoxelSandbox.Rendering;
using FluidVoxelSandbox.Terrain;
using FluidVoxelSandbox.Wind;
using FluidVoxelSandbox.Physics;
using FluidVoxelSandbox.Input;

namespace FluidVoxelSandbox.EditorTools
{
    public static class VoxelSandboxBuilder
    {
        private const string ShadersFolder = "Assets/Shaders";
        private const string MaterialsFolder = "Assets/Materials";

        [MenuItem("Voxel Sandbox/Generate All Materials")]
        public static void GenerateAllMaterials()
        {
            if (!Directory.Exists(MaterialsFolder))
            {
                Directory.CreateDirectory(MaterialsFolder);
            }

            CreateMaterial("VoxelSolid", "Voxel/Solid", MaterialsFolder);
            CreateMaterial("VoxelLiquid", "Voxel/Liquid", MaterialsFolder);
            CreateMaterial("VoxelGas", "Voxel/Gas", MaterialsFolder);
            CreateMaterial("VoxelDebug", "Voxel/Debug", MaterialsFolder);

            AssetDatabase.Refresh();
            Debug.Log("All materials generated!");
        }

        [MenuItem("Voxel Sandbox/Setup Main Scene")]
        public static void SetupMainScene()
        {
            GenerateAllMaterials();

            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 100f;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.transform.position = new Vector3(128f, 128f, -10f);
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;

            Light light = Object.FindObjectOfType<Light>();
            if (light == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.95f, 0.85f);
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            Material solidMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/VoxelSolid.mat");
            Material liquidMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/VoxelLiquid.mat");
            Material gasMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/VoxelGas.mat");
            Material debugMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/VoxelDebug.mat");

            if (gameManager.VoxelRenderer == null)
            {
                GameObject rendererObj = new GameObject("VoxelRenderer");
                rendererObj.transform.SetParent(gameManagerObj.transform);
                VoxelRenderer voxelRenderer = rendererObj.AddComponent<VoxelRenderer>();
                voxelRenderer.solidMaterial = solidMat;
                voxelRenderer.liquidMaterial = liquidMat;
                voxelRenderer.gasMaterial = gasMat;
                voxelRenderer.debugMaterial = debugMat;
            }

            TerrainGenerator terrainGen = gameManagerObj.GetComponent<TerrainGenerator>();
            if (terrainGen == null)
            {
                terrainGen = gameManagerObj.AddComponent<TerrainGenerator>();
            }

            EditorUtility.SetDirty(gameManagerObj);
            AssetDatabase.SaveAssets();

            Debug.Log("Main scene setup complete!");
        }

        private static void CreateMaterial(string name, string shaderName, string folder)
        {
            string path = Path.Combine(folder, name + ".mat");

            if (File.Exists(path))
            {
                Debug.Log($"Material already exists: {path}");
                return;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"Shader not found: {shaderName}");
                return;
            }

            Material mat = new Material(shader);

            switch (name)
            {
                case "VoxelSolid":
                    mat.SetFloat("_PixelSnap", 1f);
                    break;
                case "VoxelLiquid":
                    mat.SetFloat("_Transparency", 0.75f);
                    mat.SetFloat("_WaveSpeed", 2f);
                    mat.SetFloat("_WaveAmplitude", 0.03f);
                    break;
                case "VoxelGas":
                    mat.SetFloat("_Transparency", 0.5f);
                    mat.SetFloat("_PulseSpeed", 1.5f);
                    mat.SetFloat("_PulseAmount", 0.25f);
                    mat.SetFloat("_NoiseScale", 0.08f);
                    break;
                case "VoxelDebug":
                    mat.SetColor("_Color", Color.white);
                    break;
            }

            AssetDatabase.CreateAsset(mat, path);
            Debug.Log($"Created material: {path}");
        }
    }
}
#endif
