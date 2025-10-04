using UnityEngine;
using Ouiki.SiliconeHeart.GridSystem;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Zenject;

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
        public float zoomSpeed = 5f;           // How fast to zoom
        public float minZoom = 2f;             // Minimum orthographic size
        public float maxZoom = 20f;            // Maximum orthographic size

        private Vector3 dragOrigin;
        private bool isDragging;

        // Pinch zoom fields
        private bool pinchActive = false;
        private float lastPinchDistance = 0f;

        void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }
        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        void Update()
        {
            HandleKeyboardPan();
            HandleMouseDrag();
            HandleZoom();
            HandlePinchZoom();
        }

        void HandleKeyboardPan()
        {
            Vector2 move = Vector2.zero;
            var kbd = Keyboard.current;
            if (kbd == null) return;

            // Arrow keys
            if (kbd.leftArrowKey.isPressed) move.x -= 1;
            if (kbd.rightArrowKey.isPressed) move.x += 1;
            if (kbd.upArrowKey.isPressed) move.y += 1;
            if (kbd.downArrowKey.isPressed) move.y -= 1;

            // WASD keys
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

        void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scrollValue = mouse.scroll.y.ReadValue();
                if (Mathf.Abs(scrollValue) > 0.01f)
                {
                    float zoomDelta = -scrollValue * zoomSpeed * Time.deltaTime;
                    float newSize = Mathf.Clamp(targetCamera.orthographicSize + zoomDelta, minZoom, maxZoom);
                    targetCamera.orthographicSize = newSize;
                }
            }

            // Optional: Keyboard zoom (Q/E or +/- keys)
            var kbd = Keyboard.current;
            if (kbd != null)
            {
                if (kbd.qKey.isPressed || kbd.minusKey.isPressed)
                {
                    float newSize = Mathf.Clamp(targetCamera.orthographicSize + zoomSpeed * Time.deltaTime, minZoom, maxZoom);
                    targetCamera.orthographicSize = newSize;
                }
                if (kbd.eKey.isPressed || kbd.equalsKey.isPressed)
                {
                    float newSize = Mathf.Clamp(targetCamera.orthographicSize - zoomSpeed * Time.deltaTime, minZoom, maxZoom);
                    targetCamera.orthographicSize = newSize;
                }
            }
        }

        void HandlePinchZoom()
        {
            // Only handle pinch if there are two touches
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 2)
            {
                var t0 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
                var t1 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[1];

                Vector2 pos0 = t0.screenPosition;
                Vector2 pos1 = t1.screenPosition;

                float pinchDistance = Vector2.Distance(pos0, pos1);

                if (!pinchActive)
                {
                    pinchActive = true;
                    lastPinchDistance = pinchDistance;
                }
                else
                {
                    float pinchDelta = pinchDistance - lastPinchDistance;
                    float zoomAmount = -pinchDelta * 0.02f; // Adjust sensitivity as needed

                    float newSize = Mathf.Clamp(targetCamera.orthographicSize + zoomAmount, minZoom, maxZoom);
                    targetCamera.orthographicSize = newSize;

                    lastPinchDistance = pinchDistance;
                }
            }
            else
            {
                pinchActive = false;
            }
        }

        public void PanCamera(Vector3 delta)
        {
            Vector3 newPos = targetCamera.transform.position + delta;

            // Clamp to ground bounds (+ extra cells)
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

                // If grid is smaller than camera view, fallback to fallback zone
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