namespace UnityEngine.Purchasing
{
    interface IGooglePlayStoreChangeSubscriptionService
    {
        void ChangeSubscription(ProductDefinition product, Order currentOrder, GooglePlayReplacementMode? desiredReplacementMode);
    }
}
