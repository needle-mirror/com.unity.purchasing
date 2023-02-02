using System;
using System.Collections.Generic;
using Uniject;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class SkuDetailsResponseConsolidator : ISkuDetailsResponseConsolidator
    {
        const int k_RequiredNumberOfCallbacks = 2;
        int m_NumberReceivedCallbacks;
        readonly Action<ISkuDetailsQueryResponse> m_OnSkuDetailsResponseConsolidated;
        readonly ISkuDetailsQueryResponse m_Responses = new SkuDetailsQueryResponse();
        readonly IUtil m_Util;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;

        internal SkuDetailsResponseConsolidator(IUtil util, ITelemetryDiagnostics telemetryDiagnostics,
            Action<ISkuDetailsQueryResponse> onSkuDetailsResponseConsolidated)
        {
            m_Util = util;
            m_OnSkuDetailsResponseConsolidated = onSkuDetailsResponseConsolidated;
            m_TelemetryDiagnostics = telemetryDiagnostics;
        }

        public void Consolidate(IGoogleBillingResult billingResult, IEnumerable<AndroidJavaObject> skuDetails)
        {
            try
            {
                m_NumberReceivedCallbacks++;

                m_Responses.AddResponse(billingResult, skuDetails);

                if (m_NumberReceivedCallbacks >= k_RequiredNumberOfCallbacks)
                {
                    m_OnSkuDetailsResponseConsolidated(m_Responses);
                }
            }
            catch (Exception ex)
            {
                m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.SkuDetailsResponseConsolidatorError, ex);
            }
        }
    }
}
