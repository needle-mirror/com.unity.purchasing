using System;
using System.Collections.Generic;
using Uniject;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class ProductDetailsResponseConsolidator : IProductDetailsResponseConsolidator
    {
        const int k_RequiredNumberOfCallbacks = 2;
        int m_NumberReceivedCallbacks;
        readonly Action<IProductDetailsQueryResponse> m_OnProductDetailsResponseConsolidated;
        readonly IProductDetailsQueryResponse m_Responses = new ProductDetailsQueryResponse();
        readonly IUtil m_Util;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;

        internal ProductDetailsResponseConsolidator(IUtil util, ITelemetryDiagnostics telemetryDiagnostics,
            Action<IProductDetailsQueryResponse> onProductDetailsResponseConsolidated)
        {
            m_Util = util;
            m_OnProductDetailsResponseConsolidated = onProductDetailsResponseConsolidated;
            m_TelemetryDiagnostics = telemetryDiagnostics;
        }

        public void Consolidate(IGoogleBillingResult billingResult, IEnumerable<AndroidJavaObject> productDetails)
        {
            try
            {
                m_NumberReceivedCallbacks++;

                m_Responses.AddResponse(billingResult, productDetails);

                if (m_NumberReceivedCallbacks >= k_RequiredNumberOfCallbacks)
                {
                    m_OnProductDetailsResponseConsolidated(m_Responses);
                }
            }
            catch (Exception ex)
            {
                m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.SkuDetailsResponseConsolidatorError, ex);
            }
        }
    }
}
