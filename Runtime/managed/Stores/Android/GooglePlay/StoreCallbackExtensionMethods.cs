using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    public static class StoreCallbackExtensionMethods
    {
        public static Product FindProductById(this IStoreCallback storeCallback, string sku)
        {
            if (sku != null && storeCallback.products != null)
            {
                return storeCallback.products.WithID(sku) ?? storeCallback.products.WithStoreSpecificID(sku);
            }
            return null;
        }
    }
}
