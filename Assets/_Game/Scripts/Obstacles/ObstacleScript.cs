using UnityEngine;
using Ouiki.SiliconeHeart.Obstacles;

namespace Ouiki.SiliconeHeart.Obstacles
{
    public class ObstacleScript : MonoBehaviour
    {
        #region Fields
        public ObstacleDataSO data;
        public Vector2Int GridPosition;
        public int Width => data != null ? data.width : 1;
        public int Height => data != null ? data.height : 1;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            Debug.Log($"[ObstacleScript] Awake for {gameObject.name}, GridPos={GridPosition}, Size={Width}x{Height}");
        }
        #endregion
    }
}