using UnityEngine;
using System.Collections.Generic;
using Ouiki.SiliconeHeart.GridSystem;
using Zenject;
using System;
using Ouiki.SiliconeHeart.PlayGameMode; // Use your PlayModeManager for mode logic

namespace Ouiki.SiliconeHeart.Buildings
{
    public class BuildingManager : MonoBehaviour
    {
        #region Injected Fields
        [Inject] private GridManager gridManager;
        [Inject] private List<BuildingDataSO> buildingTypes;
        [Inject] private PlayModeManager playModeManager; // <-- Use injected PlayModeManager
        #endregion

        #region Inspector Fields
        public Transform buildingParent;
        #endregion

        #region State Fields
        public List<FieldBuilding> placedBuildings = new List<FieldBuilding>();
        [HideInInspector] public BuildingDataSO activeBuilding;
        private FieldBuilding hoveredBuilding = null;
        #endregion

        #region Placement & Removal
        public void HandleRemoveHover(Vector2Int gridPos)
        {
            FieldBuilding building = placedBuildings.Find(b =>
                gridPos.x >= b.gridPos.x &&
                gridPos.x < b.gridPos.x + b.width &&
                gridPos.y >= b.gridPos.y &&
                gridPos.y < b.gridPos.y + b.height
            );

            if (hoveredBuilding != null && hoveredBuilding != building)
            {
                if (hoveredBuilding.SpriteRenderer != null)
                    hoveredBuilding.ClearTint();
            }

            if (building != null)
            {
                gridManager.HighlightAreaColor(building.gridPos, building.width, building.height, gridManager.solidRedColor);
                building.TintRed();
            }
            else
            {
                gridManager.ClearHighlight();
            }

            hoveredBuilding = building;
        }

        public bool TryPlaceBuilding(Vector2Int gridPos)
        {
            // Only allow placement if PlayModeManager is in Place mode
            if (playModeManager == null || playModeManager.CurrentMode != GamePlayMode.Place || activeBuilding == null)
                return false;

            if (!gridManager.IsAreaPlaceable(gridPos, activeBuilding.width, activeBuilding.height))
                return false;

            gridManager.SetAreaOccupied(gridPos, activeBuilding.width, activeBuilding.height, true);

            var go = new GameObject($"Building_{activeBuilding.buildingName}_{gridPos.x}_{gridPos.y}");
            go.transform.SetParent(buildingParent, false);
            go.layer = LayerMask.NameToLayer("Building");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = activeBuilding.buildingSprite;
            sr.sortingOrder = 30;
            float cellSize = gridManager.cellSize;
            float spriteWidthUnits = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
            float spriteHeightUnits = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
            float scaleX = (cellSize * activeBuilding.width) / spriteWidthUnits;
            float scaleY = (cellSize * activeBuilding.height) / spriteHeightUnits;
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            Vector3 cellWorldPos = gridManager.CellWorldPos(gridPos);
            float offsetX = cellSize * (activeBuilding.width / 2f - 0.5f);
            float offsetY = cellSize * (activeBuilding.height / 2f - 0.5f);
            Vector3 centerPos = cellWorldPos + new Vector3(offsetX, offsetY, 0f);
            go.transform.position = centerPos;

            var fieldBuilding = go.AddComponent<FieldBuilding>();
            fieldBuilding.buildingID = activeBuilding.BuildingID;
            fieldBuilding.gridPos = gridPos;
            fieldBuilding.width = activeBuilding.width;
            fieldBuilding.height = activeBuilding.height;
            fieldBuilding.buildingSprite = activeBuilding.buildingSprite;

            fieldBuilding.OnBuildingDeleted += HandleBuildingDeleted;
            placedBuildings.Add(fieldBuilding);

            Debug.Log($"Added building {fieldBuilding.buildingID} at {gridPos} size {fieldBuilding.width}x{fieldBuilding.height}. Buildings in list: {placedBuildings.Count}");
            DebugPlacedBuildings();
            return true;
        }

        public void RemoveBuilding(FieldBuilding building)
        {
            if (building == null) return;
            building.OnBuildingDeleted -= HandleBuildingDeleted;
            if (placedBuildings.Contains(building))
                placedBuildings.Remove(building);
            gridManager.SetAreaOccupied(building.gridPos, building.width, building.height, false);
            gridManager.ClearHighlight();
            if (building.SpriteRenderer != null)
                building.ClearTint();
            Debug.Log($"Removed building {building.buildingID} at {building.gridPos}. Buildings left: {placedBuildings.Count}");
            DebugPlacedBuildings();
            if (building != null && building.gameObject != null)
                Destroy(building.gameObject);
        }

        public void HandleBuildingDeleted(FieldBuilding deletedBuilding)
        {
            RemoveBuilding(deletedBuilding);
        }

        public bool TryRemoveBuildingAt(Vector2Int gridPos)
        {
            // Only allow removal if PlayModeManager is in Remove mode
            if (playModeManager == null || playModeManager.CurrentMode != GamePlayMode.Remove)
                return false;

            FieldBuilding found = placedBuildings.Find(b =>
                gridPos.x >= b.gridPos.x &&
                gridPos.x < b.gridPos.x + b.width &&
                gridPos.y >= b.gridPos.y &&
                gridPos.y < b.gridPos.y + b.height
            );
            if (found != null)
            {
                RemoveBuilding(found);
                return true;
            }
            Debug.Log("No building found to remove at " + gridPos);
            return false;
        }

        public void SetActiveBuilding(BuildingDataSO buildingData)
        {
            activeBuilding = buildingData;
        }
        #endregion

        #region Debug
        private void DebugPlacedBuildings()
        {
            Debug.Log($"PlacedBuildings count: {placedBuildings.Count}");
            for (int i = 0; i < placedBuildings.Count; i++)
            {
                var b = placedBuildings[i];
                if (b != null)
                    Debug.Log($"[{i}] {b.buildingID} at {b.gridPos} size {b.width}x{b.height} parent={b.transform.parent?.name}");
                else
                    Debug.LogWarning($"[{i}] Building is null (already destroyed)");
            }
        }
        #endregion
    }
}