namespace Ouiki.SiliconeHeart.Core
{
    using UnityEngine;
    using Ouiki.SiliconeHeart.GridSystem;
    using Ouiki.SiliconeHeart.Buildings;
    using Ouiki.SiliconeHeart.Persistence;
    using Ouiki.SiliconeHeart.UI;
    using Zenject;
    using Ouiki.SiliconeHeart.Input;


    public class Bootstrap : MonoBehaviour
    {
        [Inject] private GridManager gridManager;
        [Inject] private BuildingManager buildingManager;
        [Inject] private SaveLoadManager saveLoadManager;
        [Inject] private UIManager uiManager;
        [Inject] private BuildingPanelPopulator buildingPanelPopulator;
        [Inject] private MapGenerator mapGenerator;
        [Inject] private MainUIController mainUIController;
        [Inject] private InputHandler inputHandler;
        [Inject] private InfiniteScrollPanel infiniteScrollPanel;

        void Start()
        {
            // Generate map, setup grid, populate building panel
            mapGenerator.GenerateGround();
            gridManager.Initialize();
            buildingPanelPopulator.PopulatePanel();

            // Setup UI and listeners
            mainUIController.Init();

            // Optionally set initial mode and ghost state
            buildingManager.SetMode(BuildMode.None);
            inputHandler.Init();
            infiniteScrollPanel.EnsureLayoutGroup();
            
        }
    }
}