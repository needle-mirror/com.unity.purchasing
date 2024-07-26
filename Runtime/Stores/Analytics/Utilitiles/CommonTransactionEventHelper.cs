namespace UnityEngine.Purchasing
{
    class CommonTransactionEventHelper
    {
        internal static string GetTransactionName(Product product)
        {
            return string.IsNullOrEmpty(product.metadata.localizedTitle) ?
                product.definition.storeSpecificId :
                product.metadata.localizedTitle;
        }

    }
}
