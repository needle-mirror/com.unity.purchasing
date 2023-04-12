using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Purchasing;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// Customer Editor class for the IAPButtons. This class handle how the IAPButtons should represent itself in the UnityEditor.
    /// </summary>
    public abstract class AbstractIAPButtonEditor : Editor
    {
        private static readonly string[] excludedFields = new string[] { "m_Script", "onTransactionsRestored" };
        private static readonly string[] restoreButtonExcludedFields = new string[] { "m_Script", "consumePurchase", "onPurchaseComplete", "onPurchaseFailed", "titleText", "descriptionText", "priceText" };
        private const string kNoProduct = "<None>";

        private readonly List<string> m_ValidIDs = new List<string>();
        private SerializedProperty m_ProductIDProperty;

        /// <summary>
        /// Event trigger when IAPButton is enabled in the scene.
        /// </summary>
        protected void OnEnableInternal()
        {
            m_ProductIDProperty = serializedObject.FindProperty("productId");
        }

        /// <summary>
        /// Event trigger when trying to draw the BaseIAPButton in the inspector.
        /// </summary>
        protected void OnInspectorGuiInternal()
        {
            var isAPurchaseButton = ((BaseIAPButton)target).IsAPurchaseButton();
            var productId = ((BaseIAPButton)target).GetProductId();
            DrawProductIdDropDown(isAPurchaseButton, productId);
        }

        void DrawProductIdDropDown(bool isAPurchaseButton, string productId)
        {
            serializedObject.Update();

            DrawProductIdDropdownWhenButtonIsPurchaseType(isAPurchaseButton, productId);

            DrawPropertiesExcluding(serializedObject, isAPurchaseButton ? excludedFields : restoreButtonExcludedFields);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawProductIdDropdownWhenButtonIsPurchaseType(bool isAPurchaseButton, string productId)
        {
            if (isAPurchaseButton)
            {
                EditorGUILayout.LabelField(new GUIContent("Product ID:", "Select a product from the IAP catalog."));
                LoadProductIdsFromCodelessCatalog();
                m_ProductIDProperty.stringValue = GetCurrentlySelectedProduct(productId);

                if (GUILayout.Button("IAP Catalog..."))
                {
                    ProductCatalogEditor.ShowWindow();
                }
            }
        }

        void LoadProductIdsFromCodelessCatalog()
        {
            var catalog = ProductCatalog.LoadDefaultCatalog();

            m_ValidIDs.Clear();
            m_ValidIDs.Add(kNoProduct);
            foreach (var product in catalog.allProducts)
            {
                m_ValidIDs.Add(product.id);
            }
        }

        string GetCurrentlySelectedProduct(string productId)
        {
            var currentIndex = string.IsNullOrEmpty(productId) ? 0 : m_ValidIDs.IndexOf(productId);
            var newIndex = EditorGUILayout.Popup(currentIndex, m_ValidIDs.ToArray());
            return newIndex > 0 && newIndex < m_ValidIDs.Count ? m_ValidIDs[newIndex] : string.Empty;
        }
    }
}
