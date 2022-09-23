# [Apple Family Sharing](https://developer.apple.com/app-store/subscriptions/#family-sharing)

## Introduction

Apple allows auto-renewable subscriptions and non-consumable in-app purchases to be shared within a family.
In order to use this feature, Family Sharing must be enabled on a per purchasable basis. See [Turn on Family Sharing for in-app purchases](https://help.apple.com/app-store-connect/#/dev45b03fab9).

### Is Family Shareable

The family shareable status of a product is available through the `isFamilyShareable` field found in the Apple product metadata.
The metadata can be obtained from `ProductMetadata.GetAppleProductMetadata()` via `IStoreController.products`.
````
        bool IsProductFamilyShareable(Product product)
        {
            var appleProductMetadata = product.metadata.GetAppleProductMetadata();
            return appleProductMetadata?.isFamilyShareable ?? false;
        }
````

### Revoke Entitlement

In order to be handle revoked entitlements, you can specify a listener through the `IAppleConfiguration.SetEntitlementsRevokedListener(Action<List<Product>>`.
This will be called each time products have been revoked with the list of revoked products.
````
        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.Configure<IAppleConfiguration>().SetEntitlementsRevokedListener(EntitlementsRevokeListener);

            UnityPurchasing.Initialize(this, builder);
        }

        void EntitlementsRevokeListener(List<Product> revokedProducts)
        {
            foreach (var revokedProduct in revokedProducts)
            {
                Debug.Log($"Revoked product: {revokedProduct.definition.id}");
            }
        }
````
