using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Ouiki.SiliconeHeart.GridSystem;
using Ouiki.SiliconeHeart.Buildings;
using Zenject;
using Ouiki.SiliconeHeart.PlayGameMode;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Ouiki.SiliconeHeart.Input
{
    public class InputHandler : MonoBehaviour
    {
        #region References & State
        [Header("References")]
        [Inject] private GridManager gridManager;
        [Inject] private BuildingManager buildingManager;
        [Inject] private PlayModeManager playModeManager;
        private Camera mainCamera;
        [HideInInspector] public SpriteRenderer ghostRenderer;

        private bool isDraggingFromButton = false;
        private BuildingDataSO dragBuildingData = null;
        private BuildingDataSO selectedBuildingData = null;

        private Vector2Int lastGhostGridPos;
        private int lastGhostWidth;
        private int lastGhostHeight;

        public BuildingDataSO SelectedBuildingData => selectedBuildingData;

        private GamePlayMode previousMode = GamePlayMode.None;

        // Touch state
        private Vector2 lastTouchScreenPos;
        private Vector2 lastTouchStartPos;
        private bool touchMoved = false;
        private float touchSwipeThreshold = 10f; // pixels

        // Simulator flag
        [Header("Simulate Touch")]
        public bool simulateTouch = false; // Set this in Inspector for simulator mode

        #endregion

        #region Initialization
        void Awake()
        {
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        public void Init()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (ghostRenderer == null)
            {
                var go = new GameObject("BuildingGhost", typeof(SpriteRenderer));
                ghostRenderer = go.GetComponent<SpriteRenderer>();
                go.transform.SetParent(null);
                ghostRenderer.gameObject.SetActive(false);
            }
            ghostRenderer.sortingOrder = 15;
        }
        #endregion

        #region Update
        void Update()
        {
            if (playModeManager != null && playModeManager.CurrentMode != previousMode)
            {
                Debug.Log($"[InputHandler] Game mode changed: {previousMode} -> {playModeManager.CurrentMode}");
                OnModeChanged(playModeManager.CurrentMode, previousMode);
                previousMode = playModeManager.CurrentMode;
            }

            if (isDraggingFromButton && dragBuildingData != null)
            {
                UpdateGhostFromPointer(dragBuildingData);
                UpdateGhostPosition();
                return;
            }

            // --- DEBUG: What input mode are we using? ---
#if UNITY_EDITOR
            Debug.Log("[InputHandler] UNITY_EDITOR: using PC input");
            HandlePCInput();
#else
            if (simulateTouch)
            {
                Debug.Log("[InputHandler] SIMULATOR MODE: using simulated touch as mouse");
                HandleSimulatorTouch();
            }
            else if (Application.isMobilePlatform)
            {
                Debug.Log("[InputHandler] DEVICE: using mobile touch");
                HandleMobileInput();
            }
            else
            {
                Debug.Log("[InputHandler] DESKTOP: using PC input");
                HandlePCInput();
            }
#endif

            UpdateGhostPosition();
        }
        #endregion

        #region Mode Change Handler
        void OnModeChanged(GamePlayMode newMode, GamePlayMode oldMode)
        {
            SetGhost(false);
            dragBuildingData = null;
            selectedBuildingData = null;
            isDraggingFromButton = false;

            if (newMode == GamePlayMode.Remove || newMode == GamePlayMode.None)
            {
                if (buildingManager != null)
                    buildingManager.SetActiveBuilding(null);
            }
            if (oldMode == GamePlayMode.Remove || oldMode == GamePlayMode.Place)
            {
                gridManager.ClearHighlight();
            }
        }
        #endregion

        #region PC Input
        void HandlePCInput()
        {
            if (gridManager == null || buildingManager == null || mainCamera == null || playModeManager == null)
                return;

            Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            float zDistance = -mainCamera.transform.position.z;
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, zDistance));
            mouseWorld.z = 0f;
            Vector2Int gridPos = GetCenteredGridPosition(mouseWorld);

            Debug.Log($"[InputHandler-PC] Mouse position: {mouseScreenPos} / World: {mouseWorld} / Grid: {gridPos} / Mode: {playModeManager.CurrentMode}");

            if (!gridManager.IsValidCell(gridPos.x, gridPos.y))
            {
                gridManager.ClearHighlight();
                SetGhost(false);
                return;
            }

            if (playModeManager.CurrentMode == GamePlayMode.Remove)
            {
                gridManager.UpdateOverlayVisibility();
                buildingManager.HandleRemoveHover(gridPos);

                if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
                {
                    Debug.Log("[InputHandler-PC] Mouse click for remove at " + gridPos);

                    Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, zDistance));
                    Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);

                    RaycastHit2D hit = Physics2D.Raycast(worldPoint2D, Vector2.zero);
                    if (hit.collider != null)
                    {
                        FieldBuilding fieldBuilding = hit.collider.GetComponent<FieldBuilding>();
                        if (fieldBuilding != null)
                        {
                            Debug.Log("[InputHandler-PC] Removed building via collider at " + gridPos);
                            buildingManager.RemoveBuilding(fieldBuilding);
                            gridManager.ClearHighlight();
                            SetGhost(false);
                            return;
                        }
                    }
                    bool removed = buildingManager.TryRemoveBuildingAt(gridPos);
                    Debug.Log($"[InputHandler-PC] Remove by grid: {removed}");
                    if (removed)
                    {
                        gridManager.ClearHighlight();
                        SetGhost(false);
                    }
                }
                return;
            }

            if (selectedBuildingData != null && playModeManager.CurrentMode == GamePlayMode.Place)
            {
                bool validCell = gridManager.IsValidCell(gridPos.x, gridPos.y);
                bool canPlace = validCell && gridManager.IsAreaPlaceable(gridPos, selectedBuildingData.width, selectedBuildingData.height);

                SetGhost(true, selectedBuildingData.buildingSprite, gridPos, selectedBuildingData.width, selectedBuildingData.height, canPlace);
                gridManager.HighlightArea(gridPos, selectedBuildingData.width, selectedBuildingData.height, canPlace);

                bool placeAction = Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject();

                if (placeAction)
                {
                    Debug.Log($"[InputHandler-PC] Mouse click for place at {gridPos} (canPlace={canPlace})");
                    if (canPlace)
                    {
                        buildingManager.SetActiveBuilding(selectedBuildingData);
                        buildingManager.TryPlaceBuilding(gridPos);
                        Debug.Log("[InputHandler-PC] Building placed!");
                    }
                    else
                    {
                        Debug.LogWarning("[InputHandler-PC] Can't place here");
                        selectedBuildingData = null;
                        SetGhost(false);
                        gridManager.ClearHighlight();
                    }
                }
                return;
            }

            gridManager.ClearHighlight();
            SetGhost(false);
        }
        #endregion

        #region Simulator Input (Simulate Touch with Mouse)
        void HandleSimulatorTouch()
        {
            if (gridManager == null || buildingManager == null || mainCamera == null || playModeManager == null)
                return;

            Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            float zDistance = -mainCamera.transform.position.z;
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, zDistance));
            mouseWorld.z = 0f;
            Vector2Int gridPos = GetCenteredGridPosition(mouseWorld);

            Debug.Log($"[InputHandler-SIMULATOR] Simulated touch/mouse position: {mouseScreenPos} / World: {mouseWorld} / Grid: {gridPos} / Mode: {playModeManager.CurrentMode}");

            if (!gridManager.IsValidCell(gridPos.x, gridPos.y))
            {
                gridManager.ClearHighlight();
                SetGhost(false);
                return;
            }

            if (playModeManager.CurrentMode == GamePlayMode.Remove)
            {
                gridManager.UpdateOverlayVisibility();
                buildingManager.HandleRemoveHover(gridPos);

                if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
                {
                    Debug.Log("[InputHandler-SIMULATOR] Simulated touch/click for remove at " + gridPos);

                    RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
                    if (hit.collider != null)
                    {
                        FieldBuilding fieldBuilding = hit.collider.GetComponent<FieldBuilding>();
                        if (fieldBuilding != null)
                        {
                            Debug.Log("[InputHandler-SIMULATOR] Removed building via collider at " + gridPos);
                            buildingManager.RemoveBuilding(fieldBuilding);
                            gridManager.ClearHighlight();
                            SetGhost(false);
                            return;
                        }
                    }
                    bool removed = buildingManager.TryRemoveBuildingAt(gridPos);
                    Debug.Log($"[InputHandler-SIMULATOR] Remove by grid: {removed}");
                    if (removed)
                    {
                        gridManager.ClearHighlight();
                        SetGhost(false);
                    }
                }
                return;
            }

            if (selectedBuildingData != null && playModeManager.CurrentMode == GamePlayMode.Place)
            {
                bool validCell = gridManager.IsValidCell(gridPos.x, gridPos.y);
                bool canPlace = validCell && gridManager.IsAreaPlaceable(gridPos, selectedBuildingData.width, selectedBuildingData.height);

                SetGhost(true, selectedBuildingData.buildingSprite, gridPos, selectedBuildingData.width, selectedBuildingData.height, canPlace);
                gridManager.HighlightArea(gridPos, selectedBuildingData.width, selectedBuildingData.height, canPlace);

                bool placeAction = Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject();

                if (placeAction)
                {
                    Debug.Log($"[InputHandler-SIMULATOR] Simulated touch/click for place at {gridPos} (canPlace={canPlace})");
                    if (canPlace)
                    {
                        buildingManager.SetActiveBuilding(selectedBuildingData);
                        buildingManager.TryPlaceBuilding(gridPos);
                        Debug.Log("[InputHandler-SIMULATOR] Building placed!");
                    }
                    else
                    {
                        Debug.LogWarning("[InputHandler-SIMULATOR] Can't place here");
                        selectedBuildingData = null;
                        SetGhost(false);
                        gridManager.ClearHighlight();
                    }
                }
                return;
            }

            gridManager.ClearHighlight();
            SetGhost(false);
        }
        #endregion

        #region Mobile Input
        void HandleMobileInput()
        {
            if (gridManager == null || buildingManager == null || mainCamera == null || playModeManager == null)
                return;

            if (Touch.activeTouches.Count > 0)
            {
                var touch = Touch.activeTouches[0];
                lastTouchScreenPos = touch.screenPosition;

                Vector2 screenPos = touch.screenPosition;
                float zDistance = -mainCamera.transform.position.z;
                Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDistance));
                world.z = 0f;
                Vector2Int gridPos = GetCenteredGridPosition(world);

                bool validCell = gridManager.IsValidCell(gridPos.x, gridPos.y);

                Debug.Log($"[InputHandler-Mobile] Touch phase: {touch.phase} at ScreenPos: {screenPos} / World: {world} / Grid: {gridPos} / Mode: {playModeManager.CurrentMode}");

                if (selectedBuildingData != null && playModeManager.CurrentMode == GamePlayMode.Place)
                {
                    bool canPlace = validCell && gridManager.IsAreaPlaceable(gridPos, selectedBuildingData.width, selectedBuildingData.height);

                    SetGhost(true, selectedBuildingData.buildingSprite, gridPos, selectedBuildingData.width, selectedBuildingData.height, canPlace);
                    gridManager.HighlightArea(gridPos, selectedBuildingData.width, selectedBuildingData.height, canPlace);

                    bool placeAction =
                        (touch.phase == UnityEngine.InputSystem.TouchPhase.Began || touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                        && !EventSystem.current.IsPointerOverGameObject(touch.finger.index);

                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        lastTouchStartPos = touch.screenPosition;
                        touchMoved = false;
                        Debug.Log($"[InputHandler-Mobile] Touch BEGAN at {screenPos}");
                    }
                    else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                    {
                        if ((touch.screenPosition - lastTouchStartPos).magnitude > touchSwipeThreshold)
                        {
                            touchMoved = true;
                            Debug.Log($"[InputHandler-Mobile] Touch MOVED > threshold ({touchSwipeThreshold})");
                        }
                    }
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended && touchMoved && !EventSystem.current.IsPointerOverGameObject(touch.finger.index))
                    {
                        placeAction = true;
                        Debug.Log("[InputHandler-Mobile] Touch ENDED with swipe, will attempt place");
                    }

                    if (placeAction)
                    {
                        Debug.Log($"[InputHandler-Mobile] Place attempt at {gridPos} (canPlace={canPlace})");
                        if (canPlace)
                        {
                            buildingManager.SetActiveBuilding(selectedBuildingData);
                            buildingManager.TryPlaceBuilding(gridPos);
                            Debug.Log("[InputHandler-Mobile] Building placed!");
                        }
                        else
                        {
                            Debug.LogWarning("[InputHandler-Mobile] Can't place here");
                            selectedBuildingData = null;
                            SetGhost(false);
                            gridManager.ClearHighlight();
                        }
                    }
                    return;
                }

                if (playModeManager.CurrentMode == GamePlayMode.Remove)
                {
                    gridManager.UpdateOverlayVisibility();

                    bool removeAction =
                        (touch.phase == UnityEngine.InputSystem.TouchPhase.Began || touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                        && !EventSystem.current.IsPointerOverGameObject(touch.finger.index);

                    if (removeAction)
                    {
                        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(touch.screenPosition.x, touch.screenPosition.y, -mainCamera.transform.position.z));
                        Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);

                        RaycastHit2D hit = Physics2D.Raycast(worldPoint2D, Vector2.zero);
                        if (hit.collider != null)
                        {
                            FieldBuilding fieldBuilding = hit.collider.GetComponent<FieldBuilding>();
                            if (fieldBuilding != null)
                            {
                                Debug.Log($"[InputHandler-Mobile] Removed building via collider tap at {gridPos}");
                                buildingManager.RemoveBuilding(fieldBuilding);
                                gridManager.ClearHighlight();
                                SetGhost(false);
                                return;
                            }
                        }
                        bool removed = buildingManager.TryRemoveBuildingAt(gridPos);
                        Debug.Log($"[InputHandler-Mobile] Remove by grid: {removed}");
                        if (removed)
                        {
                            gridManager.ClearHighlight();
                            SetGhost(false);
                        }
                    }
                    return;
                }

                gridManager.ClearHighlight();
                SetGhost(false);
            }
        }
        #endregion

        #region Ghost Pointer Update
        void UpdateGhostFromPointer(BuildingDataSO buildingData)
        {
            Vector2 pointerPos = Vector2.zero;
#if UNITY_EDITOR
            pointerPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            if (simulateTouch)
                pointerPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            else if (Touch.activeTouches.Count > 0)
                pointerPos = Touch.activeTouches[0].screenPosition;
            else
                pointerPos = lastTouchScreenPos;
#endif
            float zDistance = -mainCamera.transform.position.z;
            Vector3 pointerWorld = mainCamera.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, zDistance));
            pointerWorld.z = 0f;
            Vector2Int gridPos = GetCenteredGridPosition(pointerWorld);

            bool validCell = gridManager.IsValidCell(gridPos.x, gridPos.y);
            bool canPlace = validCell && gridManager.IsAreaPlaceable(gridPos, buildingData.width, buildingData.height);

            SetGhost(true, buildingData.buildingSprite, gridPos, buildingData.width, buildingData.height, canPlace);
            gridManager.HighlightArea(gridPos, buildingData.width, buildingData.height, canPlace);

            bool placeAction = false;
#if UNITY_EDITOR
            placeAction = Mouse.current.leftButton.wasReleasedThisFrame;
#else
            if (simulateTouch)
                placeAction = Mouse.current.leftButton.wasReleasedThisFrame;
            else if (Touch.activeTouches.Count > 0)
            {
                var touch = Touch.activeTouches[0];
                placeAction = (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Ended);

                if (touch.phase == TouchPhase.Began)
                {
                    lastTouchStartPos = touch.screenPosition;
                    touchMoved = false;
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    if ((touch.screenPosition - lastTouchStartPos).magnitude > touchSwipeThreshold)
                        touchMoved = true;
                }
                if (touch.phase == TouchPhase.Ended && touchMoved)
                    placeAction = true;
            }
            else
                placeAction = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#endif

            bool pointerOverUI = false;
#if UNITY_EDITOR
            pointerOverUI = EventSystem.current.IsPointerOverGameObject();
#else
            if (simulateTouch)
                pointerOverUI = EventSystem.current.IsPointerOverGameObject();
            else if (Touch.activeTouches.Count > 0)
                pointerOverUI = EventSystem.current.IsPointerOverGameObject(Touch.activeTouches[0].finger.index);
            else
                pointerOverUI = EventSystem.current.IsPointerOverGameObject();
#endif

            if (placeAction && !pointerOverUI)
            {
                if (canPlace)
                {
                    Debug.Log("[InputHandler] Ghost place triggered at " + gridPos);
                    selectedBuildingData = buildingData;
                    buildingManager.SetActiveBuilding(buildingData);
                    buildingManager.TryPlaceBuilding(gridPos);
                }
                else
                {
                    Debug.LogWarning("[InputHandler] Ghost cannot place at " + gridPos);
                    selectedBuildingData = null;
                    SetGhost(false);
                    gridManager.ClearHighlight();
                }
                isDraggingFromButton = false;
                dragBuildingData = null;
            }
        }
        #endregion

        #region Ghost (Preview) Management
        Vector2Int GetCenteredGridPosition(Vector3 pointerWorld)
        {
            int w = 1, h = 1;
            if (isDraggingFromButton && dragBuildingData != null)
            {
                w = dragBuildingData.width;
                h = dragBuildingData.height;
            }
            else if (selectedBuildingData != null)
            {
                w = selectedBuildingData.width;
                h = selectedBuildingData.height;
            }
            else if (playModeManager != null && playModeManager.CurrentMode == GamePlayMode.Place && buildingManager.activeBuilding != null)
            {
                w = buildingManager.activeBuilding.width;
                h = buildingManager.activeBuilding.height;
            }

            Vector2 pointerWorldCentered = pointerWorld;
            pointerWorldCentered.x -= gridManager.cellSize * (w / 2f - 0.5f);
            pointerWorldCentered.y -= gridManager.cellSize * (h / 2f - 0.5f);

            Vector2Int gridPosRaw = gridManager.WorldToGrid(pointerWorldCentered);
            return gridPosRaw;
        }

        void UpdateGhostPosition()
        {
            if (ghostRenderer != null && ghostRenderer.gameObject.activeSelf && mainCamera != null)
            {
                Vector3 cellWorldPos = gridManager.CellWorldPos(lastGhostGridPos);
                float offsetX = gridManager.cellSize * (lastGhostWidth / 2f - 0.5f);
                float offsetY = gridManager.cellSize * (lastGhostHeight / 2f - 0.5f);
                Vector3 centerPos = cellWorldPos + new Vector3(offsetX, offsetY, 0f);
                ghostRenderer.transform.position = centerPos;
            }
        }

        void SetGhost(bool active, Sprite sprite = null, Vector2Int gridPos = default, int width = 1, int height = 1, bool canPlace = true)
        {
            if (ghostRenderer == null)
                return;

            ghostRenderer.gameObject.SetActive(active);
            ghostRenderer.enabled = active;

            if (active)
            {
                ghostRenderer.sortingOrder = 15;
                if (sprite != null)
                    ghostRenderer.sprite = sprite;

                lastGhostGridPos = gridPos;
                lastGhostWidth = width;
                lastGhostHeight = height;

                float scaleX = 1f, scaleY = 1f;
                if (ghostRenderer.sprite != null)
                {
                    float spriteWidthUnits = ghostRenderer.sprite.rect.width / ghostRenderer.sprite.pixelsPerUnit;
                    float spriteHeightUnits = ghostRenderer.sprite.rect.height / ghostRenderer.sprite.pixelsPerUnit;
                    scaleX = (gridManager.cellSize * width) / spriteWidthUnits;
                    scaleY = (gridManager.cellSize * height) / spriteHeightUnits;
                }
                ghostRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);

                ghostRenderer.color = canPlace ? new Color(0f, 1f, 1f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
            }
        }
        #endregion

        #region UI Methods
        public void OnPlaceModeSelected()
        {
            Debug.Log("[InputHandler] Switch to Place mode");
            if (playModeManager != null)
                playModeManager.SetPlaceMode();
        }

        public void OnRemoveModeSelected()
        {
            Debug.Log("[InputHandler] Switch to Remove mode");
            if (playModeManager != null)
                playModeManager.SetRemoveMode();
        }

        public void OnNoneModeSelected()
        {
            Debug.Log("[InputHandler] Switch to None mode");
            if (playModeManager != null)
                playModeManager.SetNoneMode();
        }

        public void BeginDragBuilding(BuildingDataSO buildingData)
        {
            Debug.Log("[InputHandler] Begin drag/select " + (buildingData != null ? buildingData.name : "null"));
            isDraggingFromButton = true;
            dragBuildingData = buildingData;
            selectedBuildingData = buildingData;
            SetGhost(true, buildingData.buildingSprite);
            buildingManager.SetActiveBuilding(buildingData);
        }

        public void EndDragBuilding()
        {
            Debug.Log("[InputHandler] End drag/select");
            isDraggingFromButton = false;
            dragBuildingData = null;
            SetGhost(false);
            gridManager.ClearHighlight();
        }

        public void SelectBuilding(BuildingDataSO buildingData)
        {
            Debug.Log("[InputHandler] Toggle select building: " + (buildingData != null ? buildingData.name : "null"));
            if (selectedBuildingData == buildingData)
            {
                selectedBuildingData = null;
                SetGhost(false);
                gridManager.ClearHighlight();
                buildingManager.SetActiveBuilding(null);
                isDraggingFromButton = false;
                dragBuildingData = null;
            }
            else
            {
                selectedBuildingData = buildingData;
                SetGhost(true, buildingData.buildingSprite);
                buildingManager.SetActiveBuilding(buildingData);
                isDraggingFromButton = true;
                dragBuildingData = buildingData;
            }
        }
        #endregion
    }
}