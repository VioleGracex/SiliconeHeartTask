namespace Ouiki.SiliconeHeart.Buildings
{
    using UnityEngine;
    using NaughtyAttributes;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [CreateAssetMenu(menuName = "Building/BuildingData")]
    public class BuildingDataSO : ScriptableObject
    {
        [SerializeField, ReadOnly]
        private string buildingID; // Unique, auto-generated

        public string BuildingID => buildingID; // Public getter, read-only

        [ReadOnly]
        public string buildingName;
        [TextArea]
        public string description;
        public Sprite buildingSprite;
        public int width = 1; // tiles in X
        public int height = 1; // tiles in Y

#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private string lastAssetPath; // To track duplication

        private void OnValidate()
        {
            string currentPath = AssetDatabase.GetAssetPath(this);

            // Assign new ID if missing, or if asset was duplicated (path changed)
            if (string.IsNullOrEmpty(buildingID) ||
                (!string.IsNullOrEmpty(lastAssetPath) && lastAssetPath != currentPath))
            {
                buildingID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(this);
            }

            // Set buildingName to the actual asset file name (without extension)
            string assetName = System.IO.Path.GetFileNameWithoutExtension(currentPath);
            if (!string.IsNullOrEmpty(assetName) && buildingName != assetName)
            {
                buildingName = assetName;
                EditorUtility.SetDirty(this);
            }

            lastAssetPath = currentPath;
        }
#endif
    }
}