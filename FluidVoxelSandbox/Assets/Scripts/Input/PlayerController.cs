using UnityEngine;
using System.Collections.Generic;
using FluidVoxelSandbox.Core;
using FluidVoxelSandbox.Rendering;

namespace FluidVoxelSandbox.Input
{
    public class PlayerController : MonoBehaviour
    {
        private VoxelMap _map;
        private Camera _camera;
        private Vector3 _cameraPosition;

        [Header("Brush Settings")]
        public VoxelType selectedVoxelType = VoxelType.Stone;
        public int brushRadius = 1;
        public float brushInterval = 0.05f;
        public bool continuousDraw = true;
        public bool eraseMode = false;

        [Header("Selection Types")]
        public VoxelType[] solidTypes = new VoxelType[]
        {
            VoxelType.Stone,
            VoxelType.Dirt,
            VoxelType.Grass,
            VoxelType.Sand
        };

        public VoxelType[] liquidTypes = new VoxelType[]
        {
            VoxelType.Water,
            VoxelType.Oil,
            VoxelType.Lava
        };

        public VoxelType[] gasTypes = new VoxelType[]
        {
            VoxelType.Smoke,
            VoxelType.Steam,
            VoxelType.ToxicGas
        };

        [Header("Camera")]
        public float cameraMoveSpeed = 20f;
        public float cameraZoomSpeed = 5f;
        public float minCameraSize = 10f;
        public float maxCameraSize = 200f;

        [Header("Input Keys")]
        public KeyCode eraseKey = KeyCode.LeftShift;
        public KeyCode increaseBrushKey = KeyCode.RightBracket;
        public KeyCode decreaseBrushKey = KeyCode.LeftBracket;
        public KeyCode[] numberKeys = new KeyCode[]
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8,
            KeyCode.Alpha9
        };

        [Header("UI")]
        public bool showCursor = true;

        private float _drawTimer;
        private int _lastMouseVoxelX = -1;
        private int _lastMouseVoxelY = -1;

        public void Initialize(VoxelMap map, Camera cam = null)
        {
            _map = map;
            if (cam != null)
            {
                _camera = cam;
            }
            else
            {
                _camera = Camera.main;
            }
            _cameraPosition = _camera.transform.position;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.VoxelMap != null && _map == null)
            {
                Initialize(GameManager.Instance.VoxelMap);
                if (GameManager.Instance.FluidSimulation != null && GameManager.Instance.WindField != null && GameManager.Instance.WindController != null)
                {
                    GameManager.Instance.FluidSimulation.Initialize(
                        GameManager.Instance.VoxelMap,
                        GameManager.Instance.WindField,
                        GameManager.Instance.WindController
                    );
                }
                if (GameManager.Instance.VoxelCollision != null)
                {
                    GameManager.Instance.VoxelCollision.Initialize(GameManager.Instance.VoxelMap);
                }
                if (GameManager.Instance.VoxelRenderer != null)
                {
                    GameManager.Instance.VoxelRenderer.Initialize(GameManager.Instance.VoxelMap);
                }
                if (GameManager.Instance.TerrainGenerator != null)
                {
                    GameManager.Instance.RegenerateTerrain();
                }
                if (GameManager.Instance.WindController != null && GameManager.Instance.WindField != null)
                {
                    GameManager.Instance.WindController.Initialize(GameManager.Instance.WindField);
                }
            }

            if (_camera == null) return;

            HandleCameraInput();
            HandleBrushInput();
            HandleVoxelTypeSelection();
        }

        private void HandleCameraInput()
        {
            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                float orthoSize = _camera.orthographicSize;
                orthoSize -= scroll * cameraZoomSpeed;
                orthoSize = Mathf.Clamp(orthoSize, minCameraSize, maxCameraSize);
                _camera.orthographicSize = orthoSize;
            }

            Vector3 move = Vector3.zero;

            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
                move.y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
                move.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                move.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
                move.x += 1f;

            if (move.sqrMagnitude > 0f)
            {
                move.Normalize();
                float speedMod = _camera.orthographicSize / 50f;
                _cameraPosition += move * cameraMoveSpeed * speedMod * Time.deltaTime;
            }

            if (UnityEngine.Input.GetMouseButton(2))
            {
                float panSpeed = _camera.orthographicSize * 0.8f;
                _cameraPosition.x -= UnityEngine.Input.GetAxis("Mouse X") * panSpeed * 0.02f;
                _cameraPosition.y -= UnityEngine.Input.GetAxis("Mouse Y") * panSpeed * 0.02f;
            }

            ClampCameraPosition();
            _camera.transform.position = _cameraPosition;
        }

        private void ClampCameraPosition()
        {
            if (_map == null) return;

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            float mapWidth = _map.Width * _map.VoxelSize;
            float mapHeight = _map.Height * _map.VoxelSize;

            _cameraPosition.x = Mathf.Clamp(_cameraPosition.x, halfWidth, Mathf.Max(halfWidth, mapWidth - halfWidth));
            _cameraPosition.y = Mathf.Clamp(_cameraPosition.y, halfHeight, Mathf.Max(halfHeight, mapHeight - halfHeight));
            _cameraPosition.z = -10f;
        }

        private void HandleBrushInput()
        {
            if (_map == null) return;

            bool currentEraseMode = UnityEngine.Input.GetKey(eraseKey) || eraseMode;

            if (UnityEngine.Input.GetKeyDown(increaseBrushKey))
            {
                brushRadius = Mathf.Clamp(brushRadius + 1, 1, 20);
            }

            if (UnityEngine.Input.GetKeyDown(decreaseBrushKey))
            {
                brushRadius = Mathf.Clamp(brushRadius - 1, 1, 20);
            }

            Vector3 mousePos = UnityEngine.Input.mousePosition;
            Vector2 worldPos = _camera.ScreenToWorldPoint(mousePos);

            int voxelX, voxelY;
            if (!_map.WorldToVoxel(worldPos, out voxelX, out voxelY))
            {
                return;
            }

            _drawTimer += Time.deltaTime;

            if (UnityEngine.Input.GetMouseButton(0) || UnityEngine.Input.GetMouseButton(1))
            {
                if (continuousDraw)
                {
                    if (_drawTimer >= brushInterval)
                    {
                        _drawTimer = 0f;

                        if (brushRadius <= 1)
                        {
                            DrawLine(_lastMouseVoxelX, _lastMouseVoxelY, voxelX, voxelY,
                                currentEraseMode ? VoxelType.Empty : selectedVoxelType);
                        }
                        else
                        {
                            ApplyBrush(voxelX, voxelY, currentEraseMode ? VoxelType.Empty : selectedVoxelType);
                        }
                    }
                }
            }

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (brushRadius <= 1)
                    ApplyVoxel(voxelX, voxelY, currentEraseMode ? VoxelType.Empty : selectedVoxelType);
                else
                    ApplyBrush(voxelX, voxelY, currentEraseMode ? VoxelType.Empty : selectedVoxelType);
            }

            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                Voxel v = _map.GetVoxel(voxelX, voxelY);
                if (!v.IsEmpty)
                {
                    selectedVoxelType = v.Type;
                    Debug.Log($"Selected voxel type: {v.Type}");
                }
            }

            _lastMouseVoxelX = voxelX;
            _lastMouseVoxelY = voxelY;
        }

        private void DrawLine(int x0, int y0, int x1, int y1, VoxelType type)
        {
            if (x0 < 0 || y0 < 0)
            {
                ApplyVoxel(x1, y1, type);
                return;
            }

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0, y = y0;
            int safety = 1000;

            while (safety-- > 0)
            {
                ApplyVoxel(x, y, type);

                if (x == x1 && y == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private void ApplyVoxel(int x, int y, VoxelType type)
        {
            if (!_map.InBounds(x, y)) return;

            if (type == VoxelType.Empty)
            {
                Voxel current = _map.GetVoxel(x, y);
                if (!current.IsEmpty)
                {
                    _map.SetVoxel(x, y, VoxelType.Empty);
                }
            }
            else
            {
                Voxel current = _map.GetVoxel(x, y);
                if (current.Type != type)
                {
                    _map.SetVoxel(x, y, type);
                }
            }
        }

        private void ApplyBrush(int centerX, int centerY, VoxelType type)
        {
            int r2 = brushRadius * brushRadius;

            for (int dy = -brushRadius; dy <= brushRadius; dy++)
            {
                for (int dx = -brushRadius; dx <= brushRadius; dx++)
                {
                    int dist2 = dx * dx + dy * dy;
                    if (dist2 > r2) continue;

                    float falloff = 1f - (float)dist2 / r2;
                    if (falloff < 0.3f && Random.value > falloff * 2f) continue;

                    int x = centerX + dx;
                    int y = centerY + dy;

                    ApplyVoxel(x, y, type);
                }
            }
        }

        private void HandleVoxelTypeSelection()
        {
            List<VoxelType> allTypes = new List<VoxelType>();
            allTypes.AddRange(solidTypes);
            allTypes.AddRange(liquidTypes);
            allTypes.AddRange(gasTypes);

            for (int i = 0; i < numberKeys.Length && i < allTypes.Count; i++)
            {
                if (UnityEngine.Input.GetKeyDown(numberKeys[i]))
                {
                    selectedVoxelType = allTypes[i];
                    Debug.Log($"Selected: {selectedVoxelType}");
                }
            }
        }

        public void CycleVoxelCategory()
        {
            VoxelCategory currentCat = VoxelProperties.GetCategory(selectedVoxelType);
            VoxelCategory nextCat = (VoxelCategory)(((int)currentCat + 1) % 4);
            if (nextCat == VoxelCategory.Empty) nextCat = VoxelCategory.Solid;

            switch (nextCat)
            {
                case VoxelCategory.Solid:
                    selectedVoxelType = solidTypes[0];
                    break;
                case VoxelCategory.Liquid:
                    selectedVoxelType = liquidTypes[0];
                    break;
                case VoxelCategory.Gas:
                    selectedVoxelType = gasTypes[0];
                    break;
            }
        }

        public void NextVoxelInCategory()
        {
            VoxelCategory cat = VoxelProperties.GetCategory(selectedVoxelType);
            VoxelType[] arr = null;
            switch (cat)
            {
                case VoxelCategory.Solid: arr = solidTypes; break;
                case VoxelCategory.Liquid: arr = liquidTypes; break;
                case VoxelCategory.Gas: arr = gasTypes; break;
            }

            if (arr == null || arr.Length == 0) return;

            int idx = System.Array.IndexOf(arr, selectedVoxelType);
            idx = (idx + 1) % arr.Length;
            selectedVoxelType = arr[idx];
        }

        public Vector2 GetMouseWorldPosition()
        {
            Vector3 mousePos = UnityEngine.Input.mousePosition;
            return _camera.ScreenToWorldPoint(mousePos);
        }

        public Vector2Int GetMouseVoxelPosition()
        {
            Vector2 worldPos = GetMouseWorldPosition();
            int x, y;
            _map.WorldToVoxel(worldPos, out x, out y);
            return new Vector2Int(x, y);
        }
    }
}
