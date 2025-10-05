using UnityEngine;
using System;

namespace Ouiki.SiliconeHeart.Buildings
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class FieldBuilding : MonoBehaviour
    {
        public string buildingID;
        public Vector2Int gridPos;
        public int width;
        public int height;
        public Sprite buildingSprite;

        public SpriteRenderer SpriteRenderer { get; private set; }
        public BoxCollider2D BoxCollider2D { get; private set; }

        public event Action<FieldBuilding> OnBuildingDeleted;
        private bool isDeleted = false;

        void Awake()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();

            // Remove any existing collider to reset
            PolygonCollider2D existingPoly = GetComponent<PolygonCollider2D>();
            if (existingPoly != null)
                Destroy(existingPoly);

            BoxCollider2D = GetComponent<BoxCollider2D>();
            if (BoxCollider2D == null)
                BoxCollider2D = gameObject.AddComponent<BoxCollider2D>();

            // Set collider size to match grid size and cell size
            float cellSize = GetCellSize();
            BoxCollider2D.size = new Vector2(width * cellSize, height * cellSize);
            BoxCollider2D.offset = Vector2.zero; // Centered on object
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