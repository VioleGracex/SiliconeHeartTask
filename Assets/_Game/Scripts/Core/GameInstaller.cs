namespace Ouiki.SiliconeHeart.Core
{
    using UnityEngine;
    using Zenject;
    using Ouiki.SiliconeHeart.GridSystem;
    using Ouiki.SiliconeHeart.Buildings;
    using Ouiki.SiliconeHeart.Persistence;
    using Ouiki.SiliconeHeart.UI;
    using Ouiki.SiliconeHeart.Input;
    using System.Collections.Generic;
    using Ouiki.SiliconeHeart.PlayGameMode;

    public class GameInstaller : MonoInstaller
    {
        public GridManager gridManager;
        public BuildingManager buildingManager;
        public SaveLoadManager saveLoadManager;
        public UIManager uiManager;
        public BuildingPanelPopulator buildingPanelPopulator;
        public MapGenerator mapGenerator;
        public InputHandler inputHandler; 
        public InfiniteScrollPanel infiniteScrollPanel;
        public MainUIController mainUIController;
        public PlayModeManager playModeManager; 
        public List<BuildingDataSO> buildingTypes;
     

        public override void InstallBindings()
        {
            Container.Bind<GridManager>().FromInstance(gridManager).AsSingle();
            Container.Bind<BuildingManager>().FromInstance(buildingManager).AsSingle();
            Container.Bind<SaveLoadManager>().FromInstance(saveLoadManager).AsSingle();
            Container.Bind<UIManager>().FromInstance(uiManager).AsSingle();
            Container.Bind<BuildingPanelPopulator>().FromInstance(buildingPanelPopulator).AsSingle();
            Container.Bind<MapGenerator>().FromInstance(mapGenerator).AsSingle();
            Container.Bind<InputHandler>().FromInstance(inputHandler).AsSingle();
            Container.Bind<InfiniteScrollPanel>().FromInstance(infiniteScrollPanel).AsSingle();
            Container.Bind<MainUIController>().FromInstance(mainUIController).AsSingle();

            Container.Bind<List<BuildingDataSO>>().FromInstance(buildingTypes).AsSingle();

            Container.Bind<PlayModeManager>().FromInstance(playModeManager).AsSingle(); // <-- Bind PlayModeManager
        }
    }
}