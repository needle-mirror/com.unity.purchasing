using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Store;
using Windows.Foundation;

namespace UnityEngine.Purchasing.Default
{
    public class CurrentApp : ICurrentApp
    {

        public IAsyncOperation<IReadOnlyList<UnfulfilledConsumable>> GetUnfulfilledConsumablesAsync()
        {
            return global::Windows.ApplicationModel.Store.CurrentApp.GetUnfulfilledConsumablesAsync();
        }

        public IAsyncOperation<ListingInformation> LoadListingInformationAsync()
        {
            return global::Windows.ApplicationModel.Store.CurrentApp.LoadListingInformationAsync();
        }

        public IAsyncOperation<FulfillmentResult> ReportConsumableFulfillmentAsync(string productId, Guid transactionId)
        {
            return global::Windows.ApplicationModel.Store.CurrentApp.ReportConsumableFulfillmentAsync(productId, transactionId);
        }

        public IAsyncOperation<PurchaseResults> RequestProductPurchaseAsync(string productId)
        {
            return global::Windows.ApplicationModel.Store.CurrentApp.RequestProductPurchaseAsync(productId);
        }

        public IAsyncOperation<string> RequestProductReceiptAsync(string productId) {
            return global::Windows.ApplicationModel.Store.CurrentApp.GetProductReceiptAsync(productId);
        }

        public LicenseInformation LicenseInformation
        {
            get
            {
                return global::Windows.ApplicationModel.Store.CurrentApp.LicenseInformation;
            }
        }

        public IAsyncOperation<string> RequestAppReceiptAsync() {
            return global::Windows.ApplicationModel.Store.CurrentApp.GetAppReceiptAsync();
        }

        public void BuildMockProducts(List<WinProductDescription> products) {
            // Only implemented by the mock store.
        }
    }
}
