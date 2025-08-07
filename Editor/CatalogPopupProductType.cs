using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// Represents the type of product in a catalog popup.
    /// </summary>
    public enum CatalogPopupProductType
    {
        /// <summary>
        /// Represents a product that can be consumed.
        /// </summary>
        Consumable = ProductType.Consumable,
        /// <summary>
        /// Represents a product that is not consumed.
        /// </summary>
        NonConsumable = ProductType.NonConsumable,
        /// <summary>
        /// Represents a product that is a subscription.
        /// </summary>
        Subscription = ProductType.Subscription
    }
}
