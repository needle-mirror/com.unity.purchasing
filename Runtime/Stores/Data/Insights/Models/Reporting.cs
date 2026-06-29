namespace UnityEngine.Purchasing.Stores.Data.Insights.Models
{
    // Mirrors insights.common.v1alpha1.Reporting
    // (see Schemas~/insights/common/v1alpha1/reporting.proto).
    internal sealed class Reporting
    {
        public string Platform { get; set; } = "";
        public string AppBundleId { get; set; } = "";
    }
}
