namespace Ouiki.SiliconeHeart.Core
{
    using UnityEngine;
    using Ouiki.SiliconeHeart.GridSystem;
    using Ouiki.SiliconeHeart.Buildings;
    using Ouiki.SiliconeHeart.Persistence;
    using Ouiki.SiliconeHeart.UI;
    using Zenject;
    using Ouiki.SiliconeHeart.Input;
using Ouiki.SiliconeHeart.PlayGameMode;


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
        [Inject] private PlayModeManager gameModeManager; // <--- Injected, not static

        void Start()
        {
            // Generate map, setup grid, populate building panel
            mapGenerator.GenerateGround();
            gridManager.Initialize();
            buildingPanelPopulator.PopulatePanel();

            // Setup UI and listeners
            mainUIController.Init();

            // Set initial game mode using injected GameModeManager
            if (gameModeManager != null)
                gameModeManager.SetNoneMode();

            inputHandler.Init();
            infiniteScrollPanel.EnsureLayoutGroup();
        }
    }
}