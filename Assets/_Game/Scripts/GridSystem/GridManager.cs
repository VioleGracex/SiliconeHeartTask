using UnityEngine;
using System.Collections.Generic;
using Ouiki.SiliconeHeart.Buildings;
using Zenject;

namespace Ouiki.SiliconeHeart.GridSystem
{
    #region Types
    public enum CellType { Empty, Occupied, Blocked }

    public class Cell
    {
        public Vector2Int position;
        public CellType type;
        public Cell(Vector2Int pos, CellType t) { position = pos; type = t; }
    }
    #endregion

    public class GridManager : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Grid Settings")]
        public int gridWidth = 20, gridHeight = 12;
        public float cellSize = 1.0f;

        [Header("Layer Parents")]
        public Transform groundParent;
        public Transform overlayParent;

        [Header("Overlay Sprite")]
        public Sprite overlaySprite;

        [Header("Colors")]
        public Color overlayColor = new Color(1f, 1f, 1f, 0.2f);
        public Color highlightColor = new Color(0f, 1f, 1f, 0.4f);
        public Color blockedHighlightColor = new Color(1f, 0f, 0f, 0.4f);
        public Color solidRedColor = new Color(1f, 0f, 0f, 0.8f);
        #endregion

        #region Private Fields
        private Cell[,] grid;
        private List<GameObject> highlightedOverlayObjects = new();
        [Inject] private BuildingManager buildingManager;
        #endregion

        #region Initialization
        public void Initialize()
        {
            grid = new Cell[gridWidth, gridHeight];

            if (overlayParent != null)
            {
                var children = new List<GameObject>();
                foreach (Transform t in overlayParent) children.Add(t.gameObject);
                foreach (var go in children) DestroyImmediate(go);
            }

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = new Cell(new Vector2Int(x, y), CellType.Empty);
                    Vector3 cellPos = transform.position + new Vector3(x * cellSize, y * cellSize, 0f);

                    if (overlayParent != null)
                    {
                        GameObject overlayObj = new GameObject($"Overlay_{x}_{y}");
                        overlayObj.transform.SetParent(overlayParent);
                        overlayObj.transform.position = cellPos;
                        overlayObj.transform.localScale = Vector3.one * cellSize;
                        var sr = overlayObj.AddComponent<SpriteRenderer>();
                        sr.sprite = overlaySprite;
                        sr.sortingOrder = 5;
                        sr.color = overlayColor;
                    }
                }
            }
            overlayParent.gameObject.SetActive(false);
        }
        #endregion

        #region Overlay and Highlight
        public void UpdateOverlayVisibility()
        {
            if (overlayParent == null || buildingManager == null)
                return;
            bool showOverlay = buildingManager.CurrentMode == Ouiki.SiliconeHeart.Buildings.BuildMode.Place
                            || buildingManager.CurrentMode == Ouiki.SiliconeHeart.Buildings.BuildMode.Remove;
            overlayParent.gameObject.SetActive(showOverlay);
        }

        public void HighlightArea(Vector2Int pos, int width, int height, bool canPlace)
        {
            Color color = canPlace ? highlightColor : blockedHighlightColor;
            HighlightAreaColor(pos, width, height, color);
        }

        public void HighlightAreaColor(Vector2Int pos, int width, int height, Color color)
        {
            ClearHighlight();
            for (int x = pos.x; x < pos.x + width; x++)
            {
                for (int y = pos.y; y < pos.y + height; y++)
                {
                    if (IsValidCell(x, y) && overlayParent != null)
                    {
                        Transform overlayObj = overlayParent.Find($"Overlay_{x}_{y}");
                        if (overlayObj != null)
                        {
                            var sr = overlayObj.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                sr.color = color;
                                highlightedOverlayObjects.Add(overlayObj.gameObject);
                            }
                        }
                    }
                }
            }
        }

        public void ClearHighlight()
        {
            foreach (var obj in highlightedOverlayObjects)
            {
                if (obj != null)
                {
                    var sr = obj.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = overlayColor;
                }
            }
            highlightedOverlayObjects.Clear();
        }
        #endregion

        #region Grid Logic
        public bool IsValidCell(int x, int y)
        {
            return x >= -2 && y >= -2 && x < gridWidth + 2 && y < gridHeight + 2;
        }

        public bool IsAreaPlaceable(Vector2Int pos, int width, int height)
        {
            // Instead of error, just return false if any cell is out of bounds
            for (int x = pos.x; x < pos.x + width; x++)
                for (int y = pos.y; y < pos.y + height; y++)
                {
                    if (!IsCellPlaceable(x, y))
                        return false;
                }
            return true;
        }

        public bool IsCellPlaceable(int x, int y)
        {
            bool valid = IsValidCell(x, y);
            bool empty = valid && x >= 0 && y >= 0 && x < gridWidth && y < gridHeight && grid[x, y].type == CellType.Empty;
            return empty;
        }

        public void SetAreaOccupied(Vector2Int pos, int width, int height, bool value)
        {
            for (int x = pos.x; x < pos.x + width; x++)
                for (int y = pos.y; y < pos.y + height; y++)
                {
                    if (x >= 0 && y >= 0 && x < gridWidth && y < gridHeight)
                    {
                        grid[x, y].type = value ? CellType.Occupied : CellType.Empty;
                    }
                }
        }

        public CellType GetCellType(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < gridWidth && y < gridHeight) return grid[x, y].type;
            return CellType.Blocked;
        }
        #endregion

        #region Utility
        public Vector3 CellWorldPos(Vector2Int cell)
        {
            return transform.position + new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
        }

        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            Vector2 localPos = (Vector2)worldPos - (Vector2)transform.position;
            return new Vector2Int(Mathf.FloorToInt(localPos.x / cellSize), Mathf.FloorToInt(localPos.y / cellSize));
        }
        #endregion
    }
}