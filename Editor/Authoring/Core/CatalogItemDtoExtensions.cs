using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core
{
    public static class CatalogItemDtoExtensions
    {
        const string k_ManagedByKey   = "managedBy";
        const string k_ManagedByValue = "In App Purchase";

        public static CatalogItemDto ToDto(this CatalogItem item)
        {
            return new CatalogItemDto
            {
                uSku = item.uSku,
                ProductType = ConvertTypeToDto(item.ProductType),
                PricingDetails = item.PricingDetails?.Select(pd => pd.ToDto()).ToList()
                    ?? new List<PricingDetailsDto>(),
                ProductDetails = item.ProductDetails?.Select(ConvertToDto).ToList()
                    ?? new List<ProductDetailsDto>(),
                ImageUrl = NullIfEmpty(item.ImageUrl),
                StoreIdOverrides = ConvertToDto(item.StoreIdOverrides),
                Categories = item.IsWebshopAvailable ? ConvertCategoriesToDto(item.Categories) : null,
                HdImages = item.IsWebshopAvailable ? ConvertHdImagesToDto(item.HdImages) : null,
                Promotion = item.IsWebshopAvailable ? ConvertToDto(item.Promotion) : null,
            };
        }

        public static CatalogItem ToCatalogItem(this CatalogItemDto dto)
        {
            return new CatalogItem
            {
                uSku = dto.uSku,
                ProductType = ConvertTypeFromDto(dto.ProductType),
                PricingDetails = dto.PricingDetails?.Select(pd => pd.ToPricingDetails()).ToList()
                    ?? new List<PricingDetails>(),
                ProductDetails = dto.ProductDetails?.Select(ConvertFromDto).ToList()
                    ?? new List<ProductDetails>(),
                ImageUrl = dto.ImageUrl,
                StoreIdOverrides = dto.StoreIdOverrides?.Select(ConvertFromDto).ToList(),
                IsWebshopAvailable = dto.Schemas?.Any(s => s != null && s.Contains(LiveContentConfigClient.k_WebshopMarker)) ?? false,
                Categories = dto.Categories is null ? null : new List<string>(dto.Categories),
                HdImages = dto.HdImages?.Select(ConvertFromDto).ToList(),
                Promotion = ConvertFromDto(dto.Promotion),
            };
        }

        public static PricingDetailsDto ToDto(this PricingDetails pd)
        {
            return new PricingDetailsDto
            {
                CurrencyCode = pd.CurrencyCode,
                Amount = ToMicros(pd.Amount),
                WebshopPrice = pd.IsWebshopPriceSet ? ToMicros(pd.WebshopPrice) : null
            };
        }

        public static PricingDetails ToPricingDetails(this PricingDetailsDto pd)
        {
            return new PricingDetails
            {
                CurrencyCode = pd.CurrencyCode,
                Amount = pd.Amount / 1_000_000D,
                WebshopPrice = pd.WebshopPrice / 1_000_000D ?? 0
            };
        }

        public static void PreserveRoundTripFields(this CatalogItemDto dto, CatalogItemDto source)
        {
            dto.AdditionalProperties = source.AdditionalProperties;
            dto.Metadata = source.Metadata;
            ForwardUnknownSchemas(dto, source);
        }

        public static void ApplyManagedByMetadata(this CatalogItemDto dto)
        {
            dto.Metadata ??= new JObject();
            dto.Metadata[k_ManagedByKey] = k_ManagedByValue;
        }

        static ProductDetailsDto ConvertToDto(ProductDetails pd)
        {
            return new ProductDetailsDto
            {
                Title = pd.Title,
                Description = NullIfEmpty(pd.Description),
                Language = pd.Language,
                Subtitle = NullIfEmpty(pd.Subtitle),
                Badge = ConvertToDto(pd.Badge)
            };
        }

        static ProductDetails ConvertFromDto(ProductDetailsDto pd)
        {
            return new ProductDetails
            {
                Title = pd.Title,
                Description = pd.Description,
                Language = pd.Language,
                Subtitle = pd.Subtitle,
                Badge = pd.Badge == null ? null : new ProductBadge { Text = pd.Badge.Text, ImageUrl = pd.Badge.ImageUrl }
            };
        }

        static ProductBadgeDto ConvertToDto(ProductBadge badge)
        {
            if (badge is null || string.IsNullOrEmpty(badge.Text))
                return null;
            return new ProductBadgeDto
            {
                Text = badge.Text,
                ImageUrl = NullIfEmpty(badge.ImageUrl)
            };
        }

        static List<StoreIdOverrideDto> ConvertToDto(List<StoreIdOverride> overrides)
        {
            if (overrides is null)
                return null;
            var result = new List<StoreIdOverrideDto>(overrides.Count);
            foreach (var o in overrides)
            {
                if (o is null || string.IsNullOrEmpty(o.Value))
                    continue;
                result.Add(new StoreIdOverrideDto
                {
                    Store = ConvertStoreIdToDto(o.Store),
                    Value = o.Value
                });
            }
            return result.Count == 0 ? null : result;
        }

        static StoreIdOverride ConvertFromDto(StoreIdOverrideDto s) =>
            new StoreIdOverride { Store = ConvertStoreIdFromDto(s.Store), Value = s.Value };

        static List<HdImageDto> ConvertHdImagesToDto(List<HdImage> images)
        {
            if (images is null)
                return null;
            var result = new List<HdImageDto>(images.Count);
            foreach (var img in images)
            {
                if (img is null || string.IsNullOrEmpty(img.Url))
                    continue;
                result.Add(new HdImageDto { Url = img.Url, AltText = NullIfEmpty(img.AltText) });
            }
            return result.Count == 0 ? null : result;
        }

        static HdImage ConvertFromDto(HdImageDto h) =>
            new HdImage { Url = h.Url, AltText = h.AltText };

        static PromotionDto ConvertToDto(Promotion p) =>
            p is null ? null : new PromotionDto
            {
                Type = ConvertPromotionTypeToDto(p.Type),
                StartsAt = p.StartsAt,
                EndsAt = p.EndsAt,
            };

        static Promotion ConvertFromDto(PromotionDto p) => p is null
                ? null
                : new Promotion
            {
                Type = ConvertPromotionTypeFromDto(p.Type),
                StartsAt = p.StartsAt,
                EndsAt = p.EndsAt,
            };

        static ProductTypeDto ConvertTypeToDto(ProductType productType)
        {
            switch (productType)
            {
                case ProductType.Consumable:    return ProductTypeDto.Consumable;
                case ProductType.NonConsumable: return ProductTypeDto.NonConsumable;
                case ProductType.Subscription:  return ProductTypeDto.Subscription;
                case ProductType.Unknown:       return ProductTypeDto.Unknown;
                default: throw new ArgumentOutOfRangeException(nameof(productType), productType, null);
            }
        }

        static ProductType ConvertTypeFromDto(ProductTypeDto productType)
        {
            switch (productType)
            {
                case ProductTypeDto.Consumable:
                    return ProductType.Consumable;
                case ProductTypeDto.NonConsumable:
                case ProductTypeDto.NonConsumable2:  // legacy wire value "non-consumable"
                    return ProductType.NonConsumable;
                case ProductTypeDto.Subscription:
                    return ProductType.Subscription;
                case ProductTypeDto.Unknown:
                    return ProductType.Unknown;
                default: throw new ArgumentOutOfRangeException(nameof(productType), productType, null);
            }
        }

        static StoreIdDto ConvertStoreIdToDto(StoreId store)
        {
            switch (store)
            {
                case StoreId.Apple:       return StoreIdDto.Apple;
                case StoreId.Google:      return StoreIdDto.Google;
                case StoreId.XboxStore:   return StoreIdDto.XboxStore;
                case StoreId.MacAppStore: return StoreIdDto.MacAppStore;
                default: throw new ArgumentOutOfRangeException(nameof(store), store, null);
            }
        }

        static StoreId ConvertStoreIdFromDto(StoreIdDto store)
        {
            switch (store)
            {
                case StoreIdDto.Apple:       return StoreId.Apple;
                case StoreIdDto.Google:      return StoreId.Google;
                case StoreIdDto.XboxStore:   return StoreId.XboxStore;
                case StoreIdDto.MacAppStore: return StoreId.MacAppStore;
                default: throw new ArgumentOutOfRangeException(nameof(store), store, null);
            }
        }

        static PromotionTypeDto ConvertPromotionTypeToDto(PromotionType t)
        {
            switch (t)
            {
                case PromotionType.Sale:    return PromotionTypeDto.Sale;
                case PromotionType.Bonus:   return PromotionTypeDto.Bonus;
                case PromotionType.Limited: return PromotionTypeDto.Limited;
                default: throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }
        }

        static PromotionType ConvertPromotionTypeFromDto(PromotionTypeDto t)
        {
            switch (t)
            {
                case PromotionTypeDto.Sale:    return PromotionType.Sale;
                case PromotionTypeDto.Bonus:   return PromotionType.Bonus;
                case PromotionTypeDto.Limited: return PromotionType.Limited;
                default: throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }
        }


        static List<string> ConvertCategoriesToDto(List<string> categories)
        {
            if (categories is null)
                return null;
            var result = new List<string>(categories.Count);
            foreach (var c in categories)
            {
                if (!string.IsNullOrEmpty(c))
                    result.Add(c);
            }
            return result.Count == 0 ? null : result;
        }

        static void ForwardUnknownSchemas(CatalogItemDto target, CatalogItemDto source)
        {
            if (source.Schemas is null)
                return;
            target.Schemas ??= new List<string>();
            foreach (var s in source.Schemas)
            {
                if (LiveContentConfigClient.IsSdkControlled(s))
                    continue;
                if (!target.Schemas.Contains(s))
                    target.Schemas.Add(s);
            }
        }

        static long ToMicros(double amount) =>
            checked((long)Math.Round(amount * 1_000_000D, MidpointRounding.AwayFromZero));

        static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;
    }
}
