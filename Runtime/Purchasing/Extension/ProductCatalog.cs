using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents a price in a format that is serialized as both a decimal and a double.
    /// </summary>
    [Serializable]
    public class Price : ISerializationCallbackReceiver
    {
        /// <summary>
        /// The price as a decimal.
        /// </summary>
        public decimal value;

        [SerializeField] private int[] data;

#pragma warning disable 414 // This field appears to be unused, but it is here for serialization
        [SerializeField] private double num;
#pragma warning restore 414

        /// <summary>
        /// Callback executed before Serialization.
        /// Converts value to raw data and to a double.
        /// </summary>
        public void OnBeforeSerialize()
        {
            data = decimal.GetBits(value);
            num = decimal.ToDouble(value);
        }

        /// <summary>
        /// Callback executed after Deserialization.
        /// Converts the raw data to a decimal and asigns it to value.
        /// </summary>
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
        /// <summary>
        /// The name of the store.
        /// </summary>
        public string store;

        /// <summary>
        /// The unique id of the store.
        /// </summary>
        public string id;

        /// <summary>
        /// Constructor. Simply assigns the parameters as member data.
        /// </summary>
        /// <param name="store_"> The name of the store. </param>
        /// <param name="id_">  The unique id of the store. </param>
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
        /// <summary>
        /// Afrikaans.
        /// </summary>
        af_ZA,
        /// <summary>
        /// Albanian.
        /// </summary>
        sq_SQ,
        /// <summary>
        /// Amharic.
        /// </summary>
        am_ET,
        /// <summary>
        /// Arabic.
        /// </summary>
        ar_AE,
        /// <summary>
        /// Armenian.
        /// </summary>
        hy_AM,
        /// <summary>
        /// Azerbaijani.
        /// </summary>
        az_AZ,
        /// <summary>
        /// Bangla.
        /// </summary>
        bn_BD,
        /// <summary>
        /// Basque.
        /// </summary>
        eu_ES,
        /// <summary>
        /// Belarusian.
        /// </summary>
        be_BY,
        /// <summary>
        /// Bulgarian.
        /// </summary>
        bg_BG,
        /// <summary>
        /// Burmese.
        /// </summary>
        my_MM,
        /// <summary>
        /// Catalan.
        /// </summary>
        ca_CA,
        /// <summary>
        /// Chinese (Hong Kong).
        /// </summary>
        zh_HK,
        /// <summary>
        /// Chinese (Simplified).
        /// </summary>
        zh_CN,
        /// <summary>
        /// Chinese (Traditional).
        /// </summary>
        zh_TW,
        /// <summary>
        /// Croatian.
        /// </summary>
        hr_HR,
        /// <summary>
        /// Czech.
        /// </summary>
        cs_CZ,
        /// <summary>
        /// Danish.
        /// </summary>
        da_DK,
        /// <summary>
        /// Dutch.
        /// </summary>
        nl_NL,
        /// <summary>
        /// English (Australia).
        /// </summary>
        en_AU,
        /// <summary>
        /// English (Canada).
        /// </summary>
        en_CA,
        /// <summary>
        /// English (United States).
        /// </summary>
        en_US,
        /// <summary>
        /// English (United Kingdom).
        /// </summary>
        en_GB,
        /// <summary>
        /// English.
        /// </summary>
        en_IN,
        /// <summary>
        /// English.
        /// </summary>
        en_SG,
        /// <summary>
        /// English.
        /// </summary>
        en_ZA,
        /// <summary>
        /// Estonian.
        /// </summary>
        et_EE,
        /// <summary>
        /// Filipino.
        /// </summary>
        fil_FIL,
        /// <summary>
        /// Finnish.
        /// </summary>
        fi_FI,
        /// <summary>
        /// French (Canada).
        /// </summary>
        fr_CA,
        /// <summary>
        /// French (France).
        /// </summary>
        fr_FR,
        /// <summary>
        /// Galician.
        /// </summary>
        gl_ES,
        /// <summary>
        /// Georgian.
        /// </summary>
        ka_GE,
        /// <summary>
        /// German.
        /// </summary>
        de_DE,
        /// <summary>
        /// Greek.
        /// </summary>
        el_GR,
        /// <summary>
        /// Gujarati.
        /// </summary>
        gu_IN,
        /// <summary>
        /// Hebrew.
        /// </summary>
        iw_IL,
        /// <summary>
        /// Hindi.
        /// </summary>
        hi_IN,
        /// <summary>
        /// Hungarian.
        /// </summary>
        hu_HU,
        /// <summary>
        /// Icelandic.
        /// </summary>
        is_IS,
        /// <summary>
        /// Indonesian.
        /// </summary>
        id_ID,
        /// <summary>
        /// Italian.
        /// </summary>
        it_IT,
        /// <summary>
        /// Japanese.
        /// </summary>
        ja_JP,
        /// <summary>
        /// Kannada.
        /// </summary>
        kn_IN,
        /// <summary>
        /// Kazakh.
        /// </summary>
        kk_KZ,
        /// <summary>
        /// Khmer.
        /// </summary>
        km_KH,
        /// <summary>
        /// Korean.
        /// </summary>
        ko_KR,
        /// <summary>
        /// Kyrgyz.
        /// </summary>
        ky_KG,
        /// <summary>
        /// Lao.
        /// </summary>
        lo_LA,
        /// <summary>
        /// Latvian.
        /// </summary>
        lv_LV,
        /// <summary>
        /// Lithuanian.
        /// </summary>
        lt_LT,
        /// <summary>
        /// Macedonian.
        /// </summary>
        mk_MK,
        /// <summary>
        /// Malay (Malaysia).
        /// </summary>
        ms_MY,
        /// <summary>
        /// Malay.
        /// </summary>
        ms_MS,
        /// <summary>
        /// Malayalam.
        /// </summary>
        ml_IN,
        /// <summary>
        /// Marathi.
        /// </summary>
        mr_IN,
        /// <summary>
        /// Mongolian.
        /// </summary>
        mn_MN,
        /// <summary>
        /// Nepali.
        /// </summary>
        ne_NP,
        /// <summary>
        /// Norwegian.
        /// </summary>
        no_NO,
        /// <summary>
        /// Persian.
        /// </summary>
        fa_FA,
        /// <summary>
        /// Persian.
        /// </summary>
        fa_AE,
        /// <summary>
        /// Persian.
        /// </summary>
        fa_AF,
        /// <summary>
        /// Persian.
        /// </summary>
        fa_IR,
        /// <summary>
        /// Polish.
        /// </summary>
        pl_PL,
        /// <summary>
        /// Portuguese (Brazil).
        /// </summary>
        pt_BR,
        /// <summary>
        /// Portuguese (Portugal).
        /// </summary>
        pt_PT,
        /// <summary>
        /// Punjabi.
        /// </summary>
        pa_IN,
        /// <summary>
        /// Romanian.
        /// </summary>
        ro_RO,
        /// <summary>
        /// Romansh.
        /// </summary>
        rm_CH,
        /// <summary>
        /// Russian.
        /// </summary>
        ru_RU,
        /// <summary>
        /// Serbian.
        /// </summary>
        sr_RS,
        /// <summary>
        /// Sinhala.
        /// </summary>
        si_LK,
        /// <summary>
        /// Slovak.
        /// </summary>
        sk_SK,
        /// <summary>
        /// Slovenian.
        /// </summary>
        sl_SI,
        /// <summary>
        /// Spanish (Latin America).
        /// </summary>
        es_419,
        /// <summary>
        /// Spanish (Spain).
        /// </summary>
        es_ES,
        /// <summary>
        /// Spanish (Mexico).
        /// </summary>
        es_MX,
        /// <summary>
        /// Spanish (United States).
        /// </summary>
        es_US,
        /// <summary>
        /// Swahili.
        /// </summary>
        sw_KE,
        /// <summary>
        /// Swedish.
        /// </summary>
        sv_SE,
        /// <summary>
        /// Tamil.
        /// </summary>
        ta_IN,
        /// <summary>
        /// Telugu.
        /// </summary>
        te_IN,
        /// <summary>
        /// Thai.
        /// </summary>
        th_TH,
        /// <summary>
        /// Turkish.
        /// </summary>
        tr_TR,
        /// <summary>
        /// Ukrainian.
        /// </summary>
        uk_UA,
        /// <summary>
        /// Urdu.
        /// </summary>
        ur_UZ,
        /// <summary>
        /// Vietnamese.
        /// </summary>
        vi_VN,
        /// <summary>
        /// Zulu.
        /// </summary>
        zu_ZA,
    }

    /// <summary>
    /// Class that facilitates localization code extensions.
    /// </summary>
    public static class LocaleExtensions
    {

        /// <summary>
        /// Must match 1:1 "TranslationLocale"
        /// </summary>
        private static readonly string[] Labels =
        {
            "Afrikaans",
            "Albanian",
            "Amharic",
            "Arabic",
            "Armenian",
            "Azerbaijani",
            "Bangla",
            "Basque",
            "Belarusian",
            "Bulgarian",
            "Burmese",
            "Catalan",
            "Chinese (Hong Kong)",
            "Chinese (Simplified)",
            "Chinese (Traditional)",
            "Croatian",
            "Czech",
            "Danish",
            "Dutch",
            "English (Australia)",
            "English (Canada)",
            "English (United States)",
            "English (United Kingdom)",
            "English",
            "English",
            "English",
            "Estonian",
            "Filipino",
            "Finnish",
            "French (Canada)",
            "French (France)",
            "Galician",
            "Georgian",
            "German",
            "Greek",
            "Gujarati",
            "Hebrew",
            "Hindi",
            "Hungarian",
            "Icelandic",
            "Indonesian",
            "Italian",
            "Japanese",
            "Kannada",
            "Kazakh",
            "Khmer",
            "Korean",
            "Kyrgyz",
            "Lao",
            "Latvian",
            "Lithuanian",
            "Macedonian",
            "Malay (Malaysia)",
            "Malay",
            "Malayalam",
            "Marathi",
            "Mongolian",
            "Nepali",
            "Norwegian",
            "Persian",
            "Persian",
            "Persian",
            "Persian",
            "Polish",
            "Portuguese (Brazil)",
            "Portuguese (Portugal)",
            "Punjabi",
            "Romanian",
            "Romansh",
            "Russian",
            "Serbian",
            "Sinhala",
            "Slovak",
            "Slovenian",
            "Spanish (Latin America)",
            "Spanish (Spain)",
            "Spanish (Mexico)",
            "Spanish (United States)",
            "Swahili",
            "Swedish",
            "Tamil",
            "Telugu",
            "Thai",
            "Turkish",
            "Ukrainian",
            "Urdu",
            "Vietnamese",
            "Zulu",
        };
        private static readonly TranslationLocale[] GoogleLocales =
        {
            TranslationLocale.af_ZA,
            TranslationLocale.sq_SQ,
            TranslationLocale.am_ET,
            TranslationLocale.ar_AE,
            TranslationLocale.hy_AM,
            TranslationLocale.az_AZ,
            TranslationLocale.bn_BD,
            TranslationLocale.eu_ES,
            TranslationLocale.be_BY,
            TranslationLocale.bg_BG,
            TranslationLocale.my_MM,
            TranslationLocale.ca_CA,
            TranslationLocale.zh_HK,
            TranslationLocale.zh_CN,
            TranslationLocale.zh_TW,
            TranslationLocale.hr_HR,
            TranslationLocale.cs_CZ,
            TranslationLocale.da_DK,
            TranslationLocale.nl_NL,
            TranslationLocale.en_AU,
            TranslationLocale.en_CA,
            TranslationLocale.en_US,
            TranslationLocale.en_GB,
            TranslationLocale.en_IN,
            TranslationLocale.en_SG,
            TranslationLocale.en_ZA,
            TranslationLocale.et_EE,
            TranslationLocale.fil_FIL,
            TranslationLocale.fi_FI,
            TranslationLocale.fr_CA,
            TranslationLocale.fr_FR,
            TranslationLocale.gl_ES,
            TranslationLocale.ka_GE,
            TranslationLocale.de_DE,
            TranslationLocale.el_GR,
            TranslationLocale.gu_IN,
            TranslationLocale.iw_IL,
            TranslationLocale.hi_IN,
            TranslationLocale.hu_HU,
            TranslationLocale.is_IS,
            TranslationLocale.id_ID,
            TranslationLocale.it_IT,
            TranslationLocale.ja_JP,
            TranslationLocale.kn_IN,
            TranslationLocale.kk_KZ,
            TranslationLocale.km_KH,
            TranslationLocale.ko_KR,
            TranslationLocale.ky_KG,
            TranslationLocale.lo_LA,
            TranslationLocale.lv_LV,
            TranslationLocale.lt_LT,
            TranslationLocale.mk_MK,
            TranslationLocale.ms_MY,
            TranslationLocale.ms_MS,
            TranslationLocale.ml_IN,
            TranslationLocale.mr_IN,
            TranslationLocale.mn_MN,
            TranslationLocale.ne_NP,
            TranslationLocale.no_NO,
            TranslationLocale.fa_FA,
            TranslationLocale.fa_AE,
            TranslationLocale.fa_AF,
            TranslationLocale.fa_IR,
            TranslationLocale.pl_PL,
            TranslationLocale.pt_BR,
            TranslationLocale.pt_PT,
            TranslationLocale.pa_IN,
            TranslationLocale.ro_RO,
            TranslationLocale.rm_CH,
            TranslationLocale.ru_RU,
            TranslationLocale.sr_RS,
            TranslationLocale.si_LK,
            TranslationLocale.sk_SK,
            TranslationLocale.sl_SI,
            TranslationLocale.es_419,
            TranslationLocale.es_ES,
            TranslationLocale.es_US,
            TranslationLocale.sw_KE,
            TranslationLocale.sv_SE,
            TranslationLocale.ta_IN,
            TranslationLocale.te_IN,
            TranslationLocale.th_TH,
            TranslationLocale.tr_TR,
            TranslationLocale.uk_UA,
            TranslationLocale.ur_UZ,
            TranslationLocale.vi_VN,
            TranslationLocale.zu_ZA
        };

        private static readonly TranslationLocale[] AppleLocales =
        {
            TranslationLocale.ar_AE, // Arabic
            TranslationLocale.ca_CA, // Catalan
            TranslationLocale.zh_CN, // Chinese (Simplified)
            TranslationLocale.zh_TW, // Chinese (Traditional)
            TranslationLocale.hr_HR, // Croatian
            TranslationLocale.cs_CZ, // Czech
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
            TranslationLocale.iw_IL, // Hebrew
            TranslationLocale.hi_IN, // Hindi
            TranslationLocale.hu_HU, // Hungarian
            TranslationLocale.id_ID, // Indonesian
            TranslationLocale.it_IT, // Italian
            TranslationLocale.ja_JP, // Japanese
            TranslationLocale.ko_KR, // Korean
            TranslationLocale.ms_MY, // Malay
            TranslationLocale.no_NO, // Norwegian
            TranslationLocale.pl_PL, // Polish
            TranslationLocale.pt_BR, // Portuguese (Brazil)
            TranslationLocale.pt_PT, // Portuguese (Portugal)
            TranslationLocale.ro_RO, // Romanian
            TranslationLocale.ru_RU, // Russian
            TranslationLocale.es_MX, // Spanish (Mexico)
            TranslationLocale.es_ES, // Spanish (Spain)
            TranslationLocale.sv_SE, // Swedish
            TranslationLocale.th_TH, // Thai
            TranslationLocale.tr_TR, // Turkish
            TranslationLocale.uk_UA, // Ukrainian
            TranslationLocale.vi_VN, // Vietnamese
        };

        private static string[] LabelsWithSupportedPlatforms;

        /// <summary>
        /// For every enum value in TranslationLocale, build a string with Labels + GoogleLocales for
        /// each platform supported.
        /// </summary>
        /// <returns>Labels with supported platforms.</returns>
        public static string[] GetLabelsWithSupportedPlatforms()
        {
            if (LabelsWithSupportedPlatforms != null)
            {
                return LabelsWithSupportedPlatforms;
            }

            LabelsWithSupportedPlatforms = new string[Enum.GetValues(typeof(TranslationLocale)).Length];

            var googleLocalesList = GoogleLocales.ToList();
            var appleLocalesList = AppleLocales.ToList();

            var i = 0;
            foreach (TranslationLocale locale in Enum.GetValues(typeof(TranslationLocale)))
            {
                var platforms = new List<string>();
                if (googleLocalesList.Contains(locale))
                {
                    platforms.Add("Google Play");
                }

                if (appleLocalesList.Contains(locale))
                {
                    platforms.Add("Apple");
                }

                var platformSuffix = string.Join(", ", platforms.ToArray());

                LabelsWithSupportedPlatforms[i] = Labels[i] + " (" + platformSuffix + ")";

                i++;
            }

            return LabelsWithSupportedPlatforms;
        }

        /// <summary>
        /// Checks that a <c>TranslationLocale</c> is supported on Apple.
        /// </summary>
        /// <param name="locale"> The locale to check. </param>
        /// <returns> If the locale is supported or not. </returns>
        public static bool SupportedOnApple(this TranslationLocale locale)
        {
            return AppleLocales.Contains(locale);
        }

        /// <summary>
        /// Checks that a <c>TranslationLocale</c> is supported on Google.
        /// </summary>
        /// <param name="locale"> The locale to check. </param>
        /// <returns> If the locale is supported or not. </returns>
        public static bool SupportedOnGoogle(this TranslationLocale locale)
        {
            return GoogleLocales.Contains(locale);
        }
    }

    /// <summary>
    /// A description of an IAP product. Includes both a title and a longer description, plus an optional locale for
    /// specifying the language of this description. Characters wider than one byte are escaped as \\uXXXX for
    /// serialization to work around a bug in Unity's JSONUtility deserialization prior to Unity 5.6.
    /// </summary>
    [Serializable]
    public class LocalizedProductDescription
    {
        /// <summary>
        /// The <c>TranslationLocale</c> for GooglePlay.
        /// </summary>
        public TranslationLocale googleLocale = TranslationLocale.en_US;
        [SerializeField]
        private string title;
        [SerializeField]
        private string description;

        /// <summary>
        /// Copy this product description.
        /// </summary>
        /// <returns> A new instance identical to this object </returns>
        public LocalizedProductDescription Clone()
        {
            var desc = new LocalizedProductDescription
            {
                googleLocale = googleLocale,
                Title = Title,
                Description = Description
            };

            return desc;
        }

        /// <summary>
        /// The title of the product description.
        /// </summary>
        public string Title
        {
            get => DecodeNonLatinCharacters(title);
            set => title = EncodeNonLatinCharacters(value);
        }

        /// <summary>
        /// The product description displayed as a string.
        /// </summary>
        public string Description
        {
            get => DecodeNonLatinCharacters(description);
            set => description = EncodeNonLatinCharacters(value);
        }

        private static string EncodeNonLatinCharacters(string s)
        {
            if (s == null)
            {
                return s;
            }

            var sb = new StringBuilder();
            foreach (var c in s)
            {
                if (c > 127)
                {
                    var encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string DecodeNonLatinCharacters(string s)
        {
            if (s == null)
            {
                return s;
            }

            return Regex.Replace(s, @"\\u(?<Value>[a-zA-Z0-9]{4})", m =>
            {
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
        /// <summary>
        /// Types of Product Payouts. Mirrors the <c>PayoutType</c> enum.
        /// </summary>
        // Values here should mirror the values in the Core PayoutType enum, but we don't want to use that enum
        // directly because it will create a dependency between the plugin and a particular core/editor version.
        public enum ProductCatalogPayoutType
        {
            /// <summary>
            /// "Other" payouts are those with a customizable payout subtype.
            /// </summary>
            Other,
            /// <summary>
            /// Payout is a currency, often paired with quantity to specify the amount.
            /// </summary>
            Currency,
            /// <summary>
            /// Payout is an item.
            /// </summary>
            Item,
            /// <summary>
            /// Payout is a resource, often used in in-game economies or for crafting features.
            /// Examples: Iron, Wood.
            /// </summary>
            Resource
        }

        // Serialize the type as a string for readability and future-proofing
        [SerializeField]
        string t = ProductCatalogPayoutType.Other.ToString();
        /// <summary>
        /// The type of the payout of the product.
        /// </summary>
        public ProductCatalogPayoutType type
        {
            get
            {
                var retval = ProductCatalogPayoutType.Other;
                if (Enum.IsDefined(typeof(ProductCatalogPayoutType), t))
                {
                    retval = (ProductCatalogPayoutType)Enum.Parse(typeof(ProductCatalogPayoutType), t);
                }

                return retval;
            }
            set => t = value.ToString();
        }

        /// <summary>
        /// ProductCatalogPayoutType as a string.
        /// </summary>
        public string typeString => t;

        /// <summary>
        /// The maximum string length of the subtype for the "Other" payout type or any type requiring specification of a subtype.
        /// </summary>
        public const int MaxSubtypeLength = 64;

        [SerializeField]
        string st = string.Empty;

        /// <summary>
        /// The custom name for a subtype for the "Other" payout type.
        /// </summary>
        public string subtype
        {
            get => st;
            set
            {
                if (value.Length > MaxSubtypeLength)
                {
                    throw new ArgumentException(string.Format("subtype should be no longer than {0} characters", MaxSubtypeLength));
                }

                st = value;
            }
        }

        [SerializeField]
        double q;

        /// <summary>
        /// The quantity of payout.
        /// </summary>
        public double quantity
        {
            get => q;
            set => q = value;
        }

        /// <summary>
        /// The maximum byte length of the payout data when serialized.
        /// </summary>
        public const int MaxDataLength = 1024;

        [SerializeField]
        string d = string.Empty;
        /// <summary>
        /// The raw data of the payout.
        /// </summary>
        public string data
        {
            get => d;
            set
            {
                if (value.Length > MaxDataLength)
                {
                    throw new ArgumentException(string.Format("data should be no longer than {0} characters", MaxDataLength));
                }

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

        /// <summary>
        /// The ID of the item.
        /// </summary>
        public string id;

        /// <summary>
        /// The <c>ProductType</c> of the item.
        /// </summary>
        public ProductType type;

        [SerializeField]
#pragma warning disable IDE0044 //This field cannot be readonly because it will be set when deserialized.
        List<StoreID> storeIDs = new List<StoreID>();
#pragma warning restore IDE0044

        /// <summary>
        /// The default localized description of the product.
        /// </summary>
        public LocalizedProductDescription defaultDescription = new LocalizedProductDescription();

        // Apple configuration fields
        /// <summary>
        /// Screenshot path for Apple configuration.
        /// </summary>
        public string screenshotPath;

        /// <summary>
        /// The price tier for the Apple Store.
        /// </summary>
        public int applePriceTier = 0;

        // Google configuration fields
        /// <summary>
        /// The price for GooglePlay.
        /// </summary>
        public Price googlePrice = new Price();

        /// <summary>
        /// The price template for GooglePlay.
        /// </summary>
        public string pricingTemplateID;

        [SerializeField]
#pragma warning disable IDE0044 //This field cannot be readonly because it will be set when deserialized.
        List<LocalizedProductDescription> descriptions = new List<LocalizedProductDescription>();
#pragma warning restore IDE0044

        // Payouts
        [SerializeField]
#pragma warning disable IDE0044 //This field cannot be readonly because it will be set when deserialized.
        List<ProductCatalogPayout> payouts = new List<ProductCatalogPayout>();
#pragma warning restore IDE0044

        /// <summary>
        /// Adds a new payout to the list.
        /// </summary>
        public void AddPayout()
        {
            payouts.Add(new ProductCatalogPayout());
        }

        /// <summary>
        /// Removes a payout to the list.
        /// </summary>
        /// <param name="payout"> The payout to be removed. </param>
        public void RemovePayout(ProductCatalogPayout payout)
        {
            payouts.Remove(payout);
        }

        /// <summary>
        /// Gets the list of payouts for this product.
        /// </summary>
        /// <value> The list of payouts </value>
        public IList<ProductCatalogPayout> Payouts => payouts;

        /// <summary>
        /// Creates a copy of this object.
        /// </summary>
        /// <returns> A new instance of <c>ProductCatalogItem</c> identical to this. </returns>
        public ProductCatalogItem Clone()
        {
            var item = new ProductCatalogItem
            {
                id = id,
                type = type
            };
            item.SetStoreIDs(allStoreIDs);
            item.defaultDescription = defaultDescription.Clone();
            item.screenshotPath = screenshotPath;
            item.applePriceTier = applePriceTier;
            item.googlePrice.value = googlePrice.value;
            item.pricingTemplateID = pricingTemplateID;
            foreach (var desc in descriptions)
            {
                item.descriptions.Add(desc.Clone());
            }

            return item;
        }

        /// <summary>
        /// Assigns or adds the a store for this item by name and id.
        /// </summary>
        /// <param name="aStore"> The name of the store. </param>
        /// <param name="aId">  The unique id of the store. </param>
        public void SetStoreID(string aStore, string aId)
        {
            storeIDs.RemoveAll((obj) => obj.store == aStore);
            if (!string.IsNullOrEmpty(aId))
            {
                storeIDs.Add(new StoreID(aStore, aId));
            }
        }

        /// <summary>
        /// Gets the store id by name.
        /// </summary>
        /// <param name="store"> The name of the store. </param>
        /// <returns> The id of the store if found, otherwise returns null. </returns>
        public string GetStoreID(string store)
        {
            var sID = storeIDs.Find((obj) => obj.store == store);
            return sID == null ? null : sID.id;
        }

        /// <summary>
        /// Gets all of the <c>StoreIds</c> associated with this item.
        /// </summary>
        /// <value> A collection of all store IDs for this item. </value>
        public ICollection<StoreID> allStoreIDs => storeIDs;

        /// <summary>
        /// Assigns or modifies a collection of <c>StoreID</c>s associated with this item.
        /// </summary>
        /// <param name="storeIds"> The set of <c>StoreID</c>s to assign or overwrite. </param>
        public void SetStoreIDs(ICollection<StoreID> storeIds)
        {
            foreach (var storeId in storeIds)
            {
                storeIDs.RemoveAll((obj) => obj.store == storeId.store);
                if (!string.IsNullOrEmpty(storeId.id))
                {
                    storeIDs.Add(new StoreID(storeId.store, storeId.id));
                }
            }
        }

        /// <summary>
        /// Gets the product description, localized to a specific locale.
        /// </summary>
        /// <param name="locale"> The locale of the description desired. </param>
        /// <returns> The localized description of this item. </returns>
        public LocalizedProductDescription GetDescription(TranslationLocale locale)
        {
            return descriptions.Find((obj) => obj.googleLocale == locale);
        }

        /// <summary>
        /// Gets the product description, localized to a specific locale, or adds a default one if it's not already set.
        /// </summary>
        /// <param name="locale"> The locale of the description desired. </param>
        /// <returns> The localized description of this item. </returns>
        public LocalizedProductDescription GetOrCreateDescription(TranslationLocale locale)
        {
            return GetDescription(locale) ?? AddDescription(locale);
        }

        /// <summary>
        /// Adds a default product description, localized to a specific locale.
        /// </summary>
        /// <param name="locale"> The locale of the description desired. </param>
        /// <returns> The localized description of this item. </returns>
        public LocalizedProductDescription AddDescription(TranslationLocale locale)
        {
            RemoveDescription(locale);
            var newDesc = new LocalizedProductDescription
            {
                googleLocale = locale
            };
            descriptions.Add(newDesc);
            return newDesc;
        }

        /// <summary>
        /// Removes a product description, localized to a specific locale.
        /// </summary>
        /// <param name="locale"> The locale of the description desired. </param>
        public void RemoveDescription(TranslationLocale locale)
        {
            descriptions.RemoveAll((obj) => obj.googleLocale == locale);
        }

        /// <summary>
        /// Property that gets whether or not a valid locale is unassigned.
        /// </summary>
        /// <value> Whether or not a new locale is avalable. </value>
        public bool HasAvailableLocale => Enum.GetValues(typeof(TranslationLocale)).Length > descriptions.Count + 1; // +1 for the default description

        /// <summary>
        /// Property that gets the next avalaible locale on the list.
        /// </summary>
        /// <value> The next avalable locale. </value>
        public TranslationLocale NextAvailableLocale
        {
            get
            {
                foreach (TranslationLocale l in Enum.GetValues(typeof(TranslationLocale)))
                {
                    if (GetDescription(l) == null && defaultDescription.googleLocale != l)
                    {
                        return l;
                    }
                }

                return TranslationLocale.en_US; // Not sure what to do if all locales have a description
            }
        }

        /// <summary>
        /// Property that gets the translated descriptions.
        /// </summary>
        /// <value> A collection of all translated descriptions. </value>
        public ICollection<LocalizedProductDescription> translatedDescriptions => descriptions;
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

        /// <summary>
        /// The apple SKU of the app.
        /// </summary>
        public string appleSKU;

        /// <summary>
        /// The apple team ID of the app.
        /// </summary>
        public string appleTeamID;

        /// <summary>
        /// Enables automatic initialization when using Codeless IAP.
        /// </summary>
        public bool enableCodelessAutoInitialization = true;

        /// <summary>
        /// Enables automatic Unity Gaming Services initialization when using Codeless IAP.
        /// </summary>
        public bool enableUnityGamingServicesAutoInitialization;

        [SerializeField]
#pragma warning disable IDE0044 //This field cannot be readonly because it will be set when deserialized.
        List<ProductCatalogItem> products = new List<ProductCatalogItem>();
#pragma warning restore IDE0044

        /// <summary>
        /// The collection of all products.
        /// </summary>
        public ICollection<ProductCatalogItem> allProducts => products;

        /// <summary>
        /// The collection of all valid products.
        /// </summary>
        public ICollection<ProductCatalogItem> allValidProducts => products.Where(x => !string.IsNullOrEmpty(x.id) && x.id.Trim().Length != 0).ToList();

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
        /// <param name="productCatalogImpl">The product catalog implementation.</param>
        public static void Initialize(IProductCatalogImpl productCatalogImpl)
        {
            instance = productCatalogImpl;
        }

        /// <summary>
        /// Adds an item to the catalog.
        /// </summary>
        /// <param name="item"> The item to be added. </param>
        public void Add(ProductCatalogItem item)
        {
            products.Add(item);
        }

        /// <summary>
        /// Removes an item to the catalog.
        /// </summary>
        /// <param name="item"> The item to be removed. </param>
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
            foreach (var item in products)
            {
                if (!String.IsNullOrEmpty(item.id))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The path of the catalog file.
        /// </summary>
        public const string kCatalogPath = "Assets/Resources/IAPProductCatalog.json";

        /// <summary>
        /// The previous path of the catalog file used in older versions of Unity IAP.
        /// </summary>
        public const string kPrevCatalogPath = "Assets/Plugins/UnityPurchasing/Resources/IAPProductCatalog.json";


        /// <summary>
        /// Serializes the catalog to JSON.
        /// </summary>
        /// <param name="catalog"> The catalog. </param>
        /// <returns> The raw json string of the catalog data </returns>
        public static string Serialize(ProductCatalog catalog)
        {
            return JsonUtility.ToJson(catalog);
        }

        /// <summary>
        /// Deserializes the catalog from JSON.
        /// </summary>
        /// <param name="catalogJSON"> The raw json string of catalog data. </param>
        /// <returns> The deserialized Prodcut Catalog. </returns>
        public static ProductCatalog Deserialize(string catalogJSON)
        {
            return JsonUtility.FromJson<ProductCatalog>(catalogJSON);
        }

        /// <summary>
        /// Deserializes the catalog from a text asset.
        /// </summary>
        /// <param name="asset"> The text asset. </param>
        /// <returns> The deserialized Prodcut Catalog. </returns>
        public static ProductCatalog FromTextAsset(TextAsset asset)
        {
            return Deserialize(asset.text);
        }

        /// <summary>
        /// Loads the default catalog.
        /// </summary>
        /// <returns> The <c>ProductCatalog</c> instance. </returns>
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
        /// <summary>
        /// Loads the default catalog.
        /// </summary>
        /// <returns> The <c>ProductCatalog</c> instance. </returns>
        ProductCatalog LoadDefaultCatalog();
    }

    /// <summary>
    /// Implementation
    /// </summary>
    internal class ProductCatalogImpl : IProductCatalogImpl
    {
        /// <summary>
        /// Loads the default catalog.
        /// </summary>
        /// <returns> The <c>ProductCatalog</c> instance. </returns>
        public ProductCatalog LoadDefaultCatalog()
        {
            var asset = Resources.Load("IAPProductCatalog") as TextAsset;
            if (asset != null)
            {
                return ProductCatalog.FromTextAsset(asset);
            }
            else
            {
                return new ProductCatalog();
            }
        }
    }
}
