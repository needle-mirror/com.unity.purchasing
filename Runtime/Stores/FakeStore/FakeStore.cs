using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The various kinds of Fake Store UI presentations.
    /// Requires UIFakeStore variant of FakeStore to function.
    /// </summary>
    public enum FakeStoreUIMode
    {
        /// <summary>
        /// FakeStore by default displays no dialogs.
        /// </summary>
        Default,

        /// <summary>
        /// Simple dialog is shown when Purchasing.
        /// </summary>
        StandardUser,

        /// <summary>
        /// Dialogs with failure reason code selection when
        /// Initializing/Retrieving Products and when Purchasing.
        /// </summary>
        DeveloperUser
    }

    internal class FakeStore : JsonStore, INativeStore
    {
        protected enum DialogType
        {
            Purchase,
            RetrieveProducts,
        }

        public const string Name = "fake";
        readonly List<ConfirmedOrder> m_ConfirmedOrders = new List<ConfirmedOrder>();
        public string unavailableProductId { get; set; }
        public FakeStoreUIMode UIMode = FakeStoreUIMode.Default; // Requires UIFakeStore
        public bool purchaseCalled;
        public bool restoreCalled;


        public FakeStore(ICartValidator cartValidator, ILogger logger) : base(cartValidator, logger, FakeAppStore.Name)
        {
            SetNativeStore(this);
        }

        // Hard override of JSONStore. Don't call base, or you'll get an infinite recursive loop/stack overflow
        public override void Connect()
        {
            OnStoreConnectionSucceeded();
        }

        // INativeStore
        public void RetrieveProducts(string json)
        {
            var jsonList = (List<object>)MiniJson.JsonDecode(json);
            var productDefinitions = jsonList.DecodeJSON(FakeAppStore.Name);
            StoreRetrieveProducts(new ReadOnlyCollection<ProductDefinition>(productDefinitions.ToList()));
        }

        // This is now being used by the INativeStore implementation
        public void StoreRetrieveProducts(ReadOnlyCollection<ProductDefinition> productDefinitions)
        {
            var products = new List<ProductDescription>();
            foreach (var product in productDefinitions)
            {
                if (unavailableProductId != product.id)
                {
                    products.Add(new ProductDescription(product.storeSpecificId, GetOrCreateProductMetadata(product.id)));
                }
            }

            void handleAllowInitializeOrRetrieveProducts(bool allow, ProductFetchFailureDescription failureReason)
            {
                if (allow)
                {
                    ProductsCallback?.OnProductsRetrieved(products);
                }
                else
                {
                    ProductsCallback?.OnProductsRetrieveFailed(failureReason);
                }
            }

            // To mimic typical store behavior, only display RetrieveProducts dialog for developers
            if (!(UIMode == FakeStoreUIMode.DeveloperUser &&
                StartUI<ProductFetchFailureDescription>(productDefinitions, DialogType.RetrieveProducts, handleAllowInitializeOrRetrieveProducts)))
            {
                ProductsCallback?.OnProductsRetrieved(products);
            }
        }

        ProductMetadata GetOrCreateProductMetadata(string productId)
        {
            var metadata = new ProductMetadata("$0.01", "Fake title for " + productId, "Fake description", "USD", 0.01m); ;
            var catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog != null)
            {
                foreach (var item in catalog.allProducts)
                {
                    if (item.id == productId)
                    {
                        metadata = new ProductMetadata(item.googlePrice.value.ToString(), item.defaultDescription.Title, item.defaultDescription.Description, "USD", item.googlePrice.value);
                    }
                }
            }

            return metadata;
        }

        //INativeStore
        public void FetchExistingPurchases()
        {
            PurchaseFetchCallback?.OnAllPurchasesRetrieved(m_ConfirmedOrders);
        }

        // INativeStore
        public void Purchase(string productJSON, string developerPayload)
        {
            FakePurchase(ParseProductDefinition(productJSON), developerPayload);
        }

        ProductDefinition ParseProductDefinition(string productJSON)
        {
            var dictionary = (Dictionary<string, object>)MiniJson.JsonDecode(productJSON);

            string id, storeId;

            dictionary.TryGetValue("id", out var obj);
            id = obj.ToString();

            dictionary.TryGetValue("storeSpecificId", out obj);
            storeId = obj.ToString();

            // This doesn't currently deal with "enabled" and "payouts" that could be included in the JSON
            return new ProductDefinition(id, storeId, ParseProductType(dictionary));
        }

        ProductType ParseProductType(Dictionary<string, object> dictionary)
        {
            dictionary.TryGetValue("type", out var obj);

            var type = obj.ToString();

            var itemType = Enum.IsDefined(typeof(ProductType), type) ? (ProductType)Enum.Parse(typeof(ProductType), type) : ProductType.Consumable;
            return itemType;
        }

        void FakePurchase(ProductDefinition productDefinition, string developerPayload)
        {
            // Our billing systems should only keep track of non consumables.
            if (productDefinition.type != ProductType.Consumable)
            {
                var order = new ConfirmedOrder(new Cart(new Product(productDefinition, GetOrCreateProductMetadata(productDefinition.id))), new OrderInfo("", "", ""));
                m_ConfirmedOrders.Add(order);
            }

            void handleAllowPurchase(bool allow, PurchaseFailureReason failureReason)
            {
                if (allow)
                {
                    base.OnPurchaseSucceeded(productDefinition.storeSpecificId, "ThisIsFakeReceiptData", Guid.NewGuid().ToString());
                }
                else
                {
                    if (failureReason == (PurchaseFailureReason)Enum.Parse(typeof(PurchaseFailureReason), "Unknown"))
                    {
                        failureReason = PurchaseFailureReason.UserCancelled;
                    }

                    var product = ProductCache.FindOrDefault(productDefinition.storeSpecificId);
                    var failureDescription =
                        new PurchaseFailureDescription(product, failureReason, "failed a fake store purchase");

                    OnPurchaseFailed(failureDescription);
                }
            }

            if (!StartUI<PurchaseFailureReason>(productDefinition, DialogType.Purchase, handleAllowPurchase))
            {
                // Default non-UI FakeStore purchase behavior is to succeed
                handleAllowPurchase(true, (PurchaseFailureReason)Enum.Parse(typeof(PurchaseFailureReason), "Unknown"));
            }
        }

        public bool CheckEntitlement(string productJSON)
        {
            // TODO: IAP-3242 Add more purchase flows to Fake Store, including deferred purchases and subscriptions.
            var definition = ParseProductDefinition(productJSON);

            var entitled = CheckIfProductEntitled(definition);

            var entitlementStatus = entitled ? EntitlementStatus.FullyEntitled : EntitlementStatus.NotEntitled;
            EntitlementCallback?.OnCheckEntitlementSucceeded(definition, entitlementStatus);

            return entitled;
        }

        bool CheckIfProductEntitled(ProductDefinition definition)
        {
            var purchasedProducts = m_ConfirmedOrders.SelectMany(order => order.CartOrdered.Items()).Select(item => item.Product);
            return purchasedProducts.Any(product => product.definition.id == definition.id);
        }

        public void RestoreTransactions(Action<bool, string> callback)
        {
            foreach (var confirmedOrder in m_ConfirmedOrders)
            {
                var order = new PendingOrder(confirmedOrder.CartOrdered, confirmedOrder.Info);
                PurchaseCallback?.OnPurchaseSucceeded(order);
            }

            callback?.Invoke(true, null);
        }


        // INativeStore
        public void FinishTransaction(string productJSON, string transactionID)
        {
            // we need this for INativeStore but won't be using
        }

        /// <summary>
        /// Implemented by UIFakeStore derived class
        /// </summary>
        /// <returns><c>true</c>, if UI was started, <c>false</c> otherwise.</returns>
        protected virtual bool StartUI<T>(object model, DialogType dialogType, Action<bool, T> callback)
        {
            return false;
        }
    }
}
