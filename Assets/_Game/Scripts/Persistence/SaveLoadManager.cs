using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Ouiki.SiliconeHeart.Buildings;
using Ouiki.SiliconeHeart.GridSystem;
using Zenject;
using Ouiki.SiliconeHeart.Persistence;

namespace Ouiki.SiliconeHeart.Persistence
{
    #region Serializable Data Classes
    [System.Serializable]
    public class PlacedBuildingData
    {
        public string buildingID;
        public int gridPosX, gridPosY;
        public int width, height;
    }

    [System.Serializable]
    public class SaveData
    {
        public List<PlacedBuildingData> placedBuildings = new List<PlacedBuildingData>();
    }
    #endregion

    public class SaveLoadManager : MonoBehaviour
    {
        #region Fields and Injection
        public string saveFilePath => Path.Combine(Application.persistentDataPath, "save.json");

        [Inject] public BuildingManager buildingManager { get; private set; }
        [Inject] public GridManager gridManager { get; private set; }
        [Inject] public List<BuildingDataSO> buildingTypes { get; private set; }
        #endregion

        #region Save
        public void Save()
        {
            Debug.Log("[SaveLoadManager] Save called");

            var data = new SaveData();

            foreach (var building in buildingManager.placedBuildings)
            {
                // Skip destroyed buildings
                if (building == null || building.gameObject == null)
                    continue;

                data.placedBuildings.Add(new PlacedBuildingData
                {
                    buildingID = building.buildingID,
                    gridPosX = building.gridPos.x,
                    gridPosY = building.gridPos.y,
                    width = building.width,
                    height = building.height
                });
            }

            var json = JsonUtility.ToJson(data);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"[SaveLoadManager] SaveData written to {saveFilePath}");
        }
        #endregion

        #region Load
        public void Load()
        {
            Debug.Log("[SaveLoadManager] Load called");
            if (!File.Exists(saveFilePath))
            {
                Debug.LogWarning("[SaveLoadManager] No save file found!");
                return;
            }

            // Remove all existing buildings (manager handles correct deletion)
            foreach (var b in new List<FieldBuilding>(buildingManager.placedBuildings))
            {
                if (b != null)
                    buildingManager.RemoveBuilding(b); // The central removal method
            }
            buildingManager.placedBuildings.Clear();

            // Reset grid cells to empty
            gridManager.Initialize();

            var json = File.ReadAllText(saveFilePath);
            var data = JsonUtility.FromJson<SaveData>(json);

            foreach (var placed in data.placedBuildings)
            {
                var def = buildingTypes.Find(b => b.buildingID == placed.buildingID);
                if (def != null)
                {
                    Vector2Int gridPos = new Vector2Int(placed.gridPosX, placed.gridPosY);
                    buildingManager.activeBuilding = def;
                    buildingManager.TryPlaceBuilding(gridPos);
                }
                else
                {
                    Debug.LogWarning($"[SaveLoadManager] Building definition not found for ID: {placed.buildingID}");
                }
            }
            Debug.Log($"[SaveLoadManager] Loaded {data.placedBuildings.Count} buildings from save");
        }
        #endregion
    }
}