using UnityEngine;
using System.Collections.Generic;
using Zenject;
using Ouiki.SiliconeHeart.Buildings;
using TMPro;
using UnityEngine.UI;
using Ouiki.SiliconeHeart.Input;

namespace Ouiki.SiliconeHeart.UI
{
    public class BuildingPanelPopulator : MonoBehaviour
    {
        #region Fields
        [Inject] private List<BuildingDataSO> buildingTypes;
        [SerializeField] private InfiniteScrollPanel infiniteScrollPanel;
        [SerializeField] private GameObject buildingButtonPrefab;
        [Inject] private InputHandler inputHandler;
        #endregion

        #region Unity Lifecycle

        void Start()
        {
            Debug.Log("[BuildingPanelPopulator] Start called. Populating panel.");
            PopulatePanel();
        }
        #endregion

        #region Panel Population
        public void PopulatePanel()
        {
            Debug.Log("[BuildingPanelPopulator] PopulatePanel called.");

            if (buildingTypes == null)
            {
                Debug.LogError("[BuildingPanelPopulator] buildingTypes is NULL!");
                return;
            }
            if (infiniteScrollPanel == null)
            {
                Debug.LogError("[BuildingPanelPopulator] infiniteScrollPanel is NULL!");
                return;
            }
            if (buildingButtonPrefab == null)
            {
                Debug.LogError("[BuildingPanelPopulator] buildingButtonPrefab is NULL!");
                return;
            }

            List<GameObject> buttons = new();
            for (int i = 0; i < buildingTypes.Count; i++)
            {
                var data = buildingTypes[i];
                if (data == null)
                {
                    Debug.LogError($"[BuildingPanelPopulator] Encountered NULL data at index {i}!");
                    continue;
                }

                Debug.Log($"[BuildingPanelPopulator] Creating button for building: {data.buildingName}");

                var btnObj = Instantiate(buildingButtonPrefab);
                btnObj.name = $"BuildingButton_{data.buildingName}_{i}";
                btnObj.SetActive(true);

                var buttonComp = btnObj.GetComponent<BuildingButton>();
                if (buttonComp != null)
                {
                    buttonComp.SetBuildingData(data,inputHandler);

                    if (buttonComp.buildingNameText != null)
                    {
                        buttonComp.buildingNameText.text = data.buildingName;
                        Debug.Log($"[BuildingPanelPopulator] Set buildingNameText for {data.buildingName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[BuildingPanelPopulator] buildingNameText reference missing on prefab for {data.buildingName}");
                    }

                    if (buttonComp.buildingImage != null)
                    {
                        buttonComp.buildingImage.sprite = data.buildingSprite;
                        Debug.Log($"[BuildingPanelPopulator] Set buildingImage sprite for {data.buildingName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[BuildingPanelPopulator] buildingImage reference missing on prefab for {data.buildingName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[BuildingPanelPopulator] No BuildingButton component found on prefab for {data.buildingName}");
                }

                buttons.Add(btnObj);
            }

            Debug.Log($"[BuildingPanelPopulator] Passing {buttons.Count} buttons to InfiniteScrollPanel.Populate");
            infiniteScrollPanel.Populate(buttons);
        }
        #endregion
    }
}