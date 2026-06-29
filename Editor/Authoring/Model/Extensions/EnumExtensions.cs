using System;
using UnityEngine;
using UnityEngine.Purchasing;
using CoreProductType = UnityEditor.Purchasing.Editor.Authoring.Core.ProductType;
using CoreTranslationLocale = UnityEditor.Purchasing.Editor.Authoring.Core.TranslationLocale;

namespace UnityEditor.Purchasing.Editor.Authoring.Model.Extensions
{
    static class EnumExtensions
    {
        internal static ProductType ConvertProductType(this CoreProductType coreProductType)
        {
            // Enum parity enforced by Unit Test.
            ProductType productType = (ProductType)coreProductType;
            return productType;
        }

        internal static CoreProductType ConvertProductType(this ProductType productType)
        {
            // Enum parity enforced by Unit Test.
            CoreProductType coreProductType = (CoreProductType)productType;
            return coreProductType;
        }

        internal static TranslationLocale ConvertLanguageCode(this CoreTranslationLocale coreTranslationLocale)
        {
            // Enum parity enforced by Unit Test.
            TranslationLocale translationLocale = (TranslationLocale)coreTranslationLocale;
            return translationLocale;
        }

        internal static CoreTranslationLocale ConvertLanguageCode(this TranslationLocale translationLocale)
        {
            // Enum parity enforced by Unit Test.
            CoreTranslationLocale coreTranslationLocale = (CoreTranslationLocale)translationLocale;
            return coreTranslationLocale;
        }
    }
}
