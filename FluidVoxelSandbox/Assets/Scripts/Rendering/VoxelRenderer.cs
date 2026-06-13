using UnityEngine;
using System.Collections.Generic;
using FluidVoxelSandbox.Core;
using FluidVoxelSandbox.Wind;

namespace FluidVoxelSandbox.Rendering
{
    public class VoxelRenderer : MonoBehaviour
    {
        private VoxelMap _map;

        [Header("Render Settings")]
        public Material solidMaterial;
        public Material liquidMaterial;
        public Material gasMaterial;
        public Material debugMaterial;

        [Header("Chunk Settings")]
        public int chunkSize = 32;
        public bool rebuildOnChange = true;
        public float rebuildInterval = 0.05f;

        [Header("Animation")]
        public bool enableLiquidAnimation = true;
        public bool enableGasAnimation = true;
        public float liquidAnimationInterval = 0.05f;
        public float gasAnimationInterval = 0.08f;

        [Header("Wind Overlay")]
        public bool showWindOverlay = false;
        public int windOverlayStep = 8;
        public Color windArrowColor = new Color(1f, 1f, 1f, 0.6f);

        private GameObject _solidContainer;
        private GameObject _liquidContainer;
        private GameObject _gasContainer;
        private GameObject _debugContainer;

        private VoxelMeshBuilder.MeshData _solidMeshData;
        private VoxelMeshBuilder.MeshData _liquidMeshData;
        private VoxelMeshBuilder.MeshData _gasMeshData;

        private MeshFilter[,] _solidChunkFilters;
        private MeshFilter[,] _liquidChunkFilters;
        private MeshFilter[,] _gasChunkFilters;

        private bool[,] _dirtyChunks;
        private int _chunkCols;
        private int _chunkRows;

        private float _rebuildTimer;
        private float _time;
        private float _liquidAnimTimer;
        private float _gasAnimTimer;

        public void Initialize(VoxelMap map)
        {
            _map = map;
            _time = 0f;

            _chunkCols = Mathf.CeilToInt((float)map.Width / chunkSize);
            _chunkRows = Mathf.CeilToInt((float)map.Height / chunkSize);

            _dirtyChunks = new bool[_chunkCols, _chunkRows];

            _solidMeshData = new VoxelMeshBuilder.MeshData();
            _liquidMeshData = new VoxelMeshBuilder.MeshData();
            _gasMeshData = new VoxelMeshBuilder.MeshData();

            _solidMeshData.Init();
            _liquidMeshData.Init();
            _gasMeshData.Init();

            CreateContainers();
            CreateChunkMeshes();
            MarkAllChunksDirty();
            RebuildAllMeshes();

            _map.OnVoxelChanged += OnVoxelChanged;
            _map.OnMapResized += OnMapResized;
        }

        private void CreateContainers()
        {
            if (_solidContainer != null) Destroy(_solidContainer);
            if (_liquidContainer != null) Destroy(_liquidContainer);
            if (_gasContainer != null) Destroy(_gasContainer);
            if (_debugContainer != null) Destroy(_debugContainer);

            _solidContainer = new GameObject("SolidChunks");
            _solidContainer.transform.SetParent(transform);

            _liquidContainer = new GameObject("LiquidChunks");
            _liquidContainer.transform.SetParent(transform);

            _gasContainer = new GameObject("GasChunks");
            _gasContainer.transform.SetParent(transform);

            _debugContainer = new GameObject("DebugOverlay");
            _debugContainer.transform.SetParent(transform);
        }

        private void CreateChunkMeshes()
        {
            _solidChunkFilters = new MeshFilter[_chunkCols, _chunkRows];
            _liquidChunkFilters = new MeshFilter[_chunkCols, _chunkRows];
            _gasChunkFilters = new MeshFilter[_chunkCols, _chunkRows];

            for (int cx = 0; cx < _chunkCols; cx++)
            {
                for (int cy = 0; cy < _chunkRows; cy++)
                {
                    _solidChunkFilters[cx, cy] = CreateChunkObject(
                        $"Solid_{cx}_{cy}", _solidContainer.transform, solidMaterial);
                    _liquidChunkFilters[cx, cy] = CreateChunkObject(
                        $"Liquid_{cx}_{cy}", _liquidContainer.transform, liquidMaterial);
                    _gasChunkFilters[cx, cy] = CreateChunkObject(
                        $"Gas_{cx}_{cy}", _gasContainer.transform, gasMaterial);
                }
            }
        }

        private MeshFilter CreateChunkObject(string name, Transform parent, Material mat)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;

            MeshFilter filter = obj.AddComponent<MeshFilter>();
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.material = mat;

            return filter;
        }

        private void OnDestroy()
        {
            if (_map != null)
            {
                _map.OnVoxelChanged -= OnVoxelChanged;
                _map.OnMapResized -= OnMapResized;
            }
        }

        private void OnVoxelChanged(int x, int y, VoxelType type)
        {
            if (!rebuildOnChange) return;
            MarkChunkDirty(x, y);
        }

        private void OnMapResized()
        {
            _chunkCols = Mathf.CeilToInt((float)_map.Width / chunkSize);
            _chunkRows = Mathf.CeilToInt((float)_map.Height / chunkSize);
            _dirtyChunks = new bool[_chunkCols, _chunkRows];
            CreateContainers();
            CreateChunkMeshes();
            MarkAllChunksDirty();
            RebuildAllMeshes();
        }

        public void MarkChunkDirty(int x, int y)
        {
            int cx = x / chunkSize;
            int cy = y / chunkSize;
            cx = Mathf.Clamp(cx, 0, _chunkCols - 1);
            cy = Mathf.Clamp(cy, 0, _chunkRows - 1);
            _dirtyChunks[cx, cy] = true;

            if (cx > 0) _dirtyChunks[cx - 1, cy] = true;
            if (cx < _chunkCols - 1) _dirtyChunks[cx + 1, cy] = true;
            if (cy > 0) _dirtyChunks[cx, cy - 1] = true;
            if (cy < _chunkRows - 1) _dirtyChunks[cx, cy + 1] = true;
        }

        public void MarkAllChunksDirty()
        {
            for (int cx = 0; cx < _chunkCols; cx++)
                for (int cy = 0; cy < _chunkRows; cy++)
                    _dirtyChunks[cx, cy] = true;
        }

        public void RebuildAllMeshes()
        {
            if (_map == null) return;
            _time = Time.time;

            for (int cx = 0; cx < _chunkCols; cx++)
            {
                for (int cy = 0; cy < _chunkRows; cy++)
                {
                    RebuildChunk(cx, cy);
                }
            }

            _dirtyChunks = new bool[_chunkCols, _chunkRows];
            _rebuildTimer = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _time += dt;

            _rebuildTimer += dt;
            if (_rebuildTimer >= rebuildInterval)
            {
                _rebuildTimer = 0f;
                RebuildDirtyChunks();
            }

            if (enableLiquidAnimation)
            {
                _liquidAnimTimer += dt;
                if (_liquidAnimTimer >= liquidAnimationInterval)
                {
                    _liquidAnimTimer = 0f;
                    UpdateLiquidMeshAnimation();
                }
            }

            if (enableGasAnimation)
            {
                _gasAnimTimer += dt;
                if (_gasAnimTimer >= gasAnimationInterval)
                {
                    _gasAnimTimer = 0f;
                    UpdateGasMeshAnimation();
                }
            }
        }

        private void RebuildDirtyChunks()
        {
            if (_map == null) return;

            bool anyDirty = false;
            for (int cx = 0; cx < _chunkCols; cx++)
            {
                for (int cy = 0; cy < _chunkRows; cy++)
                {
                    if (_dirtyChunks[cx, cy])
                    {
                        RebuildChunk(cx, cy);
                        _dirtyChunks[cx, cy] = false;
                        anyDirty = true;
                    }
                }
            }
        }

        private void RebuildChunk(int cx, int cy)
        {
            int startX = cx * chunkSize;
            int startY = cy * chunkSize;
            int endX = Mathf.Min(startX + chunkSize, _map.Width);
            int endY = Mathf.Min(startY + chunkSize, _map.Height);

            _solidMeshData.Clear();
            _liquidMeshData.Clear();
            _gasMeshData.Clear();

            VoxelMeshBuilder.BuildSolidMesh(_map, _solidMeshData, startX, startY, endX, endY);
            VoxelMeshBuilder.BuildLiquidMesh(_map, _liquidMeshData, startX, startY, endX, endY, _time);
            VoxelMeshBuilder.BuildGasMesh(_map, _gasMeshData, startX, startY, endX, endY, _time);

            _solidMeshData.TrimExcess();
            _liquidMeshData.TrimExcess();
            _gasMeshData.TrimExcess();

            Mesh solidMesh = VoxelMeshBuilder.CreateMeshFromData(_solidMeshData);
            Mesh liquidMesh = VoxelMeshBuilder.CreateMeshFromData(_liquidMeshData);
            Mesh gasMesh = VoxelMeshBuilder.CreateMeshFromData(_gasMeshData);

            _solidChunkFilters[cx, cy].mesh = solidMesh;
            _liquidChunkFilters[cx, cy].mesh = liquidMesh;
            _gasChunkFilters[cx, cy].mesh = gasMesh;
        }

        private void UpdateLiquidMeshAnimation()
        {
            if (_map == null) return;

            for (int cx = 0; cx < _chunkCols; cx++)
            {
                for (int cy = 0; cy < _chunkRows; cy++)
                {
                    if (_liquidChunkFilters[cx, cy] == null) continue;
                    Mesh mesh = _liquidChunkFilters[cx, cy].mesh;
                    if (mesh == null || mesh.vertexCount == 0) continue;

                    Vector3[] vertices = mesh.vertices;
                    _liquidMeshData.Clear();

                    int startX = cx * chunkSize;
                    int startY = cy * chunkSize;
                    int endX = Mathf.Min(startX + chunkSize, _map.Width);
                    int endY = Mathf.Min(startY + chunkSize, _map.Height);

                    VoxelMeshBuilder.BuildLiquidMesh(_map, _liquidMeshData, startX, startY, endX, endY, _time);

                    if (_liquidMeshData.vertices.Count == vertices.Length)
                    {
                        mesh.vertices = _liquidMeshData.vertices.ToArray();
                        mesh.RecalculateBounds();
                    }
                }
            }
        }

        private void UpdateGasMeshAnimation()
        {
            if (_map == null) return;

            for (int cx = 0; cx < _chunkCols; cx++)
            {
                for (int cy = 0; cy < _chunkRows; cy++)
                {
                    if (_gasChunkFilters[cx, cy] == null) continue;
                    Mesh mesh = _gasChunkFilters[cx, cy].mesh;
                    if (mesh == null || mesh.vertexCount == 0) continue;

                    Vector3[] vertices = mesh.vertices;
                    Color32[] colors = mesh.colors32;
                    _gasMeshData.Clear();

                    int startX = cx * chunkSize;
                    int startY = cy * chunkSize;
                    int endX = Mathf.Min(startX + chunkSize, _map.Width);
                    int endY = Mathf.Min(startY + chunkSize, _map.Height);

                    VoxelMeshBuilder.BuildGasMesh(_map, _gasMeshData, startX, startY, endX, endY, _time);

                    if (_gasMeshData.vertices.Count == vertices.Length)
                    {
                        mesh.vertices = _gasMeshData.vertices.ToArray();
                        mesh.colors32 = _gasMeshData.colors.ToArray();
                        mesh.RecalculateBounds();
                    }
                }
            }
        }

        public void SetWindOverlayEnabled(bool enabled)
        {
            showWindOverlay = enabled;
            if (enabled)
            {
                DrawWindOverlay();
            }
            else
            {
                ClearWindOverlay();
            }
        }

        private void DrawWindOverlay()
        {
            if (_map == null || GameManager.Instance == null || GameManager.Instance.WindField == null) return;

            ClearWindOverlay();
            WindField windField = GameManager.Instance.WindField;

            GameObject overlayObj = new GameObject("WindOverlay");
            overlayObj.transform.SetParent(_debugContainer.transform);

            LineRenderer lineRenderer = overlayObj.AddComponent<LineRenderer>();
            lineRenderer.material = debugMaterial;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.positionCount = 0;
            lineRenderer.useWorldSpace = true;

            List<Vector3> linePoints = new List<Vector3>();
            float voxelSize = _map.VoxelSize;

            for (int x = 0; x < _map.Width; x += windOverlayStep)
            {
                for (int y = 0; y < _map.Height; y += windOverlayStep)
                {
                    Vector2 wind = windField.GetWindAt(x, y);
                    Vector2 startPos = new Vector2(x * voxelSize + voxelSize * 0.5f, y * voxelSize + voxelSize * 0.5f);
                    Vector2 endPos = startPos + wind.normalized * voxelSize * 0.8f;

                    linePoints.Add(new Vector3(startPos.x, startPos.y, -0.1f));
                    linePoints.Add(new Vector3(endPos.x, endPos.y, -0.1f));

                    Vector2 dir = (endPos - startPos).normalized;
                    Vector2 perp = new Vector2(-dir.y, dir.x);
                    Vector2 arrowBase = endPos - dir * voxelSize * 0.2f;
                    linePoints.Add(new Vector3(endPos.x, endPos.y, -0.1f));
                    linePoints.Add(new Vector3(arrowBase.x + perp.x * voxelSize * 0.15f, arrowBase.y + perp.y * voxelSize * 0.15f, -0.1f));
                    linePoints.Add(new Vector3(endPos.x, endPos.y, -0.1f));
                    linePoints.Add(new Vector3(arrowBase.x - perp.x * voxelSize * 0.15f, arrowBase.y - perp.y * voxelSize * 0.15f, -0.1f));
                }
            }

            lineRenderer.positionCount = linePoints.Count;
            lineRenderer.SetPositions(linePoints.ToArray());
            lineRenderer.startColor = windArrowColor;
            lineRenderer.endColor = windArrowColor;
        }

        private void ClearWindOverlay()
        {
            if (_debugContainer == null) return;
            for (int i = _debugContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_debugContainer.transform.GetChild(i).gameObject);
            }
        }

        public void ToggleWindOverlay()
        {
            SetWindOverlayEnabled(!showWindOverlay);
        }
    }
}
