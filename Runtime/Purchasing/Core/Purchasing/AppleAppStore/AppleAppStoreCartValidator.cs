namespace UnityEngine.Purchasing
{
    class AppleAppStoreCartValidator : StoreCartValidator
    {
        internal AppleAppStoreCartValidator(string storeName)
            : base(storeName, new SingleProductSingleQuantityCartValidator()) { }
    }
}
