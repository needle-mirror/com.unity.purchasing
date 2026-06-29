using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.IO
{
    class CatalogCsvParser : ICatalogCsvParser
    {
        public const string ParseStateType = "CatalogCsvParseIssue";

        const string k_ColumnCatalogListingId = "CatalogListingId";
        const string k_ColumnSku = "Sku";
        const string k_ColumnTitle = "Title";
        const string k_ColumnDescription = "Description";
        const string k_ColumnSubtitle = "Subtitle";
        const string k_ColumnBadgeText = "BadgeText";
        const string k_ColumnBadgeImageUrl = "BadgeImageUrl";
        const string k_ColumnLanguage = "Language";
        const string k_ColumnProductType = "ProductType";
        const string k_ColumnCurrencyCode = "CurrencyCode";
        const string k_ColumnAmount = "Amount";
        const string k_ColumnWebshopPrice = "WebshopPrice";
        const string k_ColumnImageUrl = "ImageUrl";
        const string k_ColumnGoogleOverride = "GoogleOverride";
        const string k_ColumnAppleOverride = "AppleOverride";
        const string k_ColumnXboxStoreOverride = "XboxStoreOverride";
        const string k_ColumnMacAppStoreOverride = "MacAppStoreOverride";
        const string k_ColumnIsWebshopAvailable = "IsWebshopAvailable";
        // Per-row (one entry per row, aggregated by CatalogListingId on parse).
        const string k_ColumnCategory = "Category";
        const string k_ColumnHdImageUrl = "HdImageUrl";
        const string k_ColumnHdImageAltText = "HdImageAltText";
        // Per-item (set on first row, conflict-checked on subsequent rows).
        const string k_ColumnPromotionType = "PromotionType";
        const string k_ColumnPromotionStartsAt = "PromotionStartsAt";
        const string k_ColumnPromotionEndsAt = "PromotionEndsAt";

        public List<CatalogItem> Parse(string csvContent, out List<AssetState> issues)
        {
            issues = new List<AssetState>();

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                return new List<CatalogItem>();
            }

            var lines = ParseLines(csvContent);
            if (lines.Count < 2)
            {
                return new List<CatalogItem>();
            }

            var header = lines[0];
            var columnMap = BuildColumnMap(header);

            var idOrder = new List<string>();
            var groups = new Dictionary<string, CatalogItem>(StringComparer.OrdinalIgnoreCase);
            var conflictMessages = new List<string>();
            var duplicateMessages = new List<string>();
            var firstRowFor = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 1; i < lines.Count; i++)
            {
                var rowNumber = i + 1;
                var fields = lines[i];
                if (fields.Length == 0 || (fields.Length == 1 && string.IsNullOrWhiteSpace(fields[0])))
                {
                    continue;
                }

                var sku = GetField(fields, columnMap, k_ColumnSku);
                if (string.IsNullOrWhiteSpace(sku))
                {
                    issues.Add(new AssetState(
                        $"Row {rowNumber} skipped: missing Sku",
                        "Each data row must have a non-empty Sku.",
                        SeverityLevel.Warning, ParseStateType));
                    continue;
                }

                var catalogListingIdField = GetField(fields, columnMap, k_ColumnCatalogListingId);
                var catalogListingId = string.IsNullOrWhiteSpace(catalogListingIdField)
                    ? CatalogItem.CatalogListingIdPrefix + sku
                    : catalogListingIdField;

                var isFirstRow = !groups.TryGetValue(catalogListingId, out var item);
                if (isFirstRow)
                {
                    item = new CatalogItem
                    {
                        CatalogListingId = catalogListingId,
                        uSku = sku,
                        ProductType = ParseProductType(GetField(fields, columnMap, k_ColumnProductType), rowNumber, issues),
                        ImageUrl = GetField(fields, columnMap, k_ColumnImageUrl),
                        ProductDetails = new List<ProductDetails>(),
                        PricingDetails = new List<PricingDetails>(),
                        StoreIdOverrides = BuildStoreIdOverrides(fields, columnMap),
                        IsWebshopAvailable = ParseWebshopAvailability(GetField(fields, columnMap, k_ColumnIsWebshopAvailable)),
                        Categories = new List<string>(),
                        HdImages = new List<HdImage>(),
                        Promotion = ParsePromotion(
                            GetField(fields, columnMap, k_ColumnPromotionType),
                            GetField(fields, columnMap, k_ColumnPromotionStartsAt),
                            GetField(fields, columnMap, k_ColumnPromotionEndsAt),
                            rowNumber, issues),
                    };
                    groups[catalogListingId] = item;
                    idOrder.Add(catalogListingId);
                    firstRowFor[catalogListingId] = rowNumber;
                }
                else
                {
                    var imageUrl = NullIfEmpty(GetField(fields, columnMap, k_ColumnImageUrl));
                    AddConflictIfChanged(imageUrl, item.ImageUrl,
                        nameof(CatalogItem.ImageUrl), catalogListingId,
                        rowNumber, firstRowFor[catalogListingId], conflictMessages);

                    var productTypeField = GetField(fields, columnMap, k_ColumnProductType);
                    var parsedType = !string.IsNullOrWhiteSpace(productTypeField)
                        ? ParseProductType(productTypeField, rowNumber, issues)
                        : (ProductType?)null;
                    AddConflictIfChanged(parsedType, item.ProductType,
                        nameof(CatalogItem.ProductType), catalogListingId,
                        rowNumber, firstRowFor[catalogListingId], conflictMessages);

                    var google = NullIfEmpty(GetField(fields, columnMap, k_ColumnGoogleOverride));
                    AddConflictIfChanged(google, FindOverrideValue(item, StoreId.Google),
                        k_ColumnGoogleOverride, catalogListingId,
                        rowNumber, firstRowFor[catalogListingId], conflictMessages);

                    var apple = NullIfEmpty(GetField(fields, columnMap, k_ColumnAppleOverride));
                    AddConflictIfChanged(apple, FindOverrideValue(item, StoreId.Apple),
                        k_ColumnAppleOverride, catalogListingId,
                        rowNumber, firstRowFor[catalogListingId], conflictMessages);

                    var xbox = NullIfEmpty(GetField(fields, columnMap, k_ColumnXboxStoreOverride));
                    AddConflictIfChanged(xbox, FindOverrideValue(item, StoreId.XboxStore),
                        k_ColumnXboxStoreOverride, catalogListingId,
                        rowNumber, firstRowFor[catalogListingId], conflictMessages);

                    var macos = NullIfEmpty(GetField(fields, columnMap, k_ColumnMacAppStoreOverride));
                    AddConflictIfChanged(macos, FindOverrideValue(item, StoreId.MacAppStore),
                        k_ColumnMacAppStoreOverride, catalogListingId,
                        rowNumber, firstRowFor[catalogListingId], conflictMessages);

                    CheckWebshopConflicts(item, fields, columnMap, catalogListingId,
                        rowNumber, firstRowFor[catalogListingId], conflictMessages);
                }

                var title = GetField(fields, columnMap, k_ColumnTitle);
                var description = GetField(fields, columnMap, k_ColumnDescription);
                var language = ParseLocale(GetField(fields, columnMap, k_ColumnLanguage), rowNumber, issues);

                if (!string.IsNullOrWhiteSpace(title))
                {
                    var subtitle = NullIfEmpty(GetField(fields, columnMap, k_ColumnSubtitle));
                    var badge = BuildBadge(
                        GetField(fields, columnMap, k_ColumnBadgeText),
                        GetField(fields, columnMap, k_ColumnBadgeImageUrl));

                    Predicate<ProductDetails> matchesProductValues = d =>
                        d.Title == title
                        && d.Description == (description ?? string.Empty)
                        && d.Subtitle == subtitle
                        && AreBadgesEqual(d.Badge, badge);

                    var existingDetail = item.ProductDetails.Find(d => d.Language == language);
                    var detailKey = $"{catalogListingId}|{language}";
                    if (existingDetail == null)
                    {
                        firstRowFor[detailKey] = rowNumber;
                        item.ProductDetails.Add(new ProductDetails
                        {
                            Title = title,
                            Description = description ?? string.Empty,
                            Language = language,
                            Subtitle = subtitle,
                            Badge = badge,
                        });
                    }
                    else
                    {
                        CheckDuplicate(matchesProductValues(existingDetail),
                            nameof(ProductDetails), $"{catalogListingId}, {language}",
                            rowNumber, firstRowFor[detailKey],
                            duplicateMessages, conflictMessages);
                    }
                }

                var currencyCode = GetField(fields, columnMap, k_ColumnCurrencyCode);
                var amountStr = GetField(fields, columnMap, k_ColumnAmount);

                if (!string.IsNullOrWhiteSpace(currencyCode)
                    && double.TryParse(amountStr, NumberStyles.Float | NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture, out var amount))
                {
                    var webshopPriceStr = GetField(fields, columnMap, k_ColumnWebshopPrice);
                    double webshopPrice = 0;
                    if (!string.IsNullOrWhiteSpace(webshopPriceStr))
                        double.TryParse(webshopPriceStr, NumberStyles.Float | NumberStyles.AllowThousands,
                            CultureInfo.InvariantCulture, out webshopPrice);

                    Predicate<PricingDetails> matchesPricingValues = p =>
                        p.Amount == amount && p.WebshopPrice == webshopPrice;

                    var existingPricing = item.PricingDetails.Find(p =>
                        string.Equals(p.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));
                    var pricingKey = $"{catalogListingId}|{currencyCode}";
                    if (existingPricing == null)
                    {
                        firstRowFor[pricingKey] = rowNumber;
                        item.PricingDetails.Add(new PricingDetails
                        {
                            CurrencyCode = currencyCode,
                            Amount = amount,
                            WebshopPrice = webshopPrice,
                        });
                    }
                    else
                    {
                        CheckDuplicate(matchesPricingValues(existingPricing),
                            nameof(PricingDetails), $"{catalogListingId}, {currencyCode}",
                            rowNumber, firstRowFor[pricingKey],
                            duplicateMessages, conflictMessages);
                    }
                }

                AppendWebshopRowEntries(item, fields, columnMap);
            }

            foreach (var id in idOrder)
            {
                NullOutEmptyWebshopCollections(groups[id]);
            }

            if (conflictMessages.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var detail in conflictMessages)
                {
                    sb.Append("- ").AppendLine(detail);
                }
                issues.Add(new AssetState(
                    "Row Conflicts (first occurrence kept)",
                    sb.ToString(),
                    SeverityLevel.Warning, ParseStateType));
            }

            if (duplicateMessages.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var detail in duplicateMessages)
                {
                    sb.Append("- ").AppendLine(detail);
                }
                issues.Add(new AssetState(
                    "Duplicate Rows (safely ignored)",
                    sb.ToString(),
                    SeverityLevel.Info, ParseStateType));
            }

            var result = new List<CatalogItem>(idOrder.Count);
            foreach (var id in idOrder)
            {
                result.Add(groups[id]);
            }

            return result;
        }

        static List<StoreIdOverride> BuildStoreIdOverrides(string[] fields, Dictionary<string, int> columnMap)
        {
            var google = NullIfEmpty(GetField(fields, columnMap, k_ColumnGoogleOverride));
            var apple = NullIfEmpty(GetField(fields, columnMap, k_ColumnAppleOverride));
            var xbox = NullIfEmpty(GetField(fields, columnMap, k_ColumnXboxStoreOverride));
            var macos = NullIfEmpty(GetField(fields, columnMap, k_ColumnMacAppStoreOverride));
            if (google == null && apple == null && xbox == null && macos == null)
                return null;

            var list = new List<StoreIdOverride>();
            if (google != null)
                list.Add(new StoreIdOverride { Store = StoreId.Google, Value = google });
            if (apple != null)
                list.Add(new StoreIdOverride { Store = StoreId.Apple, Value = apple });
            if (xbox != null)
                list.Add(new StoreIdOverride { Store = StoreId.XboxStore, Value = xbox });
            if (macos != null)
                list.Add(new StoreIdOverride { Store = StoreId.MacAppStore, Value = macos });
            return list;
        }

        static bool ParseWebshopAvailability(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && bool.TryParse(value, out var result)
                && result;
        }

        static Promotion ParsePromotion(string typeField, string startsAt, string endsAt,
            int rowNumber, List<AssetState> issues)
        {
            if (string.IsNullOrWhiteSpace(typeField))
                return null;
            if (!Enum.TryParse<PromotionType>(typeField, true, out var type))
            {
                issues.Add(new AssetState(
                    $"Row {rowNumber}: unknown {k_ColumnPromotionType} '{typeField}'",
                    $"Expected one of: {string.Join(", ", Enum.GetNames(typeof(PromotionType)))}.",
                    SeverityLevel.Warning, ParseStateType));
                return null;
            }
            return new Promotion
            {
                Type = type,
                StartsAt = ParseDateTimeOffset(startsAt),
                EndsAt = ParseDateTimeOffset(endsAt),
            };
        }

        static DateTimeOffset? ParseDateTimeOffset(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var result))
                return result;
            return null;
        }

        // Per-row aggregation for webshop multi-entry columns (Category and HdImageUrl/HdImageAltText).
        // Same shape as ProductDetails / PricingDetails: one entry per row, dedup by natural key
        // (string value for Category, Url for HdImage).
        static void AppendWebshopRowEntries(CatalogItem item, string[] fields, Dictionary<string, int> columnMap)
        {
            var category = NullIfEmpty(GetField(fields, columnMap, k_ColumnCategory));
            if (category is not null && !item.Categories.Contains(category))
                item.Categories.Add(category);

            var hdImageUrl = NullIfEmpty(GetField(fields, columnMap, k_ColumnHdImageUrl));
            if (hdImageUrl is not null && !item.HdImages.Exists(h => h.Url == hdImageUrl))
            {
                item.HdImages.Add(new HdImage
                {
                    Url = hdImageUrl,
                    AltText = NullIfEmpty(GetField(fields, columnMap, k_ColumnHdImageAltText)),
                });
            }
        }

        // Per-row collections are eagerly initialized in the first-row branch so AppendWebshopRowEntries
        // doesn't need a null check. Items that never see a webshop entry should still expose null
        // (matches the StoreIdOverrides convention — null means "not set", empty list means "explicit").
        static void NullOutEmptyWebshopCollections(CatalogItem item)
        {
            if (item.Categories is { Count: 0 })
                item.Categories = null;
            if (item.HdImages is { Count: 0 })
                item.HdImages = null;
        }

        static void CheckWebshopConflicts(CatalogItem item, string[] fields, Dictionary<string, int> columnMap,
            string catalogListingId, int rowNumber, int firstRow, List<string> conflictMessages)
        {
            var availability = GetField(fields, columnMap, k_ColumnIsWebshopAvailable);
            if (!string.IsNullOrWhiteSpace(availability)
                && ParseWebshopAvailability(availability) != item.IsWebshopAvailable)
            {
                AddMessage(conflictMessages, rowNumber, catalogListingId,
                    k_ColumnIsWebshopAvailable, "conflicts with", firstRow);
            }

            AddConflictIfChanged(
                NullIfEmpty(GetField(fields, columnMap, k_ColumnPromotionType)),
                item.Promotion?.Type.ToString(),
                k_ColumnPromotionType, catalogListingId, rowNumber, firstRow, conflictMessages);

            AddDateTimeOffsetConflictIfChanged(
                GetField(fields, columnMap, k_ColumnPromotionStartsAt),
                item.Promotion?.StartsAt,
                k_ColumnPromotionStartsAt, catalogListingId, rowNumber, firstRow, conflictMessages);

            AddDateTimeOffsetConflictIfChanged(
                GetField(fields, columnMap, k_ColumnPromotionEndsAt),
                item.Promotion?.EndsAt,
                k_ColumnPromotionEndsAt, catalogListingId, rowNumber, firstRow, conflictMessages);
        }

        // Compare DateTimeOffsets as parsed values, not strings — the round-trip format of
        // ToString("o") doesn't match common CSV inputs like "2026-01-01T00:00:00Z" (it expands
        // to ".0000000+00:00"), which would otherwise false-positive-conflict on repeat rows.
        static void AddDateTimeOffsetConflictIfChanged(string newValue, DateTimeOffset? existing,
            string fieldName, string context, int rowNumber, int originalRow, List<string> conflictMessages)
        {
            if (string.IsNullOrWhiteSpace(newValue))
                return;
            var parsed = ParseDateTimeOffset(newValue);
            if (parsed.HasValue && parsed != existing)
                AddMessage(conflictMessages, rowNumber, context, fieldName, "conflicts with", originalRow);
        }

        static string FindOverrideValue(CatalogItem item, StoreId store)
        {
            if (item.StoreIdOverrides == null)
                return null;
            foreach (var o in item.StoreIdOverrides)
            {
                if (o.Store == store)
                    return o.Value;
            }
            return null;
        }

        static ProductBadge BuildBadge(string text, string imageUrl)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            return new ProductBadge
            {
                Text = text,
                ImageUrl = NullIfEmpty(imageUrl),
            };
        }

        static bool AreBadgesEqual(ProductBadge a, ProductBadge b)
        {
            if (a == null || b == null)
            {
                return a == null && b == null;
            }
            return a.Text == b.Text && a.ImageUrl == b.ImageUrl;
        }

        static bool IsConflict<T>(T newValue, T existingValue)
        {
            return newValue is not null && !newValue.Equals(existingValue);
        }

        static void AddConflictIfChanged<T>(T newValue, T existingValue, string fieldName,
            string context, int rowNumber, int originalRow, List<string> conflictMessages)
        {
            if (IsConflict(newValue, existingValue))
            {
                AddMessage(conflictMessages, rowNumber, context, fieldName, "conflicts with", originalRow);
            }
        }

        static void CheckDuplicate(bool isIdentical, string fieldName, string context,
            int rowNumber, int originalRow,
            List<string> duplicateMessages, List<string> conflictMessages)
        {
            if (isIdentical)
            {
                AddMessage(duplicateMessages, rowNumber, context, fieldName, "duplicate of", originalRow);
            }
            else
            {
                AddMessage(conflictMessages, rowNumber, context, fieldName, "conflicts with", originalRow);
            }
        }

        static void AddMessage(List<string> messages, int rowNumber, string context, string fieldName,
            string verb, int originalRow)
        {
            messages.Add($"Row {rowNumber}: ({context}) {fieldName} — {verb} row {originalRow}");
        }

        static string NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

        static string GetField(string[] fields, Dictionary<string, int> columnMap, string columnName)
        {
            if (!columnMap.TryGetValue(columnName, out var index) || index >= fields.Length)
            {
                return string.Empty;
            }

            return fields[index]?.Trim() ?? string.Empty;
        }

        static Dictionary<string, int> BuildColumnMap(string[] headerFields)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headerFields.Length; i++)
            {
                var name = headerFields[i]?.Trim();
                if (!string.IsNullOrEmpty(name) && !map.ContainsKey(name))
                {
                    map[name] = i;
                }
            }
            return map;
        }

        static ProductType ParseProductType(string value, int rowNumber, List<AssetState> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ProductType.Consumable;
            if (Enum.TryParse<ProductType>(value, true, out var result))
                return result;

            issues.Add(new AssetState(
                $"Row {rowNumber}: unknown ProductType '{value}'",
                $"Defaulting to {ProductType.Consumable}.",
                SeverityLevel.Warning, ParseStateType));
            return ProductType.Consumable;
        }

        static TranslationLocale ParseLocale(string value, int rowNumber, List<AssetState> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
                return TranslationLocale.en_US;
            // Schema/BCP-47 uses hyphens (e.g. "en-US"); the C# enum name uses underscores
            // (e.g. en_US). Accept both.
            var normalized = value.Replace('-', '_');
            if (Enum.TryParse<TranslationLocale>(normalized, true, out var result))
                return result;

            issues.Add(new AssetState(
                $"Row {rowNumber}: unknown Language '{value}'",
                $"Defaulting to {FormatLocale(TranslationLocale.en_US)}.",
                SeverityLevel.Warning, ParseStateType));
            return TranslationLocale.en_US;
        }

        static string FormatLocale(TranslationLocale locale) => locale.ToString().Replace('_', '-');

        public string Serialize(List<CatalogItem> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                $"{k_ColumnCatalogListingId},{k_ColumnSku},{k_ColumnTitle},{k_ColumnDescription}," +
                $"{k_ColumnSubtitle},{k_ColumnBadgeText},{k_ColumnBadgeImageUrl}," +
                $"{k_ColumnLanguage},{k_ColumnProductType}," +
                $"{k_ColumnCurrencyCode},{k_ColumnAmount},{k_ColumnWebshopPrice}," +
                $"{k_ColumnImageUrl},{k_ColumnGoogleOverride},{k_ColumnAppleOverride}," +
                $"{k_ColumnXboxStoreOverride},{k_ColumnMacAppStoreOverride}," +
                $"{k_ColumnIsWebshopAvailable},{k_ColumnCategory}," +
                $"{k_ColumnHdImageUrl},{k_ColumnHdImageAltText}," +
                $"{k_ColumnPromotionType},{k_ColumnPromotionStartsAt},{k_ColumnPromotionEndsAt}");

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.uSku))
                {
                    continue;
                }

                var catalogListingId = string.IsNullOrWhiteSpace(item.CatalogListingId) ? item.uSku : item.CatalogListingId;
                var details = item.ProductDetails ?? new List<ProductDetails>();
                var pricing = item.PricingDetails ?? new List<PricingDetails>();
                var categories = item.Categories ?? new List<string>();
                var hdImages = item.HdImages ?? new List<HdImage>();
                var googleOverride = FindOverrideValue(item, StoreId.Google);
                var appleOverride = FindOverrideValue(item, StoreId.Apple);
                var xboxStoreOverride = FindOverrideValue(item, StoreId.XboxStore);
                var macAppStoreOverride = FindOverrideValue(item, StoreId.MacAppStore);
                var promotionType = item.Promotion?.Type.ToString() ?? string.Empty;
                var promotionStartsAt = FormatDateTimeOffset(item.Promotion?.StartsAt);
                var promotionEndsAt = FormatDateTimeOffset(item.Promotion?.EndsAt);

                var rowCount = Math.Max(1, Math.Max(
                    Math.Max(details.Count, pricing.Count),
                    Math.Max(categories.Count, hdImages.Count)));

                for (var i = 0; i < rowCount; i++)
                {
                    var detail = i < details.Count ? details[i] : null;
                    var price = i < pricing.Count ? pricing[i] : null;
                    var category = i < categories.Count ? categories[i] : string.Empty;
                    var hdImg = i < hdImages.Count ? hdImages[i] : null;

                    sb.Append(CsvEscape(catalogListingId)).Append(',');
                    sb.Append(CsvEscape(item.uSku)).Append(',');
                    sb.Append(CsvEscape(detail?.Title ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(detail?.Description ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(detail?.Subtitle ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(detail?.Badge?.Text ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(detail?.Badge?.ImageUrl ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(detail != null ? FormatLocale(detail.Language) : "en-US")).Append(',');
                    sb.Append(CsvEscape(item.ProductType.ToString())).Append(',');
                    sb.Append(CsvEscape(price?.CurrencyCode ?? string.Empty)).Append(',');
                    sb.Append(price != null
                        ? price.Amount.ToString("G", CultureInfo.InvariantCulture)
                        : string.Empty).Append(',');
                    sb.Append(price != null && price.IsWebshopPriceSet
                        ? price.WebshopPrice.ToString("G", CultureInfo.InvariantCulture)
                        : string.Empty).Append(',');
                    sb.Append(CsvEscape(item.ImageUrl ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(googleOverride ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(appleOverride ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(xboxStoreOverride ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(macAppStoreOverride ?? string.Empty)).Append(',');
                    sb.Append(item.IsWebshopAvailable ? "true" : string.Empty).Append(',');
                    sb.Append(CsvEscape(category)).Append(',');
                    sb.Append(CsvEscape(hdImg?.Url ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(hdImg?.AltText ?? string.Empty)).Append(',');
                    sb.Append(CsvEscape(promotionType)).Append(',');
                    sb.Append(CsvEscape(promotionStartsAt)).Append(',');
                    sb.AppendLine(CsvEscape(promotionEndsAt));
                }
            }

            return sb.ToString();
        }

        static string FormatDateTimeOffset(DateTimeOffset? value) =>
            value?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty;

        static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        static List<string[]> ParseLines(string csvContent)
        {
            var results = new List<string[]>();
            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < csvContent.Length; i++)
            {
                var c = csvContent[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csvContent.Length && csvContent[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && i + 1 < csvContent.Length && csvContent[i + 1] == '\n')
                    {
                        i++;
                    }

                    fields.Add(current.ToString());
                    current.Clear();
                    results.Add(fields.ToArray());
                    fields.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (fields.Count > 0 || current.Length > 0)
            {
                fields.Add(current.ToString());
                results.Add(fields.ToArray());
            }

            return results;
        }
    }
}
