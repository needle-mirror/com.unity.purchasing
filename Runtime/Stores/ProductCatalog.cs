using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Reflection;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents a price in a format that is serialized as both a decimal and a double.
    /// </summary>
    [Serializable]
    public class Price : ISerializationCallbackReceiver
    {
        public decimal value;

        [SerializeField] private int[] data;

#pragma warning disable 414 // This field appears to be unused, but it is here for serialization
        [SerializeField] private double num;
#pragma warning restore 414

        public void OnBeforeSerialize()
        {
            data = decimal.GetBits(value);
            num = decimal.ToDouble(value);
        }

        public void OnAfterDeserialize()
        {
            if (data != null && data.Length == 4)
            {
                value = new decimal(data);
            }
        }
    }

    /// <summary>
    /// Represents a pair of store identifier and product id for the store.
    /// </summary>
    [Serializable]
    public class StoreID
    {
        public string store;
        public string id;

        public StoreID(string store_, string id_)
        {
            store = store_;
            id = id_;
        }
    }

    /// <summary>
    /// The locales supported by Google for IAP product translation.
    /// </summary>
    public enum TranslationLocale
    {
        // Added for Google:
        zh_TW, // Chinese
        cs_CZ, // Czech
        da_DK, // Danish
        nl_NL, // Dutch
        en_US, // English
        fr_FR, // French
        fi_FI, // Finnish
        de_DE, // German
        iw_IL, // Hebrew
        hi_IN, // Hindi
        it_IT, // Italian
        ja_JP, // Japanese
        ko_KR, // Korean
        no_NO, // Norwegian
        pl_PL, // Polish
        pt_PT, // Portuguese
        ru_RU, // Russian
        es_ES, // Spanish
        sv_SE, // Swedish
        // Added for Xiaomi:
        zh_CN, // Chinese (Simplified)
        // Added for Apple:
        en_AU, // English (Australia)
        en_CA, // English (Canada)
        en_GB, // English (U.K.)
        fr_CA, // French (Canada)
        el_GR, // Greek
        id_ID, // Indonesian
        ms_MY, // Malay
        pt_BR, // Portuguese (Brazil)
        es_MX, // Spanish (Mexico)
        th_TH, // Thai
        tr_TR, // Turkish
        vi_VN, // Vietnamese
    }

    public static class LocaleExtensions
    {
        /// <summary>
        /// Must match 1:1 "TranslationLocale"
        /// </summary>
        private static readonly string[] Labels =
        {
            "Chinese (Traditional)",
            "Czech",
            "Danish",
            "Dutch",
            "English (U.S.)",
            "French",
            "Finnish",
            "German",
            "Hebrew",
            "Hindi",
            "Italian",
            "Japanese",
            "Korean",
            "Norwegian",
            "Polish",
            "Portuguese (Portugal)",
            "Russian",
            "Spanish (Spain)",
            "Swedish",
            "Chinese (Simplified)",
            "English (Australia)",
            "English (Canada)",
            "English (U.K.)",
            "French (Canada)",
            "Greek",
            "Indonesian",
            "Malay",
            "Portuguese (Brazil)",
            "Spanish (Mexico)",
            "Thai",
            "Turkish",
            "Vietnamese"
        };

        private static readonly TranslationLocale[] GoogleLocales =
        {
            TranslationLocale.zh_TW,
            TranslationLocale.cs_CZ,
            TranslationLocale.da_DK,
            TranslationLocale.nl_NL,
            TranslationLocale.en_US,
            TranslationLocale.fr_FR,
            TranslationLocale.fi_FI,
            TranslationLocale.de_DE,
            TranslationLocale.iw_IL,
            TranslationLocale.hi_IN,
            TranslationLocale.it_IT,
            TranslationLocale.ja_JP,
            TranslationLocale.ko_KR,
            TranslationLocale.no_NO,
            TranslationLocale.pl_PL,
            TranslationLocale.pt_PT,
            TranslationLocale.ru_RU,
            TranslationLocale.es_ES,
            TranslationLocale.sv_SE,
        };

        private static readonly TranslationLocale[] AppleLocales =
        {
            TranslationLocale.zh_CN, // Chinese (Simplified)
            TranslationLocale.zh_TW, // Chinese (Traditional)
            TranslationLocale.da_DK, // Danish
            TranslationLocale.nl_NL, // Dutch
            TranslationLocale.en_AU, // English (Australia)
            TranslationLocale.en_CA, // English (Canada)
            TranslationLocale.en_GB, // English (U.K.)
            TranslationLocale.en_US, // English (U.S.)
            TranslationLocale.fi_FI, // Finnish
            TranslationLocale.fr_FR, // French
            TranslationLocale.fr_CA, // French (Canada)
            TranslationLocale.de_DE, // German
            TranslationLocale.el_GR, // Greek
            TranslationLocale.id_ID, // Indonesian
            TranslationLocale.it_IT, // Italian
            TranslationLocale.ja_JP, // Japanese
            TranslationLocale.ko_KR, // Korean
            TranslationLocale.ms_MY, // Malay
            TranslationLocale.no_NO, // Norwegian
            TranslationLocale.pt_BR, // Portuguese (Brazil)
            TranslationLocale.pt_PT, // Portuguese (Portugal)
            TranslationLocale.ru_RU, // Russian
            TranslationLocale.es_MX, // Spanish (Mexico)
            TranslationLocale.es_ES, // Spanish (Spain)
            TranslationLocale.sv_SE, // Swedish
            TranslationLocale.th_TH, // Thai
            TranslationLocale.tr_TR, // Turkish
            TranslationLocale.vi_VN, // Vietnamese
        };

        private static string[] LabelsWithSupportedPlatforms;

        /// <summary>
        /// For every enum value in TranslationLocale, build a string with Labels + GoogleLocales for
        /// each platform supported.
        /// </summary>
        /// <returns></returns>
        public static string[] GetLabelsWithSupportedPlatforms()
        {
            if (LabelsWithSupportedPlatforms != null)
                return LabelsWithSupportedPlatforms;

            LabelsWithSupportedPlatforms = new string[Enum.GetValues(typeof(TranslationLocale)).Length];

            List<TranslationLocale> googleLocalesList = GoogleLocales.ToList();
            List<TranslationLocale> appleLocalesList = AppleLocales.ToList();

            int i = 0;
            foreach (TranslationLocale locale in Enum.GetValues(typeof(TranslationLocale)))
            {
                var platforms = new List<string>();
                if (googleLocalesList.Contains(locale))
                    platforms.Add("Google Play");
                if (appleLocalesList.Contains(locale))
                    platforms.Add("Apple");

                var platformSuffix = string.Join(", ", platforms.ToArray());

                LabelsWithSupportedPlatforms[i] = Labels[i] + " (" + platformSuffix + ")";

                i++;
            }

            return LabelsWithSupportedPlatforms;
        }

        public static bool SupportedOnApple(this TranslationLocale locale)
        {
            return AppleLocales.Contains(locale);
        }

        public static bool SupportedOnGoogle(this TranslationLocale locale)
        {
            return GoogleLocales.Contains(locale);
        }
    }

    /// <summary>
    /// A description of an IAP product. Includes both a title and a longer description, plus an optional locale for
    /// specifying the language of this description. Characters wider than one byte are escaped as \uXXXX for
    /// serialization to work around a bug in Unity's JSONUtility deserialization prior to Unity 5.6.
    /// </summary>
    [Serializable]
    public class LocalizedProductDescription
    {
        public TranslationLocale googleLocale = TranslationLocale.en_US;
        [SerializeField]
        private string title;
        [SerializeField]
        private string description;

        public LocalizedProductDescription Clone()
        {
            var desc = new LocalizedProductDescription ();

            desc.googleLocale = this.googleLocale;
            desc.Title = this.Title;
            desc.Description = this.Description;

            return desc;
        }

        public string Title {
            get {
                return DecodeNonLatinCharacters(title);
            }
            set {
                title = EncodeNonLatinCharacters(value);
            }
        }

        public string Description {
            get {
                return DecodeNonLatinCharacters(description);
            }
            set {
                description = EncodeNonLatinCharacters(value);
            }
        }

        private static string EncodeNonLatinCharacters(string s)
        {
            if (s == null)
                return s;

            var sb = new StringBuilder();
            foreach (char c in s) {
                if (c > 127) {
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                } else {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string DecodeNonLatinCharacters(string s)
        {
            if (s == null)
                return s;

            return Regex.Replace(s, @"\\u(?<Value>[a-zA-Z0-9]{4})", m => {
                return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
            });
        }
    }

    /// <summary>
    /// Represents the definition of a payout in the product catalog.
    /// </summary>
    [Serializable]
    public class ProductCatalogPayout
    {
        // Values here should mirror the values in the Core PayoutType enum, but we don't want to use that enum
        // directly because it will create a dependency between the plugin and a particular core/editor version.
        public enum ProductCatalogPayoutType
        {
            Other,
            Currency,
            Item,
            Resource
        }

        // Serialize the type as a string for readability and future-proofing
        [SerializeField]
        string t = ProductCatalogPayoutType.Other.ToString();
        public ProductCatalogPayoutType type {
            get {
                var retval = ProductCatalogPayoutType.Other;
                if (Enum.IsDefined(typeof(ProductCatalogPayoutType), t))
                    retval = (ProductCatalogPayoutType)Enum.Parse (typeof (ProductCatalogPayoutType), t);
                return retval;
            }
            set {
                t = value.ToString ();
            }
        }
        public string typeString {
            get {
                return t;
            }
        }

        public const int MaxSubtypeLength = 64;

        [SerializeField]
        string st = string.Empty;
        public string subtype {
            get {
                return st;
            }
            set {
                if (value.Length > MaxSubtypeLength)
                    throw new ArgumentException (string.Format ("subtype should be no longer than {0} characters", MaxSubtypeLength));
                st = value;
            }
        }

        [SerializeField]
        double q;
        public double quantity {
            get {
                return q;
            }
            set {
                q = value;
            }
        }

        public const int MaxDataLength = 1024;

        [SerializeField]
        string d = string.Empty;
        public string data {
            get {
                return d;
            }
            set {
                if (value.Length > MaxDataLength)
                    throw new ArgumentException (string.Format ("data should be no longer than {0} characters", MaxDataLength));
                d = value;
            }
        }
    }

    /// <summary>
    /// Represents a single product from the product catalog. Each item contains some common fields and some fields
    /// that are specific to a particular store.
    /// </summary>
    [Serializable]
    public class ProductCatalogItem
    {
        // Local configuration fields
        public string id;
        public ProductType type;
        //public Dictionary<string, string> storeSpecificIDs = new Dictionary<string, string> ();
        [SerializeField]
        private List<StoreID> storeIDs = new List<StoreID>();

        public LocalizedProductDescription defaultDescription = new LocalizedProductDescription();

        // Apple configuration fields
        public string screenshotPath;
        public int applePriceTier = 0;

        // Google configuration fields
        public Price googlePrice = new Price();
        public string pricingTemplateID;
        [SerializeField]
        private List<LocalizedProductDescription> descriptions = new List<LocalizedProductDescription>();

        // UDP configuration fields
        public Price udpPrice = new Price();

        // Payouts
        [SerializeField]
        private List<ProductCatalogPayout> payouts = new List<ProductCatalogPayout>();

        public void AddPayout()
        {
            payouts.Add(new ProductCatalogPayout());
        }

        public void RemovePayout(ProductCatalogPayout payout)
        {
            payouts.Remove(payout);
        }

        public IList<ProductCatalogPayout> Payouts {
            get {
                return payouts;
            }
        }

        public ProductCatalogItem Clone()
        {
            ProductCatalogItem item = new ProductCatalogItem ();

            item.id = this.id;
            item.type = this.type;
            item.SetStoreIDs (this.allStoreIDs);
            item.defaultDescription = this.defaultDescription.Clone ();
            item.screenshotPath = this.screenshotPath;
            item.applePriceTier = this.applePriceTier;
            item.googlePrice.value = this.googlePrice.value;
            item.pricingTemplateID = this.pricingTemplateID;
            foreach (var desc in this.descriptions) {
                item.descriptions.Add (desc.Clone ());
            }

            return item;
        }

        public void SetStoreID(string aStore, string aId)
        {
            storeIDs.RemoveAll((obj) => obj.store == aStore);
            if (!string.IsNullOrEmpty(aId))
                storeIDs.Add(new StoreID(aStore, aId));
        }

        public string GetStoreID(string store)
        {
            StoreID sID = storeIDs.Find((obj) => obj.store == store);
            return sID == null ? null : sID.id;
        }

        public ICollection<StoreID> allStoreIDs {
            get {
                return storeIDs;
            }
        }

        public void SetStoreIDs(ICollection<StoreID> storeIds) {
            foreach (var storeId in storeIds) {
                storeIDs.RemoveAll((obj) => obj.store == storeId.store);
                if (!string.IsNullOrEmpty(storeId.id))
                    storeIDs.Add(new StoreID(storeId.store, storeId.id));
            }
        }

        public LocalizedProductDescription GetDescription(TranslationLocale locale)
        {
            return descriptions.Find((obj) => obj.googleLocale == locale);
        }

        public LocalizedProductDescription GetOrCreateDescription(TranslationLocale locale)
        {
            return GetDescription(locale) ?? AddDescription(locale);
        }

        public LocalizedProductDescription AddDescription(TranslationLocale locale)
        {
            RemoveDescription(locale);
            var newDesc = new LocalizedProductDescription();
            newDesc.googleLocale = locale;
            descriptions.Add(newDesc);
            return newDesc;
        }

        public void RemoveDescription(TranslationLocale locale)
        {
            descriptions.RemoveAll((obj) => obj.googleLocale == locale);
        }

        public bool HasAvailableLocale {
            get {
                return Enum.GetValues(typeof(TranslationLocale)).Length > descriptions.Count + 1; // +1 for the default description
            }
        }

        public TranslationLocale NextAvailableLocale {
            get {
                foreach (TranslationLocale l in Enum.GetValues(typeof(TranslationLocale))) {
                    if (GetDescription(l) == null && defaultDescription.googleLocale != l) {
                        return l;
                    }
                }

                return TranslationLocale.en_US; // Not sure what to do if all locales have a description
            }
        }

        public ICollection<LocalizedProductDescription> translatedDescriptions {
            get {
                return descriptions;
            }
        }
    }

    /// <summary>
    /// The product catalog represents a list of IAP products, with enough information about each product to do a batch
    /// export for Apple's Application Loader or the Google Play CSV import format. To retreive the standard catalog,
    /// use ProductCatalog.LoadDefaultCatalog().
    /// </summary>
    [Serializable]
    public class ProductCatalog
    {
        private static IProductCatalogImpl instance;

        public string appleSKU;
        public string appleTeamID;
        public bool enableCodelessAutoInitialization = false;
        [SerializeField]
        private List<ProductCatalogItem> products = new List<ProductCatalogItem>();

        public ICollection<ProductCatalogItem> allProducts => products;

        public ICollection<ProductCatalogItem> allValidProducts {
            get {
                return products.Where(x => (!string.IsNullOrEmpty(x.id) && x.id.Trim().Length != 0 )).ToList();
            }
        }

        internal static void Initialize()
        {
            if (instance == null)
            {
                Initialize(new ProductCatalogImpl());
            }
        }

        /// <summary>
        /// Override the default catalog implementation.
        /// </summary>
        /// <param name="productCatalogImpl"></param>
        public static void Initialize(IProductCatalogImpl productCatalogImpl)
        {
            instance = productCatalogImpl;
        }

        public void Add(ProductCatalogItem item)
        {
            products.Add(item);
        }

        public void Remove(ProductCatalogItem item)
        {
            products.Remove(item);
        }

        /// <summary>
        /// Check if the catalog is empty. A catalog is considered empty when it contains no products with valid IDs.
        /// </summary>
        /// <returns>A boolean representing whether or not the catalog is empty.</returns>
        public bool IsEmpty()
        {
            foreach (ProductCatalogItem item in products)
            {
                if (!String.IsNullOrEmpty(item.id))
                {
                    return false;
                }
            }
            return true;
        }

        public const string kCatalogPath = "Assets/Resources/IAPProductCatalog.json";
        public const string kPrevCatalogPath = "Assets/Plugins/UnityPurchasing/Resources/IAPProductCatalog.json";

        public static string Serialize(ProductCatalog catalog)
        {
            return JsonUtility.ToJson(catalog);
        }

        public static ProductCatalog Deserialize(string catalogJSON)
        {
            return JsonUtility.FromJson<ProductCatalog>(catalogJSON);
        }

        public static ProductCatalog FromTextAsset(TextAsset asset)
        {
            return Deserialize(asset.text);
        }

        /// <summary>
        /// Loads the default catalog.
        /// </summary>
        public static ProductCatalog LoadDefaultCatalog()
        {
            Initialize();

            return instance.LoadDefaultCatalog();
        }
    }

    /// <summary>
    /// For testing
    /// </summary>
    public interface IProductCatalogImpl
    {
        ProductCatalog LoadDefaultCatalog();
    }

    /// <summary>
    /// Implementation
    /// </summary>
    internal class ProductCatalogImpl : IProductCatalogImpl
    {
        public ProductCatalog LoadDefaultCatalog()
        {
            var asset = Resources.Load("IAPProductCatalog") as TextAsset;
            if (asset != null) {
                return ProductCatalog.FromTextAsset(asset);
            } else {
                return new ProductCatalog();
            }
        }
    }
}
