using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Ouiki.SiliconeHeart.Buildings;
using Ouiki.SiliconeHeart.Input;

public class BuildingButton : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public BuildingDataSO buildingData;
    public TMP_Text buildingNameText;
    public Image buildingImage;

    private InputHandler inputHandler;
    private bool dragging = false;

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
        // Toggle select/deselect building for placement (works for both mouse and touch)
        if (inputHandler == null || buildingData == null) return;
        inputHandler.SelectBuilding(buildingData);
        dragging = true;
        inputHandler.BeginDragBuilding(buildingData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Start drag-from-button (mobile/touch support)
        if (inputHandler == null || buildingData == null) return;
        dragging = true;
        inputHandler.BeginDragBuilding(buildingData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // For desktop mouse drag (works with EventSystem)
        if (inputHandler == null || buildingData == null) return;
        dragging = true;
        inputHandler.BeginDragBuilding(buildingData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Drag ghost follows pointer (handled in InputHandler)
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // End drag-from-button (mobile/touch support)
        if (inputHandler == null) return;
        if (dragging)
        {
            inputHandler.EndDragBuilding();
            dragging = false;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // End drag (desktop/mouse)
        if (inputHandler == null) return;
        inputHandler.EndDragBuilding();
        dragging = false;
    }
}