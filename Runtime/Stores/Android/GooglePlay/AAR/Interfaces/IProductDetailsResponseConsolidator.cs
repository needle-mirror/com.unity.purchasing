using System.Collections.Generic;
using UnityEngine.Purchasing.Models;
namespace UnityEngine.Purchasing.Interfaces
{
    interface IProductDetailsResponseConsolidator
    {
        void Consolidate(IGoogleBillingResult billingResult, IEnumerable<AndroidJavaObject> productDetails);
    }
}
