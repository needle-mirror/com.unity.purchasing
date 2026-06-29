# IAP Catalog Item

An IAP Catalog Item asset defines the configuration of a catalog item in the IAP service.

An IAP Catalog Item has `.ucat` for extension.

## Creation

Right-click in the `Project Window` then select `Create > Services > IAP Catalog Item` to create an IAP Catalog Item asset.

Once created, a corresponding item will appear in the deployment window, allowing you to deploy the newly created catalog item.

## Edition

Any text editor can modify an IAP catalog item file, however, it is always preferred to choose an IDE that [supports JSON Schema definitions](https://json-schema.org/implementations#editors) to benefit from code completion, or to use directly the Inspector window.

## Deletion

Deleting an IAP Catalog Item asset isn't enough to remove the resource from the service, it is also needed to delete it using the Unity dashboard.

## Format and schema

An IAP Catalog Item asset is written in JSON, and its schema is `UnityRemoteCatalog` version `1.1.0` (`https://services.api.unity.com/schema-registry/v1/schemas/UnityRemoteCatalog/versions/1.1.0`).

Here is an example of an IAP Catalog Item asset content. `subtitle`, `badge`, `webshopPrice`, and `imageUrl` are optional.

```json
{
  "uSKU": "gem_pack_100",
  "type": "Consumable",
  "productDetails": [
    {
      "title": "100 Gems",
      "description": "A pack of 100 gems",
      "subtitle": "Best value",
      "language": "en-US",
      "badge": {
        "text": "Popular",
        "imageUrl": "https://example.com/badge.png"
      }
    }
  ],
  "pricing": [
    {
      "currencyCode": "USD",
      "amount": 4.99,
      "webshopPrice": 3.99
    }
  ],
  "imageUrl": "https://example.com/image.png",
  "storeIdOverrides": [
    { "store": "apple", "value": "com.example.apple.gem100" },
    { "store": "google", "value": "google_gem_100" }
  ]
}
```

## Validation rules

| Field | Rule |
|---|---|
| `uSKU` | Auto-derived from the file name when empty; can be overridden in the inspector. When set: 1-141 characters, must match `^[a-zA-Z0-9._-]+$` (letters, digits, `.`, `_`, `-` only). |
| `type` | Required. One of `Consumable`, `NonConsumable`, `Subscription`. |
| `productDetails` | At least one entry required. |
| `productDetails[*].title` | Required. 1-50 characters. |
| `productDetails[*].description` | Optional. When set: at most 250 characters. |
| `productDetails[*].subtitle` | Optional. When set: at most 50 characters. |
| `productDetails[*].badge` | Optional. Treated as unset when text is empty. |
| `productDetails[*].badge.imageUrl` | Optional URI. |
| `pricing` | At least one entry required. A `USD` entry is required. |
| `pricing[*].amount` | Required. Greater than 0. |
| `pricing[*].webshopPrice` | Optional. Price displayed on the webshop. |
| `storeIdOverrides` | Optional array of per-store ID mappings. Entries with an empty `value` are dropped before upsert. |
| `storeIdOverrides[*].store` | Required. One of `apple`, `google`. |
| `storeIdOverrides[*].value` | Required. Platform-specific product ID. |

When editing in the Inspector, the `Catalog Listing Id` field shows the file name (without extension) and is read-only — rename the asset to change it. The `Sku` field defaults to the file name but can be overridden.

The file name itself must satisfy the same constraints as `uSKU`: 1-141 characters, matching `^[a-zA-Z0-9._-]+$`. Files that violate this surface a validation error in the deployment window.
The catalog item ID (file name for `.ucat`) must satisfy the regex constraints above. Items that violate this surface a validation error in the deployment window. The file system path itself is not validated — only the catalog item ID on the model.

## CSV catalogs

In addition to per-item `.ucat` files, multiple catalog items can be authored together in a single `.catalog.csv` file. The CSV columns are:

| Column | Maps to | Required |
|---|---|---|
| `CatalogItemId` | `catalogItemId` | No (falls back to `Sku`) |
| `Sku` | `uSKU` | Yes |
| `Title` | `productDetails[*].title` | Yes (per language row) |
| `Description` | `productDetails[*].description` | No |
| `Subtitle` | `productDetails[*].subtitle` | No |
| `BadgeText` | `productDetails[*].badge.text` | No (empty = no badge) |
| `BadgeImageUrl` | `productDetails[*].badge.imageUrl` | No |
| `Language` | `productDetails[*].language` | No (defaults to `en_US`) |
| `ProductType` | `type` | No (defaults to `Consumable`) |
| `CurrencyCode` | `pricing[*].currencyCode` | No (skipped if missing) |
| `Amount` | `pricing[*].amount` | No (skipped if missing) |
| `WebshopPrice` | `pricing[*].webshopPrice` | No |
| `ImageUrl` | `imageUrl` | No |

Rows for the same `CatalogItemId` are grouped — repeat rows to add multiple languages or currencies. Missing optional columns are tolerated. The parser surfaces warnings on the parent CSV deployment item for: unknown `ProductType`, unknown `Language`, rows missing `Sku`, and duplicate `(CatalogItemId, Language)` / `(CatalogItemId, CurrencyCode)` rows (first wins).
