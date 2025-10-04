using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Ouiki.SiliconeHeart.GridSystem;
using Ouiki.SiliconeHeart.Buildings;
using Zenject;
using Ouiki.SiliconeHeart.PlayGameMode;

namespace Ouiki.SiliconeHeart.Input
{
    public class InputHandler : MonoBehaviour
    {
        #region References and State
        [Header("References")]
        [Inject] private GridManager gridManager;
        [Inject] private BuildingManager buildingManager;
        [Inject] private PlayModeManager playModeManager; // Injected, not static
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
        #endregion

        #region Initialization
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

        #region Update and Input
        void Update()
        {
            if (playModeManager != null && playModeManager.CurrentMode != previousMode)
            {
                OnModeChanged(playModeManager.CurrentMode, previousMode);
                previousMode = playModeManager.CurrentMode;
            }

            HandleInput();
            UpdateGhostPosition();
        }

        private void OnModeChanged(GamePlayMode newMode, GamePlayMode oldMode)
        {
            SetGhost(false);
            dragBuildingData = null;
            selectedBuildingData = null;
            isDraggingFromButton = false;

            if (oldMode == GamePlayMode.Remove || oldMode == GamePlayMode.Place)
            {
                gridManager.ClearHighlight();
            }
        }

        void HandleInput()
        {
            if (gridManager == null || buildingManager == null || mainCamera == null || playModeManager == null)
                return;

            Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            float zDistance = -mainCamera.transform.position.z;
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(
                new Vector3(mouseScreenPos.x, mouseScreenPos.y, zDistance)
            );
            mouseWorld.z = 0f;
            Vector2Int gridPos = GetCenteredGridPosition(mouseWorld);

            // Defensive: Ignore if gridPos is out of bounds (mouse outside map)
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
                    bool removed = buildingManager.TryRemoveBuildingAt(gridPos);
                    if (removed)
                    {
                        gridManager.ClearHighlight();
                        SetGhost(false);
                    }
                }
                return;
            }

            if (isDraggingFromButton && dragBuildingData != null)
            {
                bool validCell = gridManager.IsValidCell(gridPos.x, gridPos.y);
                bool canPlace = validCell && gridManager.IsAreaPlaceable(gridPos, dragBuildingData.width, dragBuildingData.height);

                SetGhost(true, dragBuildingData.buildingSprite, gridPos, dragBuildingData.width, dragBuildingData.height, canPlace);
                gridManager.HighlightArea(gridPos, dragBuildingData.width, dragBuildingData.height, canPlace);

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    if (canPlace)
                    {
                        selectedBuildingData = dragBuildingData;
                        buildingManager.SetActiveBuilding(dragBuildingData);
                        buildingManager.TryPlaceBuilding(gridPos);
                    }
                    else
                    {
                        selectedBuildingData = null;
                        SetGhost(false);
                        gridManager.ClearHighlight();
                    }
                    isDraggingFromButton = false;
                    dragBuildingData = null;
                }
                return;
            }

            if (playModeManager.CurrentMode == GamePlayMode.Place && buildingManager.activeBuilding != null)
            {
                bool validCell = gridManager.IsValidCell(gridPos.x, gridPos.y);
                bool canPlace = validCell &&
                    gridManager.IsAreaPlaceable(gridPos, buildingManager.activeBuilding.width, buildingManager.activeBuilding.height);

                SetGhost(true, buildingManager.activeBuilding.buildingSprite, gridPos, buildingManager.activeBuilding.width, buildingManager.activeBuilding.height, canPlace);
                gridManager.HighlightArea(gridPos, buildingManager.activeBuilding.width, buildingManager.activeBuilding.height, canPlace);

                if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
                {
                    if (canPlace)
                    {
                        bool placed = buildingManager.TryPlaceBuilding(gridPos);
                        if (!placed)
                            Debug.LogWarning("[InputHandler] Cannot place building here (blocked or out of bounds)");
                    }
                    else
                    {
                        Debug.LogWarning("[InputHandler] Cannot place building here (blocked or out of bounds)");
                    }
                }
                return;
            }

            gridManager.ClearHighlight();
            SetGhost(false);
        }
        #endregion

        #region Ghost (preview) Management
        Vector2Int GetCenteredGridPosition(Vector3 mouseWorld)
        {
            int w = 1, h = 1;
            if (isDraggingFromButton && dragBuildingData != null)
            {
                w = dragBuildingData.width;
                h = dragBuildingData.height;
            }
            else
            {
                if (playModeManager != null && playModeManager.CurrentMode == GamePlayMode.Place && buildingManager.activeBuilding != null)
                {
                    w = buildingManager.activeBuilding.width;
                    h = buildingManager.activeBuilding.height;
                }
            }

            Vector2 mouseWorldCentered = mouseWorld;
            mouseWorldCentered.x -= gridManager.cellSize * (w / 2f - 0.5f);
            mouseWorldCentered.y -= gridManager.cellSize * (h / 2f - 0.5f);

            Vector2Int gridPosRaw = gridManager.WorldToGrid(mouseWorldCentered);
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

        #region Public UI Methods
        public void OnPlaceModeSelected()
        {
            if (playModeManager != null)
                playModeManager.SetPlaceMode();
        }

        public void OnRemoveModeSelected()
        {
            if (playModeManager != null)
                playModeManager.SetRemoveMode();
        }

        public void OnNoneModeSelected()
        {
            if (playModeManager != null)
                playModeManager.SetNoneMode();
        }

        public void BeginDragBuilding(BuildingDataSO buildingData)
        {
            isDraggingFromButton = true;
            dragBuildingData = buildingData;
            selectedBuildingData = buildingData;
            SetGhost(true, buildingData.buildingSprite);
            buildingManager.SetActiveBuilding(buildingData);
        }

        public void EndDragBuilding()
        {
            isDraggingFromButton = false;
            dragBuildingData = null;
            SetGhost(false);
            gridManager.ClearHighlight();
        }

        public void SelectBuilding(BuildingDataSO buildingData)
        {
            if (selectedBuildingData == buildingData)
            {
                selectedBuildingData = null;
                SetGhost(false);
                gridManager.ClearHighlight();
            }
            else
            {
                selectedBuildingData = buildingData;
                SetGhost(true, buildingData.buildingSprite);
                buildingManager.SetActiveBuilding(buildingData);
            }
        }
        #endregion
    }
}