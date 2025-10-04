using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Ouiki.SiliconeHeart.UI
{
    [ExecuteAlways]
    public class InfiniteScrollPanel : MonoBehaviour
    {
        [Header("Panel (Viewport)")]
        public RectTransform panelRect;
        [Header("Scroll Settings")]
        public float scrollSpeed = 250f;
        public float buttonWidth = 100f;
        public float buttonHeight = 100f;
        public float buttonSpacing = 8f;

        private List<RectTransform> buttonRects = new();
        private float scrollOffset = 0f;

        // Drag/Swipe tracking
        private bool isDragging = false;
        private Vector2 dragStartPos;
        private float dragStartOffset;
        private int activePointerId = -1;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }
        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        void Update()
        {
            if (panelRect == null || buttonRects.Count == 0)
                return;

#if UNITY_EDITOR
            ProcessMouseDrag();
#else
            ProcessPointerDrag();
#endif
            UpdateButtonPositions();
        }

        // Mouse drag support (for Editor/testing)
        void ProcessMouseDrag()
        {
            // Mouse over panel? Always allow drag if over panel area
            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            bool overPanel = RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos);

            if (Mouse.current.leftButton.wasPressedThisFrame && overPanel)
            {
                isDragging = true;
                dragStartPos = mousePos;
                dragStartOffset = scrollOffset;
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
            }

            if (isDragging)
            {
                Vector2 mouseDelta = mousePos - dragStartPos;
                scrollOffset = dragStartOffset + mouseDelta.x;
            }
            else
            {
                // Fallback: Mouse wheel scroll
                float wheel = Mouse.current != null ? Mouse.current.scroll.y.ReadValue() : 0;
                if (wheel != 0f && overPanel)
                {
                    scrollOffset += wheel * scrollSpeed * Time.deltaTime;
                }
            }
        }

        // Touch or Pointer drag support (runtime, device, and EnhancedTouch)
        void ProcessPointerDrag()
        {
            bool pointerActive = false;
            Vector2 pointerPos = Vector2.zero;
            bool pointerDown = false;
            bool pointerUp = false;

            // EnhancedTouch takes priority if available
            if (Touch.activeTouches.Count > 0)
            {
                foreach (var touch in Touch.activeTouches)
                {
                    if (activePointerId == -1 && touch.phase == UnityEngine.InputSystem.TouchPhase.Began &&
                        RectTransformUtility.RectangleContainsScreenPoint(panelRect, touch.screenPosition))
                    {
                        // Start drag
                        isDragging = true;
                        activePointerId = touch.touchId;
                        dragStartPos = touch.screenPosition;
                        dragStartOffset = scrollOffset;
                    }
                    if (activePointerId == touch.touchId && isDragging)
                    {
                        pointerActive = true;
                        pointerPos = touch.screenPosition;
                        pointerDown = touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary;
                        pointerUp = touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                    }
                }
            }
            else // Fallback to mouse or pointer input
            {
                var pointer = Pointer.current;
                if (pointer != null)
                {
                    pointerPos = pointer.position.ReadValue();
                    bool overPanel = RectTransformUtility.RectangleContainsScreenPoint(panelRect, pointerPos);

                    if (pointer.press.wasPressedThisFrame && overPanel)
                    {
                        isDragging = true;
                        activePointerId = 0;
                        dragStartPos = pointerPos;
                        dragStartOffset = scrollOffset;
                    }
                    if (isDragging && activePointerId == 0)
                    {
                        pointerActive = true;
                        pointerDown = pointer.press.isPressed;
                        pointerUp = pointer.press.wasReleasedThisFrame;
                    }
                }
            }

            if (pointerActive && pointerDown)
            {
                Vector2 delta = pointerPos - dragStartPos;
                scrollOffset = dragStartOffset + delta.x;
            }
            if (pointerUp)
            {
                isDragging = false;
                activePointerId = -1;
            }

            // Fallback: Mouse wheel scroll
            float wheel = Mouse.current != null ? Mouse.current.scroll.y.ReadValue() : 0;
            if (wheel != 0f && RectTransformUtility.RectangleContainsScreenPoint(panelRect, pointerPos))
            {
                scrollOffset += wheel * scrollSpeed * Time.deltaTime;
            }
        }

        void UpdateButtonPositions()
        {
            float totalWidth = buttonRects.Count * (buttonWidth + buttonSpacing);
            float panelWidth = panelRect.rect.width;
            float centerX = panelWidth * 0.5f;

            for (int i = 0; i < buttonRects.Count; i++)
            {
                RectTransform rt = buttonRects[i];
                float baseX = i * (buttonWidth + buttonSpacing);
                float x = baseX + scrollOffset;

                // Loop the button if it's far left or right
                x = x % totalWidth;
                if (x < 0) x += totalWidth;

                float anchoredX = x - totalWidth * 0.5f + centerX;
                rt.anchoredPosition = new Vector2(anchoredX, 0);
                rt.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.SetParent(panelRect, false);
                rt.gameObject.SetActive(true);
            }
        }

        public void Populate(List<GameObject> buttons)
        {
            // Remove old buttons
            foreach (Transform child in panelRect)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
            buttonRects.Clear();

            foreach (var obj in buttons)
            {
                var rt = obj.GetComponent<RectTransform>();
                if (rt == null)
                {
                    rt = obj.AddComponent<RectTransform>();
                }
                rt.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                obj.transform.SetParent(panelRect, false);
                buttonRects.Add(rt);
                obj.SetActive(true);
            }

            scrollOffset = 0f;
            UpdateButtonPositions();
        }
    }
}