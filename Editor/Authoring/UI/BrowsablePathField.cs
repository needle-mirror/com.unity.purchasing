using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.UI
{
    class BrowsablePathField : VisualElement
    {
        readonly TextField m_TextField;

        public event Action<string> ValueChanged;

        public string Value
        {
            get => m_TextField.value;
            set => m_TextField.value = value;
        }

        public float LabelMinWidth
        {
            set => m_TextField.labelElement.style.minWidth = value;
        }

        public BrowsablePathField(string label = null)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.flexGrow = 1;

            m_TextField = new TextField(label) { value = string.Empty };
            m_TextField.style.flexGrow = 1;
            m_TextField.labelElement.style.minWidth = 0;
            m_TextField.labelElement.style.width = StyleKeyword.Auto;
            m_TextField.labelElement.style.marginRight = 4;
            Add(m_TextField);

            var browseButton = new Button(OnBrowseClicked);
            browseButton.style.width = 24;
            browseButton.style.height = 20;
            browseButton.style.marginLeft = 2;
            browseButton.style.paddingTop = 0;
            browseButton.style.paddingBottom = 0;
            browseButton.style.paddingLeft = 0;
            browseButton.style.paddingRight = 0;
            browseButton.style.flexShrink = 0;
            browseButton.style.justifyContent = Justify.Center;
            browseButton.style.alignItems = Align.Center;

#if UNITY_2023_2_OR_NEWER
            browseButton.iconImage = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
#else
            browseButton.Add(new Image { image = EditorGUIUtility.IconContent("Folder Icon").image });
#endif

            Add(browseButton);

            m_TextField.RegisterValueChangedCallback(evt => ValueChanged?.Invoke(evt.newValue));
        }

        void OnBrowseClicked()
        {
            var currentFolder = string.IsNullOrWhiteSpace(Value) ? "Assets" : Value;
            var picked = EditorUtility.OpenFolderPanel("Select Folder", currentFolder, "");
            if (string.IsNullOrEmpty(picked))
            {
                return;
            }

            var dataPath = Application.dataPath;
            if (picked.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                picked = "Assets" + picked.Substring(dataPath.Length);
            }

            Value = picked;
        }
    }
}
