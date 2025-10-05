using UnityEngine;
using UnityEngine.UI;
using Ouiki.SiliconeHeart.GridSystem;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Zenject;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Ouiki.SiliconeHeart.Core
{
    public class CameraManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [Inject] private GridManager gridManager;

        [Header("Camera Movement")]
        public float panSpeed = 10f;
        public float dragSpeed = 0.5f;
        public Vector2 fallbackZoneCenter = Vector2.zero;
        public Vector2 fallbackZoneSize = new Vector2(10f, 10f);

        public int extraCells = 2;

        [Header("Camera Zoom")]
        public float zoomSpeed = 2f;           // Camera zoom step for buttons/mouse wheel
        public float minZoom = 2f;             // Minimum orthographic size
        public float maxZoom = 20f;            // Maximum orthographic size

        [Header("Zoom Buttons")]
        public Button zoomInButton;
        public Button zoomOutButton;

        private Vector3 dragOrigin;
        private bool isDragging;

        // Pinch zoom fields
        private bool pinchActive = false;
        private float lastPinchDistance = 0f;

        // Touch pan fields
        private Vector2 touchPanOrigin;
        private bool touchPanning;
        private int panFingerId = -1;

        void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            // Setup listeners for zoom buttons if assigned in inspector
            if (zoomInButton != null)
                zoomInButton.onClick.AddListener(ZoomIn);

            if (zoomOutButton != null)
                zoomOutButton.onClick.AddListener(ZoomOut);
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            if (zoomInButton != null)
                zoomInButton.onClick.RemoveListener(ZoomIn);
            if (zoomOutButton != null)
                zoomOutButton.onClick.RemoveListener(ZoomOut);
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void Update()
        {
            HandleKeyboardPan();
            if (Application.isMobilePlatform)
                HandleTouchPan();
            else
                HandleMouseDrag();
            HandleMouseWheelZoom();
            HandlePinchZoom();
        }

        void HandleKeyboardPan()
        {
            Vector2 move = Vector2.zero;
            var kbd = Keyboard.current;
            if (kbd == null) return;

            if (kbd.leftArrowKey.isPressed) move.x -= 1;
            if (kbd.rightArrowKey.isPressed) move.x += 1;
            if (kbd.upArrowKey.isPressed) move.y += 1;
            if (kbd.downArrowKey.isPressed) move.y -= 1;
            if (kbd.aKey.isPressed) move.x -= 1;
            if (kbd.dKey.isPressed) move.x += 1;
            if (kbd.wKey.isPressed) move.y += 1;
            if (kbd.sKey.isPressed) move.y -= 1;

            if (move != Vector2.zero)
            {
                move = move.normalized * panSpeed * Time.deltaTime;
                PanCamera(new Vector3(move.x, move.y, 0f));
            }
        }

        void HandleMouseDrag()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
            {
                dragOrigin = targetCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                isDragging = true;
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
            }
            if (isDragging)
            {
                Vector3 difference = dragOrigin - targetCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                PanCamera(difference * dragSpeed);
                dragOrigin = targetCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            }
        }

        void HandleTouchPan()
        {
            if (Touch.activeTouches.Count == 1)
            {
                var t = Touch.activeTouches[0];

                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(t.finger.index))
                {
                    touchPanOrigin = t.screenPosition;
                    panFingerId = t.touchId;
                    touchPanning = true;
                }
                else if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved && touchPanning && t.touchId == panFingerId)
                {
                    Vector2 delta = t.screenPosition - touchPanOrigin;
                    Vector3 worldDelta = new Vector3(-delta.x, -delta.y, 0) * dragSpeed * 0.01f;
                    PanCamera(worldDelta);
                    touchPanOrigin = t.screenPosition;
                }
                else if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended && t.touchId == panFingerId)
                {
                    touchPanning = false;
                    panFingerId = -1;
                }
            }
            else
            {
                touchPanning = false;
                panFingerId = -1;
            }
        }

        void HandleMouseWheelZoom()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scrollValue = mouse.scroll.y.ReadValue();
                if (Mathf.Abs(scrollValue) > 0.01f)
                {
                    float step = zoomSpeed * Mathf.Sign(-scrollValue); // Negative to match typical scroll behavior
                    float targetZoom = Mathf.Clamp(targetCamera.orthographicSize + step, minZoom, GetMaxZoom());
                    Debug.Log($"[CameraManager] Mouse wheel zoom: {targetCamera.orthographicSize} -> {targetZoom}");
                    targetCamera.orthographicSize = targetZoom;
                }
            }
        }

        void HandlePinchZoom()
        {
            if (Touch.activeTouches.Count == 2)
            {
                var t0 = Touch.activeTouches[0];
                var t1 = Touch.activeTouches[1];

                Vector2 pos0 = t0.screenPosition;
                Vector2 pos1 = t1.screenPosition;

                float pinchDistance = Vector2.Distance(pos0, pos1);

                float desiredZoom = targetCamera.orthographicSize;

                if (!pinchActive)
                {
                    pinchActive = true;
                    lastPinchDistance = pinchDistance;
                }
                else
                {
                    float pinchDelta = pinchDistance - lastPinchDistance;
                    float zoomAmount = -pinchDelta * 0.02f;

                    desiredZoom = Mathf.Clamp(targetCamera.orthographicSize + zoomAmount, minZoom, GetMaxZoom());
                    targetCamera.orthographicSize = desiredZoom;

                    lastPinchDistance = pinchDistance;
                }
            }
            else
            {
                pinchActive = false;
            }
        }

        float GetMaxZoom()
        {
            if (gridManager == null || gridManager.gridWidth <= 0 || gridManager.gridHeight <= 0)
                return maxZoom;

            float cellSize = gridManager.cellSize;
            int gridWidth = gridManager.gridWidth;
            int gridHeight = gridManager.gridHeight;

            float mapWidth = (gridWidth + extraCells * 2) * cellSize;
            float mapHeight = (gridHeight + extraCells * 2) * cellSize;

            float aspect = targetCamera.aspect;

            float maxOrthoByHeight = mapHeight * 0.5f;
            float maxOrthoByWidth = mapWidth * 0.5f / aspect;

            float maxByCells = Mathf.Min(maxOrthoByHeight, maxOrthoByWidth);

            return Mathf.Min(maxZoom, maxByCells);
        }

        public void ZoomIn()
        {
            float targetZoom = Mathf.Clamp(targetCamera.orthographicSize - zoomSpeed, minZoom, GetMaxZoom());
            Debug.Log($"[CameraManager] ZoomIn button: {targetCamera.orthographicSize} -> {targetZoom}");
            targetCamera.orthographicSize = targetZoom;
        }

        public void ZoomOut()
        {
            float targetZoom = Mathf.Clamp(targetCamera.orthographicSize + zoomSpeed, minZoom, GetMaxZoom());
            Debug.Log($"[CameraManager] ZoomOut button: {targetCamera.orthographicSize} -> {targetZoom}");
            targetCamera.orthographicSize = targetZoom;
        }

        public void PanCamera(Vector3 delta)
        {
            Vector3 newPos = targetCamera.transform.position + delta;

            if (gridManager != null && gridManager.gridWidth > 0 && gridManager.gridHeight > 0)
            {
                Vector2 minBounds = gridManager.CellWorldPos(new Vector2Int(-extraCells, -extraCells));
                Vector2 maxBounds = gridManager.CellWorldPos(new Vector2Int(gridManager.gridWidth - 1 + extraCells, gridManager.gridHeight - 1 + extraCells));

                float camHalfWidth = targetCamera.orthographicSize * targetCamera.aspect;
                float camHalfHeight = targetCamera.orthographicSize;

                float minX = minBounds.x + camHalfWidth;
                float maxX = maxBounds.x - camHalfWidth;
                float minY = minBounds.y + camHalfHeight;
                float maxY = maxBounds.y - camHalfHeight;

                if (maxX < minX || maxY < minY)
                {
                    minX = fallbackZoneCenter.x - fallbackZoneSize.x / 2f + camHalfWidth;
                    maxX = fallbackZoneCenter.x + fallbackZoneSize.x / 2f - camHalfWidth;
                    minY = fallbackZoneCenter.y - fallbackZoneSize.y / 2f + camHalfHeight;
                    maxY = fallbackZoneCenter.y + fallbackZoneSize.y / 2f - camHalfHeight;
                }

                newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
                newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
            }

            targetCamera.transform.position = new Vector3(newPos.x, newPos.y, targetCamera.transform.position.z);
        }
    }
}