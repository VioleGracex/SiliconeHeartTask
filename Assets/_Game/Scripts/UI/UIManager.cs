namespace Ouiki.SiliconeHeart.UI
{
    using UnityEngine;
    using TMPro;
    using Ouiki.SiliconeHeart.Buildings;
    using Ouiki.SiliconeHeart.GridSystem;
    using Zenject;
    using System;
    using Ouiki.SiliconeHeart.PlayGameMode;


    public class UIManager : MonoBehaviour
    {
        #region Fields
        [Header("TextMeshPro UI")]
        [SerializeField] private TMP_Text modeIndicatorText;

        private GridManager gridManager;
        private BuildingManager buildingManager;
        [Inject] private PlayModeManager playModeManager; // Injected PlayModeManager

        private GameObject currentSilhouette;
        #endregion

        #region Zenject Injection
        [Inject]
        public void Construct(BuildingManager bm, GridManager gm, PlayModeManager pm)
        {
            buildingManager = bm;
            gridManager = gm;
            playModeManager = pm;
            SubscribeToModeChanges();
            UpdateModeIndicator();
        }
        #endregion

        #region Mode Indicator
        /// <summary>
        /// Subscribe to mode change events on PlayModeManager.
        /// </summary>
        private void SubscribeToModeChanges()
        {
            if (playModeManager != null)
            {
                playModeManager.OnPlayModeChanged -= OnPlayModeChanged;
                playModeManager.OnPlayModeChanged += OnPlayModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (playModeManager != null)
            {
                playModeManager.OnPlayModeChanged -= OnPlayModeChanged;
            }
        }

        /// <summary>
        /// Called when play mode changes.
        /// </summary>
        private void OnPlayModeChanged(GamePlayMode mode)
        {
            UpdateModeIndicator();
        }

        /// <summary>
        /// Updates the mode indicator display based on the current play mode.
        /// </summary>
        public void UpdateModeIndicator()
        {
            string text = "Game Mode";
            if (playModeManager != null)
            {
                switch (playModeManager.CurrentMode)
                {
                    case GamePlayMode.Place:
                        text = "Place Mode";
                        break;
                    case GamePlayMode.Remove:
                        text = "Remove Mode";
                        break;
                    case GamePlayMode.None:
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