namespace UnityEngine.Purchasing.Models
{
    public interface IGoogleBillingResult
    {
        GoogleBillingResponseCode responseCode
        {
            get;
        }

        string debugMessage
        {
            get;
        }
    }
}
