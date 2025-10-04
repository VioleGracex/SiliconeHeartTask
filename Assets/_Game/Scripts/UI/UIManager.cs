namespace Ouiki.SiliconeHeart.UI
{
    using UnityEngine;
    using TMPro;
    using Ouiki.SiliconeHeart.Buildings;
    using Ouiki.SiliconeHeart.GridSystem;
    using Zenject;
    using System;

    public class UIManager : MonoBehaviour
    {
        #region Fields
        [Header("TextMeshPro UI")]
        [SerializeField] private TMP_Text modeIndicatorText;

        private GridManager gridManager;
        private BuildingManager buildingManager;

        private GameObject currentSilhouette;
        #endregion

        #region Zenject Injection
        [Inject]
        public void Construct(BuildingManager bm, GridManager gm)
        {
            buildingManager = bm;
            gridManager = gm;
            SubscribeToModeChanges();
            UpdateModeIndicator();
        }
        #endregion

        #region Mode Indicator
        /// <summary>
        /// Subscribe to mode change events on BuildingManager.
        /// </summary>
        private void SubscribeToModeChanges()
        {
            if (buildingManager != null)
            {
                buildingManager.OnBuildModeChanged -= OnBuildModeChanged;
                buildingManager.OnBuildModeChanged += OnBuildModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (buildingManager != null)
            {
                buildingManager.OnBuildModeChanged -= OnBuildModeChanged;
            }
        }

        /// <summary>
        /// Called when build mode changes.
        /// </summary>
        private void OnBuildModeChanged(BuildMode mode)
        {
            UpdateModeIndicator();
        }

        /// <summary>
        /// Updates the mode indicator display based on the current build mode.
        /// </summary>
        public void UpdateModeIndicator()
        {
            string text = "Game Mode";
            if (buildingManager != null)
            {
                switch (buildingManager.CurrentMode)
                {
                    case BuildMode.Place:
                        text = "Place Mode";
                        break;
                    case BuildMode.Remove:
                        text = "Remove Mode";
                        break;
                    case BuildMode.None:
                        text = "Game Mode";
                        break;
                }
            }
            SetModeIndicator(text);
        }

        /// <summary>
        /// Sets the mode indicator text.
        /// </summary>
        public void SetModeIndicator(string mode)
        {
            if (modeIndicatorText != null)
                modeIndicatorText.text = mode;
        }
        #endregion

        #region Building Silhouette
        /// <summary>
        /// Shows a semi-transparent silhouette of the building at the grid position with color-coded highlight.
        /// </summary>
        public void ShowSilhouette(Vector2Int gridPos, BuildingDataSO building, bool canPlace)
        {
            HideSilhouette();

            currentSilhouette = new GameObject("Silhouette", typeof(SpriteRenderer));
            var sr = currentSilhouette.GetComponent<SpriteRenderer>();
            sr.sprite = building.buildingSprite;
            sr.color = canPlace
                ? new Color(0, 1, 1, 0.5f)    // Cyan, semi-transparent for placeable
                : new Color(1, 0, 0, 0.5f);   // Red, semi-transparent for blocked

            if (gridManager != null)
                currentSilhouette.transform.position = gridManager.CellWorldPos(gridPos);

            // Only call highlight methods if GridManager implements them
            if (gridManager != null)
            {
                if (HasMethod(gridManager, "ClearHighlight"))
                    gridManager.ClearHighlight();
                if (HasMethod(gridManager, "HighlightArea"))
                    gridManager.HighlightArea(gridPos, building.width, building.height, canPlace);
            }
        }

        /// <summary>
        /// Hides the building silhouette and clears grid highlights.
        /// </summary>
        public void HideSilhouette()
        {
            if (currentSilhouette != null)
            {
                Destroy(currentSilhouette);
                currentSilhouette = null;
            }
            if (gridManager != null && HasMethod(gridManager, "ClearHighlight"))
                gridManager.ClearHighlight();
        }
        #endregion

        #region Utility
        // Checks if the method exists in gridManager to prevent missing method errors.
        private bool HasMethod(object obj, string methodName)
        {
            return obj.GetType().GetMethod(methodName) != null;
        }
        #endregion
    }
}