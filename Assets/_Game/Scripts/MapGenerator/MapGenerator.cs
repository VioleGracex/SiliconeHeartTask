namespace Ouiki.SiliconeHeart.Core
{
    using UnityEngine;
    using System.Collections.Generic;
    using Ouiki.SiliconeHeart.GridSystem;
    using Zenject;
    using System;

    public class MapGenerator : MonoBehaviour
    {
        [Header("Ground Sprites (16x16 pixel art)")]
        public List<Sprite> groundSprites;

        [Header("References")]
        [Inject] private GridManager gridManager;

        public void GenerateGround()
        {
            if (gridManager == null || gridManager.groundParent == null)
            {
                Debug.LogError("[MapGenerator] groundParent not assigned in GridManager!");
                return;
            }
            if (groundSprites == null || groundSprites.Count == 0)
            {
                Debug.LogError("[MapGenerator] groundSprites not assigned or empty!");
                return;
            }

            int gridWidth = gridManager.gridWidth;
            int gridHeight = gridManager.gridHeight;
            float cellSize = gridManager.cellSize;

            var oldTiles = new List<GameObject>();
            foreach (Transform child in gridManager.groundParent)
                oldTiles.Add(child.gameObject);
            foreach (var obj in oldTiles)
                DestroyImmediate(obj);

            int count = 0;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var pos = new Vector3(x * cellSize, y * cellSize, 0f);
                    GameObject tileObj = new GameObject($"Ground_{x}_{y}");
                    tileObj.transform.SetParent(gridManager.groundParent);
                    tileObj.transform.position = pos;

                    var sr = tileObj.AddComponent<SpriteRenderer>();
                    Sprite chosen = groundSprites[UnityEngine.Random.Range(0, groundSprites.Count)];
                    sr.sprite = chosen;

                    float pixelsPerUnit = chosen.pixelsPerUnit;
                    float spriteWorldSize = 16f / pixelsPerUnit;
                    float scale = cellSize / spriteWorldSize;
                    tileObj.transform.localScale = new Vector3(scale, scale, 1);

                    sr.sortingOrder = 0;

                    count++;
                }
            }

            Debug.Log($"[MapGenerator] Generated {count} ground tiles ({gridWidth}x{gridHeight}) with cell size {cellSize}.");
        }
    }
}