using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Zenject;
using Ouiki.SiliconeHeart.Buildings;
using Ouiki.SiliconeHeart.Persistence;
using Ouiki.SiliconeHeart.GridSystem;
using DG.Tweening;
using Ouiki.SiliconeHeart.PlayGameMode;

namespace Ouiki.SiliconeHeart.Core
{
    public class MainUIController : MonoBehaviour
    {
        #region Inspector
        [Header("Mode Buttons")]
        public Button placeModeButton;
        public Button removeModeButton;

        [Header("Mode Highlights (optional)")]
        public Image placeHighlight;
        public Image removeHighlight;

        [Header("Save/Load Buttons")]
        public Button saveButton;
        public Button loadButton;

        [Header("Building Panel")]
        public RectTransform buildingPanel;
        public float panelAnimationDuration = 0.35f;
        public float panelHiddenY = -600f;
        public float panelVisibleY = 0f;

        [Header("Input Actions")]
        public InputActionAsset inputActions; // Assign in inspector
        #endregion

        #region Injected
        [Inject] BuildingManager buildingManager;
        [Inject] SaveLoadManager saveLoadManager;
        [Inject] GridManager gridManager;
        [Inject] PlayModeManager playModeManager; // <-- Injected PlayModeManager
        #endregion

        #region Public Init
        public void Init()
        {
            Debug.Log("[MainUIController] Init called");
            SetupListeners();
            SetupInputActions();
            UpdateModeVisuals();
            SetBuildingPanelVisible(false, true);
        }
        #endregion

        #region Listeners
        void SetupListeners()
        {
            if (placeModeButton != null)
                placeModeButton.onClick.AddListener(() => OnModeButtonClicked(GamePlayMode.Place));
            if (removeModeButton != null)
                removeModeButton.onClick.AddListener(() => OnModeButtonClicked(GamePlayMode.Remove));
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadClicked);
        }

        void SetupInputActions()
        {
            if (inputActions == null) return;

            var map = inputActions.FindActionMap("UI");
            if (map == null) map = inputActions.actionMaps[0]; // fallback

            map.FindAction("Place", true).performed += ctx => OnModeButtonClicked(GamePlayMode.Place);
            map.FindAction("Remove", true).performed += ctx => OnModeButtonClicked(GamePlayMode.Remove);
            map.FindAction("Save", true).performed += ctx => OnSaveClicked();
            map.FindAction("Load", true).performed += ctx => OnLoadClicked();

            inputActions.Enable();
        }

        void OnModeButtonClicked(GamePlayMode mode)
        {
            Debug.Log($"[MainUIController] Mode button clicked: {mode}");
            if (playModeManager == null) return;

            if (playModeManager.CurrentMode == mode)
            {
                playModeManager.SetNoneMode();
            }
            else
            {
                playModeManager.SetMode(mode);
            }

            UpdateModeVisuals();
            SetBuildingPanelVisible(playModeManager.CurrentMode == GamePlayMode.Place);

            if (gridManager != null)
                gridManager.UpdateOverlayVisibility();
        }

        void OnSaveClicked()
        {
            Debug.Log("[MainUIController] Save button clicked");
            saveLoadManager.Save();
        }

        void OnLoadClicked()
        {
            Debug.Log("[MainUIController] Load button clicked");
            saveLoadManager.Load();
        }
        #endregion

        #region UI Feedback
        void UpdateModeVisuals()
        {
            if (playModeManager == null) return;

            if (placeHighlight != null)
                placeHighlight.enabled = (playModeManager.CurrentMode == GamePlayMode.Place);
            if (removeHighlight != null)
                removeHighlight.enabled = (playModeManager.CurrentMode == GamePlayMode.Remove);

            if (placeModeButton != null)
                placeModeButton.interactable = true;
            if (removeModeButton != null)
                removeModeButton.interactable = true;

            Debug.Log($"[MainUIController] UpdateModeVisuals: CurrentMode = {playModeManager.CurrentMode}");
        }
        #endregion

        #region Panel Animation
        void SetBuildingPanelVisible(bool visible, bool instant = false)
        {
            if (buildingPanel == null)
            {
                Debug.LogWarning("[MainUIController] buildingPanel reference is missing!");
                return;
            }

            buildingPanel.DOKill();

            if (!visible)
            {
                buildingPanel.gameObject.SetActive(false);
            }
            else
            {
                buildingPanel.gameObject.SetActive(true);
            }

            float targetY = visible ? panelVisibleY : panelHiddenY;
            if (instant)
            {
                buildingPanel.anchoredPosition = new Vector2(buildingPanel.anchoredPosition.x, targetY);
                Debug.Log($"[MainUIController] SetBuildingPanelVisible (instant): {visible}, Y={targetY}");
            }
            else
            {
                buildingPanel.DOAnchorPosY(targetY, panelAnimationDuration)
                    .SetEase(visible ? Ease.OutBack : Ease.InBack)
                    .OnComplete(() => {
                        Debug.Log($"[MainUIController] SetBuildingPanelVisible animated. Visible={visible}, Y={targetY}");
                        if (!visible) buildingPanel.gameObject.SetActive(false);
                    });
            }
        }
        #endregion
    }
}