namespace Ouiki.SiliconeHeart.Buildings
{
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [CreateAssetMenu(menuName = "Building/BuildingData")]
    public class BuildingDataSO : ScriptableObject
    {
        [HideInInspector]
        public string buildingID; // Unique, auto-generated

        public string buildingName;
        [TextArea]
        public string description;
        public Sprite buildingSprite;
        public int width = 1; // tiles in X
        public int height = 1; // tiles in Y

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(buildingID))
            {
                buildingID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}