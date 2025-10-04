using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Ouiki.SiliconeHeart.Buildings;
using Ouiki.SiliconeHeart.Input;

public class BuildingButton : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IEndDragHandler
{
    public BuildingDataSO buildingData;
    public TMP_Text buildingNameText;
    public Image buildingImage;

    private InputHandler inputHandler;

    // Call this when populating the button
    public void SetBuildingData(BuildingDataSO data, InputHandler handler)
    {
        buildingData = data;
        inputHandler = handler;
        if (buildingNameText != null)
            buildingNameText.text = data.buildingName;
        if (buildingImage != null)
            buildingImage.sprite = data.buildingSprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inputHandler == null || buildingData == null) return;
        inputHandler.SelectBuilding(buildingData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inputHandler == null || buildingData == null) return;
        inputHandler.BeginDragBuilding(buildingData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (inputHandler == null) return;
        inputHandler.EndDragBuilding();
    }
}