using System.Runtime.Serialization;

namespace UnityEditor.Purchasing.Editor.Authoring.Core
{
    /// <summary>
    /// The locales supported by Google for IAP product translation.
    /// </summary>
    public enum TranslationLocale
    {
        /// <summary>
        /// Afrikaans.
        /// </summary>
        [EnumMember(Value = "af-ZA")]
        af_ZA,
        /// <summary>
        /// Albanian.
        /// </summary>
        [EnumMember(Value = "sq-SQ")]
        sq_SQ,
        /// <summary>
        /// Amharic.
        /// </summary>
        [EnumMember(Value = "am-ET")]
        am_ET,
        /// <summary>
        /// Arabic.
        /// </summary>
        [EnumMember(Value = "ar-AE")]
        ar_AE,
        /// <summary>
        /// Armenian.
        /// </summary>
        [EnumMember(Value = "hy-AM")]
        hy_AM,
        /// <summary>
        /// Azerbaijani.
        /// </summary>
        [EnumMember(Value = "az-AZ")]
        az_AZ,
        /// <summary>
        /// Bangla.
        /// </summary>
        [EnumMember(Value = "bn-BD")]
        bn_BD,
        /// <summary>
        /// Basque.
        /// </summary>
        [EnumMember(Value = "eu-ES")]
        eu_ES,
        /// <summary>
        /// Belarusian.
        /// </summary>
        [EnumMember(Value = "be-BY")]
        be_BY,
        /// <summary>
        /// Bulgarian.
        /// </summary>
        [EnumMember(Value = "bg-BG")]
        bg_BG,
        /// <summary>
        /// Burmese.
        /// </summary>
        [EnumMember(Value = "my-MM")]
        my_MM,
        /// <summary>
        /// Catalan.
        /// </summary>
        [EnumMember(Value = "ca-CA")]
        ca_CA,
        /// <summary>
        /// Chinese (Hong Kong).
        /// </summary>
        [EnumMember(Value = "zh-HK")]
        zh_HK,
        /// <summary>
        /// Chinese (Simplified).
        /// </summary>
        [EnumMember(Value = "zh-CN")]
        zh_CN,
        /// <summary>
        /// Chinese (Traditional).
        /// </summary>
        [EnumMember(Value = "zh-TW")]
        zh_TW,
        /// <summary>
        /// Croatian.
        /// </summary>
        [EnumMember(Value = "hr-HR")]
        hr_HR,
        /// <summary>
        /// Czech.
        /// </summary>
        [EnumMember(Value = "cs-CZ")]
        cs_CZ,
        /// <summary>
        /// Danish.
        /// </summary>
        [EnumMember(Value = "da-DK")]
        da_DK,
        /// <summary>
        /// Dutch.
        /// </summary>
        [EnumMember(Value = "nl-NL")]
        nl_NL,
        /// <summary>
        /// English (Australia).
        /// </summary>
        [EnumMember(Value = "en-AU")]
        en_AU,
        /// <summary>
        /// English (Canada).
        /// </summary>
        [EnumMember(Value = "en-CA")]
        en_CA,
        /// <summary>
        /// English
        /// </summary>
        [EnumMember(Value = "en-US")]
        en_US,
        /// <summary>
        /// English (United Kingdom).
        /// </summary>
        [EnumMember(Value = "en-GB")]
        en_GB,
        /// <summary>
        /// English.
        /// </summary>
        [EnumMember(Value = "en-IN")]
        en_IN,
        /// <summary>
        /// English.
        /// </summary>
        [EnumMember(Value = "en-SG")]
        en_SG,
        /// <summary>
        /// English.
        /// </summary>
        [EnumMember(Value = "en-ZA")]
        en_ZA,
        /// <summary>
        /// Estonian.
        /// </summary>
        [EnumMember(Value = "et-EE")]
        et_EE,
        /// <summary>
        /// Filipino.
        /// </summary>
        [EnumMember(Value = "fil-FIL")]
        fil_FIL,
        /// <summary>
        /// Finnish.
        /// </summary>
        [EnumMember(Value = "fi-FI")]
        fi_FI,
        /// <summary>
        /// French (Canada).
        /// </summary>
        [EnumMember(Value = "fr-CA")]
        fr_CA,
        /// <summary>
        /// French (France).
        /// </summary>
        [EnumMember(Value = "fr-FR")]
        fr_FR,
        /// <summary>
        /// Galician.
        /// </summary>
        [EnumMember(Value = "gl-ES")]
        gl_ES,
        /// <summary>
        /// Georgian.
        /// </summary>
        [EnumMember(Value = "ka-GE")]
        ka_GE,
        /// <summary>
        /// German.
        /// </summary>
        [EnumMember(Value = "de-DE")]
        de_DE,
        /// <summary>
        /// Greek.
        /// </summary>
        [EnumMember(Value = "el-GR")]
        el_GR,
        /// <summary>
        /// Gujarati.
        /// </summary>
        [EnumMember(Value = "gu-IN")]
        gu_IN,
        /// <summary>
        /// Hebrew.
        /// </summary>
        [EnumMember(Value = "iw-IL")]
        iw_IL,
        /// <summary>
        /// Hindi.
        /// </summary>
        [EnumMember(Value = "hi-IN")]
        hi_IN,
        /// <summary>
        /// Hungarian.
        /// </summary>
        [EnumMember(Value = "hu-HU")]
        hu_HU,
        /// <summary>
        /// Icelandic.
        /// </summary>
        [EnumMember(Value = "is-IS")]
        is_IS,
        /// <summary>
        /// Indonesian.
        /// </summary>
        [EnumMember(Value = "id-ID")]
        id_ID,
        /// <summary>
        /// Italian.
        /// </summary>
        [EnumMember(Value = "it-IT")]
        it_IT,
        /// <summary>
        /// Japanese.
        /// </summary>
        [EnumMember(Value = "ja-JP")]
        ja_JP,
        /// <summary>
        /// Kannada.
        /// </summary>
        [EnumMember(Value = "kn-IN")]
        kn_IN,

        /// <summary>
        /// Kazakh.
        /// </summary>
        [EnumMember(Value = "kk-KZ")]
        kk_KZ,
        /// <summary>
        /// Khmer.
        /// </summary>
        [EnumMember(Value = "km-KH")]
        km_KH,
        /// <summary>
        /// Korean.
        /// </summary>
        [EnumMember(Value = "ko-KR")]
        ko_KR,

        /// <summary>
        /// Kyrgyz.
        /// </summary>
        [EnumMember(Value = "ky-KG")]
        ky_KG,
        /// <summary>
        /// Lao.
        /// </summary>
        [EnumMember(Value = "lo-LA")]
        lo_LA,
        /// <summary>
        /// Latvian.
        /// </summary>
        [EnumMember(Value = "lv-LV")]
        lv_LV,
        /// <summary>
        /// Lithuanian.
        /// </summary>
        [EnumMember(Value = "lt-LT")]
        lt_LT,
        /// <summary>
        /// Macedonian.
        /// </summary>
        [EnumMember(Value = "mk-MK")]
        mk_MK,
        /// <summary>
        /// Malay (Malaysia).
        /// </summary>
        [EnumMember(Value = "ms-MY")]
        ms_MY,
        /// <summary>
        /// Malay.
        /// </summary>
        [EnumMember(Value = "ms-MS")]
        ms_MS,
        /// <summary>
        /// Malayalam.
        /// </summary>
        [EnumMember(Value = "ml-IN")]
        ml_IN,
        /// <summary>
        /// Marathi.
        /// </summary>
        [EnumMember(Value = "mr-IN")]
        mr_IN,
        /// <summary>
        /// Mongolian.
        /// </summary>
        [EnumMember(Value = "mn-MN")]
        mn_MN,
        /// <summary>
        /// Nepali.
        /// </summary>
        [EnumMember(Value = "ne-NP")]
        ne_NP,
        /// <summary>
        /// Norwegian.
        /// </summary>
        [EnumMember(Value = "no-NO")]
        no_NO,
        /// <summary>
        /// Persian.
        /// </summary>
        [EnumMember(Value = "fa-FA")]
        fa_FA,
        /// <summary>
        /// Persian.
        /// </summary>
        [EnumMember(Value = "fa-AE")]
        fa_AE,
        /// <summary>
        /// Persian.
        /// </summary>
        [EnumMember(Value = "fa-AF")]
        fa_AF,
        /// <summary>
        /// Persian.
        /// </summary>
        [EnumMember(Value = "fa-IR")]
        fa_IR,
        /// <summary>
        /// Polish.
        /// </summary>
        [EnumMember(Value = "pl-PL")]
        pl_PL,

        /// <summary>
        /// Portuguese (Brazil).
        /// </summary>
        [EnumMember(Value = "pt-BR")]
        pt_BR,
        /// <summary>
        /// Portuguese (Portugal).
        /// </summary>
        [EnumMember(Value = "pt-PT")]
        pt_PT,
        /// <summary>
        /// Punjabi.
        /// </summary>
        [EnumMember(Value = "pa-IN")]
        pa_IN,
        /// <summary>
        /// Romanian.
        /// </summary>
        [EnumMember(Value = "ro-RO")]
        ro_RO,
        /// <summary>
        /// Romansh.
        /// </summary>
        [EnumMember(Value = "rm-CH")]
        rm_CH,
        /// <summary>
        /// Russian.
        /// </summary>
        [EnumMember(Value = "ru-RU")]
        ru_RU,
        /// <summary>
        /// Serbian.
        /// </summary>
        [EnumMember(Value = "sr-RS")]
        sr_RS,
        /// <summary>
        /// Sinhala.
        /// </summary>
        [EnumMember(Value = "si-LK")]
        si_LK,
        /// <summary>
        /// Slovak.
        /// </summary>
        [EnumMember(Value = "sk-SK")]
        sk_SK,
        /// <summary>
        /// Slovenian.
        /// </summary>
        [EnumMember(Value = "sl-SI")]
        sl_SI,
        /// <summary>
        /// Spanish (Latin America).
        /// </summary>
        [EnumMember(Value = "es-419")]
        es_419,
        /// <summary>
        /// Spanish (Spain).
        /// </summary>
        [EnumMember(Value = "es-ES")]
        es_ES,
        /// <summary>
        /// Spanish (Mexico).
        /// </summary>
        [EnumMember(Value = "es-MX")]
        es_MX,
        /// <summary>
        /// Spanish (United States).
        /// </summary>
        [EnumMember(Value = "es-US")]
        es_US,
        /// <summary>
        /// Swahili.
        /// </summary>
        [EnumMember(Value = "sw-KE")]
        sw_KE,
        /// <summary>
        /// Swedish.
        /// </summary>
        [EnumMember(Value = "sv-SE")]
        sv_SE,
        /// <summary>
        /// Tamil.
        /// </summary>
        [EnumMember(Value = "ta-IN")]
        ta_IN,
        /// <summary>
        /// Telugu.
        /// </summary>
        [EnumMember(Value = "te-IN")]
        te_IN,
        /// <summary>
        /// Thai.
        /// </summary>
        [EnumMember(Value = "th-TH")]
        th_TH,
        /// <summary>
        /// Turkish.
        /// </summary>
        [EnumMember(Value = "tr-TR")]
        tr_TR,
        /// <summary>
        /// Ukrainian.
        /// </summary>
        [EnumMember(Value = "uk-UA")]
        uk_UA,
        /// <summary>
        /// Urdu.
        /// </summary>
        [EnumMember(Value = "ur-UZ")]
        ur_UZ,
        /// <summary>
        /// Vietnamese.
        /// </summary>
        [EnumMember(Value = "vi-VN")]
        vi_VN,
        /// <summary>
        /// Zulu.
        /// </summary>
        [EnumMember(Value = "zu-ZA")]
        zu_ZA,
    }
}
