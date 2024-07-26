using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
namespace UnityEngine.Purchasing
{
    class ProductDetailsQueryResponse : IProductDetailsQueryResponse
    {
        readonly ConcurrentBag<(IGoogleBillingResult, IEnumerable<AndroidJavaObject>)> m_Responses = new ConcurrentBag<(IGoogleBillingResult, IEnumerable<AndroidJavaObject>)>();

        ~ProductDetailsQueryResponse()
        {
#if UNITY_2021_2_OR_NEWER
            foreach (var response in m_Responses)
            {
                var objList = response.Item2;
                if (objList == null)
                {
                    continue;
                }

                foreach (var obj in objList)
                {
                    obj?.Dispose();
                }
            }
#endif
        }

        public void AddResponse(IGoogleBillingResult billingResult, IEnumerable<AndroidJavaObject> productDetails)
        {
            m_Responses.Add((billingResult, productDetails.Select(product => product).ToList()));
        }

        public List<AndroidJavaObject> ProductDetails()
        {
            return m_Responses.Where(response => response.Item1.responseCode == GoogleBillingResponseCode.Ok)
                .SelectMany(response => response.Item2).ToList();
        }

        public bool IsRecoverable()
        {
            return m_Responses.Select(response => response.Item1).Any(IsRecoverable);
        }

        public GoogleBillingResponseCode GetRecoverableBillingResponseCode()
        {
            if (m_Responses.Select(response => response.Item1).Any(IsServiceUnavailable))
            {
                return GoogleBillingResponseCode.ServiceUnavailable;
            }
            if (m_Responses.Select(response => response.Item1).Any(IsDeveloperError))
            {
                return GoogleBillingResponseCode.DeveloperError;
            }

            return m_Responses.FirstOrDefault().Item1.responseCode;
        }

        static bool IsRecoverable(IGoogleBillingResult billingResult)
        {
            return IsServiceUnavailable(billingResult) || IsDeveloperError(billingResult);
        }

        static bool IsServiceUnavailable(IGoogleBillingResult billingResult)
        {
            return billingResult.responseCode == GoogleBillingResponseCode.ServiceUnavailable;
        }

        static bool IsDeveloperError(IGoogleBillingResult billingResult)
        {
            return billingResult.responseCode == GoogleBillingResponseCode.DeveloperError;
        }
    }
}
