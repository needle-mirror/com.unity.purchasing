using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The Fake Store dialog, built from uGUI components in code so it needs no prefab.
    /// </summary>
    internal class UIFakeStoreWindow : MonoBehaviour
    {
        const float k_MenuScreenRatio = 0.6f;
        const int k_SortingOrder = short.MaxValue;
        const int k_FontSize = 18;
        const float k_ButtonHeight = 40f;
        const float k_Spacing = 8f;
        const int k_Padding = 12;
        const float k_ScrollSensitivity = 30f; // ScrollRect defaults to 1, which barely moves per wheel notch.

        static readonly Vector2 k_ReferenceResolution = new Vector2(800, 600);
        static readonly Color k_BlockerColor = new Color(0f, 0f, 0f, 0.5f);
        static readonly Color k_WindowColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        static readonly Color k_ButtonColor = new Color(0.32f, 0.32f, 0.32f, 1f);
        static readonly Color k_TextColor = Color.white;

        string m_QueryText;
        string m_OkText;
        string m_CancelText;
        string m_LastSelectedOptionText;
        List<string> m_Options;

        Action m_OnOk;
        Action m_OnCancel;
        Action<int> m_OnDropdown;

        bool m_CancelEnabled;
        bool m_DropdownEnabled;
        bool m_DoDropdown;

        Font m_Font;
        Text m_Title;
        RectTransform m_Content;
        ScrollRect m_ScrollRect;

        void Awake()
        {
            BuildWindow();
        }

        void BuildWindow()
        {
            m_Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = k_SortingOrder;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = k_ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // Full-screen raycast target behind the window, so clicks outside it don't reach the UI underneath.
            var blocker = CreateUIObject("Blocker", transform);
            blocker.anchorMin = Vector2.zero;
            blocker.anchorMax = Vector2.one;
            blocker.offsetMin = Vector2.zero;
            blocker.offsetMax = Vector2.zero;
            blocker.gameObject.AddComponent<Image>().color = k_BlockerColor;

            // Centered window covering k_MenuScreenRatio of the screen in each direction.
            var margin = (1f - k_MenuScreenRatio) / 2f;
            var window = CreateUIObject("Window", transform);
            window.anchorMin = new Vector2(margin, margin);
            window.anchorMax = new Vector2(1f - margin, 1f - margin);
            window.offsetMin = Vector2.zero;
            window.offsetMax = Vector2.zero;
            window.gameObject.AddComponent<Image>().color = k_WindowColor;

            var windowLayout = window.gameObject.AddComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(k_Padding, k_Padding, k_Padding, k_Padding);
            windowLayout.spacing = k_Spacing;
            ConfigureLayoutGroup(windowLayout);

            m_Title = CreateText(window, string.Empty, FontStyle.Bold, TextAnchor.MiddleCenter);

            // Scrollable area holding the dialog body.
            var viewport = CreateUIObject("Viewport", window);
            viewport.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = Color.clear; // Receives drag events.

            m_Content = CreateUIObject("Content", viewport);
            m_Content.anchorMin = new Vector2(0f, 1f);
            m_Content.anchorMax = new Vector2(1f, 1f);
            m_Content.pivot = new Vector2(0.5f, 1f);
            m_Content.sizeDelta = Vector2.zero;

            var contentLayout = m_Content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = k_Spacing;
            ConfigureLayoutGroup(contentLayout);
            m_Content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            m_ScrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            m_ScrollRect.viewport = viewport;
            m_ScrollRect.content = m_Content;
            m_ScrollRect.horizontal = false;
            m_ScrollRect.movementType = ScrollRect.MovementType.Clamped;
            m_ScrollRect.scrollSensitivity = k_ScrollSensitivity;
        }

        void RebuildContent()
        {
            foreach (Transform child in m_Content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            if (m_DoDropdown)
            {
                BuildDropdownContent();
            }
            else
            {
                BuildMainContent();
            }

            m_ScrollRect.verticalNormalizedPosition = 1f;
        }

        void BuildMainContent()
        {
            m_Title.text = "Fake Store";

            CreateText(m_Content, m_QueryText, FontStyle.Normal, TextAnchor.UpperLeft);

            if (m_DropdownEnabled)
            {
                CreateButton(m_Content, m_LastSelectedOptionText, DoDropDown);
            }

            CreateButton(m_Content, m_OkText, OnOkClicked);

            if (m_CancelEnabled)
            {
                CreateButton(m_Content, m_CancelText, OnCancelClicked);
            }
        }

        void BuildDropdownContent()
        {
            m_Title.text = "Select Response";

            if (m_Options == null)
            {
                CreateText(m_Content, "Error! No Dropdown options set!", FontStyle.Normal, TextAnchor.UpperLeft);
                return;
            }

            for (var index = 0; index < m_Options.Count; index++)
            {
                var optionIndex = index;
                CreateButton(m_Content, m_Options[index], () => OnDropdown(optionIndex, m_Options[optionIndex]));
            }
        }

        static void ConfigureLayoutGroup(HorizontalOrVerticalLayoutGroup layout)
        {
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        Text CreateText(Transform parent, string text, FontStyle style, TextAnchor alignment)
        {
            var textComponent = CreateUIObject("Text", parent).gameObject.AddComponent<Text>();
            textComponent.font = m_Font;
            textComponent.fontSize = k_FontSize;
            textComponent.fontStyle = style;
            textComponent.color = k_TextColor;
            textComponent.alignment = alignment;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.raycastTarget = false;
            textComponent.text = text;
            return textComponent;
        }

        void CreateButton(Transform parent, string label, Action onClick)
        {
            var buttonTransform = CreateUIObject($"Button ({label})", parent);
            buttonTransform.gameObject.AddComponent<LayoutElement>().minHeight = k_ButtonHeight;

            var image = buttonTransform.gameObject.AddComponent<Image>();
            image.color = k_ButtonColor;

            var button = buttonTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick.Invoke);

            var labelTransform = (RectTransform)CreateText(buttonTransform, label, FontStyle.Normal, TextAnchor.MiddleCenter).transform;
            labelTransform.anchorMin = Vector2.zero;
            labelTransform.anchorMax = Vector2.one;
            labelTransform.offsetMin = Vector2.zero;
            labelTransform.offsetMax = Vector2.zero;
        }

        void DoDropDown()
        {
            m_DoDropdown = true;
            RebuildContent();
        }

        void OnOkClicked()
        {
            m_OnOk?.Invoke();
        }

        void OnCancelClicked()
        {
            m_OnCancel?.Invoke();
        }

        internal void ConfigureMainDialogText(string queryText, string okText, string cancelText)
        {
            m_QueryText = queryText;
            m_OkText = okText;
            m_CancelText = cancelText;
            RebuildContent();
        }

        internal void ConfigureDropdownOptions(List<string> options)
        {
            m_Options = new List<string>(options);

            if (options.Count > 0)
            {
                m_LastSelectedOptionText = options.First();
            }

            RebuildContent();
        }

        void OnDropdown(int index, string selectionText)
        {
            m_LastSelectedOptionText = selectionText;
            m_OnDropdown?.Invoke(index);
            m_DoDropdown = false;
            RebuildContent();
        }

        internal void AssignCallbacks(Action onOk, Action onCancel, Action<int> onDropdown)
        {
            m_OnOk = onOk;

            m_CancelEnabled = onCancel != null;
            m_OnCancel = onCancel;

            m_DropdownEnabled = onDropdown != null;
            m_OnDropdown = onDropdown;

            RebuildContent();
        }
    }
}
