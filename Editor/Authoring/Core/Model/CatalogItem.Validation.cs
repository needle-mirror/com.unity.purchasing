using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Validations;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    public partial class CatalogItem
    {
        public const string ValidationStateType = "ValidationError";

        internal const int USkuMinLength = 1;
        internal const int USkuMaxLength = 141;
        internal const int TitleMinLength = 1;
        internal const int TitleMaxLength = 50;
        internal const int DescriptionMaxLength = 250;
        internal const int SubtitleMaxLength = 50;
        internal const int BadgeTextMaxLength = 50;
        internal const string RequiredCurrencyCode = "USD";
        internal const string ValidIdChars = "a-zA-Z0-9._-";
        internal const string USkuPattern = "^[" + ValidIdChars + "]+$";
        internal const string CatalogListingIdPrefix = "catalog/";
        static readonly Regex k_USkuRegex = new(USkuPattern, RegexOptions.Compiled);

        public IReadOnlyList<AssetState> Validate()
        {
            var states = new List<AssetState>();

            var idError = GetCatalogListingIdValidationError(CatalogListingId);
            if (idError.HasValue)
                states.Add(idError.Value);

            var skuError = GetIdValidationError(uSku, "SKU");
            if (skuError.HasValue)
                states.Add(skuError.Value);

            if (PricingDetails == null ||
                PricingDetails.Any(d => string.IsNullOrWhiteSpace(d.CurrencyCode)))
            {
                states.Add(new AssetState("Missing Pricing Details",
                    "A pricing detail is missing data",
                    SeverityLevel.Error, ValidationStateType));
            }
            else if (!PricingDetails.Any(d =>
                         string.Equals(d.CurrencyCode, RequiredCurrencyCode, StringComparison.OrdinalIgnoreCase)))
            {
                states.Add(new AssetState($"Missing {RequiredCurrencyCode} price",
                    $"A pricing entry for {RequiredCurrencyCode} is required.",
                    SeverityLevel.Error, ValidationStateType));
            }

            PricingDetails?.ForEach(d =>
            {
                if (!MinimumPriceValidation.IsPriceValid(d.CurrencyCode, d.Amount, out double minimumPrice))
                {
                    states.Add(new AssetState(
                        $"{d.CurrencyCode} is below minimum price {minimumPrice}",
                        "Prices below the minimum might not be supported by certain payment processors. Verify the minimum supported value with the processor you plan to use.",
                        SeverityLevel.Warning, ValidationStateType));
                }
            });

            if (!IsWebshopAvailable && PricingDetails != null
                && PricingDetails.Any(p => p.IsWebshopPriceSet))
            {
                states.Add(new AssetState(
                    "Webshop price set without Webshop availability",
                    "A Webshop price is set on one or more pricing entries, but \"Webshop availability\" is off. The price will not be used by the Webshop until you toggle availability on.",
                    SeverityLevel.Warning, ValidationStateType));
            }

            if (ProductDetails == null ||
                ProductDetails.Any(d => string.IsNullOrWhiteSpace(d.Title)))
            {
                states.Add(new AssetState("Missing Product Details",
                    "A product detail is missing data",
                    SeverityLevel.Error, ValidationStateType));
            }
            else
            {
                if (ProductDetails.Any(d => d.Title.Length < TitleMinLength || d.Title.Length > TitleMaxLength))
                {
                    states.Add(new AssetState("Invalid Title length",
                        $"Each title must be between {TitleMinLength} and {TitleMaxLength} characters.",
                        SeverityLevel.Error, ValidationStateType));
                }
                if (ProductDetails.Any(d => !string.IsNullOrEmpty(d.Description) && d.Description.Length > DescriptionMaxLength))
                {
                    states.Add(new AssetState("Invalid Description length",
                        $"Each description must be at most {DescriptionMaxLength} characters.",
                        SeverityLevel.Error, ValidationStateType));
                }
                if (ProductDetails.Any(d => !string.IsNullOrEmpty(d.Subtitle) && d.Subtitle.Length > SubtitleMaxLength))
                {
                    states.Add(new AssetState("Invalid Subtitle length",
                        $"Each subtitle must be at most {SubtitleMaxLength} characters.",
                        SeverityLevel.Error, ValidationStateType));
                }
                if (ProductDetails.Any(d => d.Badge != null && !string.IsNullOrEmpty(d.Badge.Text) && d.Badge.Text.Length > BadgeTextMaxLength))
                {
                    states.Add(new AssetState("Invalid Badge text length",
                        $"Each badge text must be at most {BadgeTextMaxLength} characters.",
                        SeverityLevel.Error, ValidationStateType));
                }
                if (ProductDetails.Any(d => d.Badge != null && string.IsNullOrEmpty(d.Badge.Text) && !string.IsNullOrEmpty(d.Badge.ImageUrl)))
                {
                    states.Add(new AssetState("Badge image without text",
                        "A badge has an image URL but no text. The badge will be ignored.",
                        SeverityLevel.Warning, ValidationStateType));
                }
            }

            return states;
        }

        internal static AssetState? GetIdValidationError(string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new AssetState($"Missing {label}", $"The {label} is missing.",
                    SeverityLevel.Error, ValidationStateType);
            }
            if (id.Length < USkuMinLength || id.Length > USkuMaxLength)
            {
                return new AssetState($"Invalid {label} length",
                    $"The {label} must be between {USkuMinLength} and {USkuMaxLength} characters.",
                    SeverityLevel.Error, ValidationStateType);
            }
            if (!k_USkuRegex.IsMatch(id))
            {
                return new AssetState($"Invalid {label} format",
                    $"The {label} must match the pattern {USkuPattern} (letters, digits, '.', '_' and '-' only).",
                    SeverityLevel.Error, ValidationStateType);
            }
            return null;
        }

        internal static AssetState? GetCatalogListingIdValidationError(string id)
        {
            const string label = "catalog item ID";
            if (string.IsNullOrWhiteSpace(id))
            {
                return new AssetState($"Missing {label}", $"The {label} is missing.",
                    SeverityLevel.Error, ValidationStateType);
            }
            if (!id.StartsWith(CatalogListingIdPrefix, StringComparison.Ordinal))
            {
                return new AssetState($"Invalid {label} format",
                    $"The {label} must start with '{CatalogListingIdPrefix}'.",
                    SeverityLevel.Error, ValidationStateType);
            }
            return GetIdValidationError(id.Substring(CatalogListingIdPrefix.Length), label);
        }
    }
}
