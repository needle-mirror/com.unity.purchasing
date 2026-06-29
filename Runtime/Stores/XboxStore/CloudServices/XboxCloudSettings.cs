using UnityEngine;

namespace UnityEngine.Purchasing
{
    public class XboxCloudSettings : ScriptableObject
    {
        public const string k_AssetName = "XboxCloudSettings";
#if DEBUG
        // A test hook that skips the purchase. Then purchase validation will fail.
        // If there's no purchase validation, purchase will succeed
        public static bool TestSkipPurchase { get; set; }
#endif

        [SerializeField]
        bool m_EnablePurchaseValidation;
        [SerializeField]
        string m_CloudCodeModuleName;
        [SerializeField]
        string m_ServiceTicketFunctionName;
        [SerializeField]
        string m_ValidatePurchaseFunctionName;

        public bool EnablePurchaseValidation => m_EnablePurchaseValidation;
        public string CloudCodeModuleName => m_CloudCodeModuleName;
        public string ServiceTicketFunctionName => m_ServiceTicketFunctionName;
        public string ValidatePurchaseFunctionName => m_ValidatePurchaseFunctionName;
    }
}
