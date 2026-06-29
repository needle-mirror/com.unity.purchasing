using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Authoring
{
    [CustomPropertyDrawer(typeof(CatalogProductsList))]
    sealed class CatalogProductsTableDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var items = property.FindPropertyRelative("Items");
            var count = items.arraySize;
            var foldout = new Foldout { text = $"Products ({count})", value = false };
            foldout.AddToClassList("products-foldout");

            if (count == 0)
            {
                var emptyLabel = new Label("No products found.");
                emptyLabel.AddToClassList("products-empty");
                foldout.Add(emptyLabel);
                return foldout;
            }

            var headerRow = new VisualElement();
            headerRow.AddToClassList("product-header-row");

            var skuHeader = new Label("SKU");
            skuHeader.AddToClassList("product-header-label");
            skuHeader.AddToClassList("product-sku");
            var priceHeader = new Label("Type / Price");
            priceHeader.AddToClassList("product-header-label");
            headerRow.Add(skuHeader);
            headerRow.Add(priceHeader);
            foldout.Add(headerRow);

            for (var i = 0; i < count; i++)
            {
                var element = items.GetArrayElementAtIndex(i);
                var sku = element.FindPropertyRelative("Sku").stringValue;
                var typeAndPrice = element.FindPropertyRelative("TypeAndPrice").stringValue;

                var row = new VisualElement();
                row.AddToClassList("product-row");

                var skuLabel = new Label(sku);
                skuLabel.AddToClassList("product-sku");

                var priceLabel = new Label(typeAndPrice);
                priceLabel.AddToClassList("product-price");

                row.Add(skuLabel);
                row.Add(priceLabel);
                foldout.Add(row);
            }

            return foldout;
        }
    }
}
