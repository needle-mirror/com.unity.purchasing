using System.Collections.Generic;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IProductDetailsQueryResponse
    {
        void AddResponse(IGoogleBillingResult billingResult, IEnumerable<AndroidJavaObject> productDetails);
        List<AndroidJavaObject> ProductDetails();
        bool IsRecoverable();
        IGoogleBillingResult GetGoogleBillingResult();
    }
}
