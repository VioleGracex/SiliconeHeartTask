using UnityEngine;
using System;

namespace Ouiki.SiliconeHeart.Buildings
{
    public class FieldBuilding : MonoBehaviour
    {
        public string buildingID;
        public Vector2Int gridPos;
        public int width;
        public int height;
        public Sprite buildingSprite;

        public SpriteRenderer SpriteRenderer { get; private set; }
        public PolygonCollider2D PolygonCollider2D { get; private set; }

        public event Action<FieldBuilding> OnBuildingDeleted;
        private bool isDeleted = false;

        void Awake()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();

            // Remove any existing PolygonCollider2D to reset
            PolygonCollider2D existingPoly = GetComponent<PolygonCollider2D>();
            if (existingPoly != null)
                Destroy(existingPoly);

            // Add new PolygonCollider2D
            PolygonCollider2D = gameObject.AddComponent<PolygonCollider2D>();

            // Set collider shape to a rectangle matching grid size and cell size
            if (SpriteRenderer != null)
            {
                float cellSize = GetCellSize();
                float colliderWidth = width * cellSize;
                float colliderHeight = height * cellSize;

                // Centered at 0,0
                Vector2 halfSize = new Vector2(colliderWidth / 2f, colliderHeight / 2f);

                // Vertices for rectangle (clockwise)
                Vector2[] rectVertices = new Vector2[]
                {
                    new Vector2(-halfSize.x, -halfSize.y),
                    new Vector2(-halfSize.x, halfSize.y),
                    new Vector2(halfSize.x, halfSize.y),
                    new Vector2(halfSize.x, -halfSize.y)
                };

                PolygonCollider2D.SetPath(0, rectVertices);
            }
        }

        /// <summary>
        /// Utility to get cell size from parent grid manager if possible.
        /// </summary>
        private float GetCellSize()
        {
            var gridManager = FindObjectOfType<Ouiki.SiliconeHeart.GridSystem.GridManager>();
            return gridManager != null ? gridManager.cellSize : 1f;
        }

        public void TintRed()
        {
            if (SpriteRenderer != null)
                SpriteRenderer.color = new Color(1f, 0.5f, 0.5f, 1f);
        }

        public void ClearTint()
        {
            if (SpriteRenderer != null)
                SpriteRenderer.color = Color.white;
        }

        // Do NOT destroy here. Only notify manager.
        public void RequestDelete()
        {
            if (isDeleted) return;
            isDeleted = true;
            OnBuildingDeleted?.Invoke(this);
            OnBuildingDeleted = null;
        }
    }
}