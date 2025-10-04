using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Ouiki.SiliconeHeart.UI
{
    [ExecuteAlways]
    public class InfiniteScrollPanel : MonoBehaviour
    {
        #region Fields
        [Header("Scroll Area (Content)")]
        public RectTransform scrollArea;
        [Header("Scroll Settings")]
        public float scrollSpeed = 40f;
        private float scrollPosition = 0f;
        private List<GameObject> buildingButtonObjects = new();
        private HorizontalLayoutGroup layoutGroup;
        #endregion

        #region Unity Lifecycle

        void OnValidate()
        {
            Debug.Log("[InfiniteScrollPanel] OnValidate called");
            EnsureLayoutGroup();
        }

        void Update()
        {
            if (scrollArea == null)
            {
                Debug.LogWarning("[InfiniteScrollPanel] Update aborted: scrollArea is null");
                return;
            }

            if (Mouse.current != null && RectTransformUtility.RectangleContainsScreenPoint(scrollArea, Mouse.current.position.ReadValue()))
            {
                float wheel = Mouse.current.scroll.y.ReadValue();
                if (wheel != 0f)
                {
                    Debug.Log($"[InfiniteScrollPanel] Scrolling: wheel={wheel}, scrollSpeed={scrollSpeed}, Time.deltaTime={Time.deltaTime}");
                    scrollPosition += wheel * scrollSpeed * Time.deltaTime;
                    scrollPosition = Mathf.Clamp(scrollPosition, 0f, GetMaxScroll());
                    scrollArea.anchoredPosition = new Vector2(-scrollPosition, scrollArea.anchoredPosition.y);
                    Debug.Log($"[InfiniteScrollPanel] scrollPosition={scrollPosition}, anchoredPosition={scrollArea.anchoredPosition}");
                }
            }
        }
        #endregion

        #region Layout & Scroll
        public void EnsureLayoutGroup()
        {
            if (scrollArea == null)
            {
                Debug.LogWarning("[InfiniteScrollPanel] EnsureLayoutGroup aborted: scrollArea is not assigned!");
                return;
            }
            layoutGroup = scrollArea.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
            {
                Debug.Log("[InfiniteScrollPanel] Adding HorizontalLayoutGroup to scrollArea");
                layoutGroup = scrollArea.gameObject.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                layoutGroup.spacing = 8f;
            }
            else
            {
                Debug.Log("[InfiniteScrollPanel] HorizontalLayoutGroup found");
            }
        }

        float GetMaxScroll()
        {
            float contentWidth = scrollArea.rect.width;
            float viewWidth = ((RectTransform)scrollArea.parent).rect.width;
            float maxScroll = Mathf.Max(0f, contentWidth - viewWidth);
            Debug.Log($"[InfiniteScrollPanel] GetMaxScroll: contentWidth={contentWidth}, viewWidth={viewWidth}, maxScroll={maxScroll}");
            return maxScroll;
        }
        #endregion

        #region Button Population
        public void Populate(List<GameObject> buttons)
        {
            Debug.Log($"[InfiniteScrollPanel] Populate called with {buttons.Count} buttons");
            int childCount = scrollArea.childCount;
            Debug.Log($"[InfiniteScrollPanel] Clearing {childCount} old children from scrollArea");
            foreach (Transform child in scrollArea)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child.gameObject);
                    Debug.Log($"[InfiniteScrollPanel] DestroyImmediate child: {child.gameObject.name}");
                }
                else
                {
                    Destroy(child.gameObject);
                    Debug.Log($"[InfiniteScrollPanel] Destroy child: {child.gameObject.name}");
                }
#else
                Destroy(child.gameObject);
                Debug.Log($"[InfiniteScrollPanel] Destroy child: {child.gameObject.name}");
#endif
            }
            buildingButtonObjects.Clear();

            foreach (var obj in buttons)
            {
                obj.transform.SetParent(scrollArea, false);
                obj.SetActive(true);
                buildingButtonObjects.Add(obj);
                Debug.Log($"[InfiniteScrollPanel] Added button: {obj.name} to scrollArea");

                // Setup BuildingButton visuals (name, image)
                var buttonComp = obj.GetComponent<BuildingButton>();
                if (buttonComp != null && buttonComp.buildingData != null)
                {
                    // Already assigned in SetBuildingData
                    Debug.Log($"[InfiniteScrollPanel] Button {obj.name} visuals assigned in BuildingButton");
                }
                else
                {
                    Debug.LogWarning($"[InfiniteScrollPanel] Could not assign visuals for {obj.name} (missing BuildingButton or BuildingData)");
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollArea);
            Debug.Log("[InfiniteScrollPanel] Forced LayoutRebuilder.ForceRebuildLayoutImmediate");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            Debug.Log("[InfiniteScrollPanel] Editor UI repaint requested");
#endif

            scrollPosition = 0f;
            scrollArea.anchoredPosition = new Vector2(0f, scrollArea.anchoredPosition.y);
            Debug.Log("[InfiniteScrollPanel] Reset scrollPosition and scrollArea anchoredPosition");
        }
        #endregion
    }
}