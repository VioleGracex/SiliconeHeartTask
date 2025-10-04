using UnityEngine;

namespace Ouiki.SiliconeHeart.Obstacles
{
    [CreateAssetMenu(menuName = "Environment/ObstacleData")]
    public class ObstacleDataSO : ScriptableObject
    {
        #region Fields
        public string obstacleName;
        public Sprite obstacleSprite;
        public int width = 1;
        public int height = 1;
        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(obstacleName))
            {
                obstacleName = name;
            }
        }
#endif
    }
}