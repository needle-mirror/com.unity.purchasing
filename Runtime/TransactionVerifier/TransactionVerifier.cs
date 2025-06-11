#nullable enable

using System;
using System.Threading.Tasks;
using Purchasing.TransactionVerifier;
using UnityEngine.Scripting;
using VerifyAppleTransactionRequestBody = UnityEngine.Purchasing.TransactionVerifier.Models.VerifyAppleTransactionRequest;
using VerifyAppleTransactionRequest = UnityEngine.Purchasing.TransactionVerifier.Apple.VerifyAppleTransactionRequest;
using VerifyGoogleTransactionRequestBody = UnityEngine.Purchasing.TransactionVerifier.Models.VerifyGoogleTransactionRequest;
using VerifyGoogleTransactionRequest = UnityEngine.Purchasing.TransactionVerifier.Google.VerifyGoogleTransactionRequest;

#if IAP_UNITY_AUTH_ENABLED
#else
#error Unity Authentication (com.unity.services.authentication) is required when validating transactions (when IAP_TX_VERIFIER_ENABLED is defined).
#endif

namespace UnityEngine.Purchasing.TransactionVerifier
{
    public enum Store
    {
        Apple,
        Google,
        Unknown
    }

    [Preserve]
    public class TransactionVerifier : ITransactionVerifier
    {
        readonly Store m_Store;
        readonly string? m_ProjectId;
        readonly string? m_EnvironmentId;
        readonly ITransactionVerifierService m_Service;

        public TransactionVerifier(string storeName, string? projectId, string? environmentId)
        {
            m_Store = storeName switch
            {
                "GooglePlay" => Store.Google,
                "AppleAppStore" => Store.Apple,
                _ => Store.Unknown
            };

            m_ProjectId = projectId;
            m_EnvironmentId = environmentId;

            if (m_ProjectId == null || m_EnvironmentId == null)
            {
                Debug.unityLogger.LogError("TransactionVerification", "Unknown project or environment id. Ensure core (UnityServices.InitializeAsync) and authentication (AnalyticsService.Instance.SignIn) are initialized.");
            }

            m_Service = TransactionVerifierService.Instance;
        }

        public async Task<VerificationResponse> VerifyPendingOrder(string transactionRepresentation)
        {
            switch (m_Store)
            {
                case Store.Apple:
                    // Create Apple request
                    var appleBody = new VerifyAppleTransactionRequestBody(transactionRepresentation);
                    var appleRequest = new VerifyAppleTransactionRequest(m_ProjectId, m_EnvironmentId, appleBody);

                    // Send Request
                    var appleResponse = await m_Service.AppleApi.VerifyAppleTransactionAsync(appleRequest);

                    // Convert apple response to generic
                    if (appleResponse.Status == 200)
                    {
                        return new VerificationResponse(appleResponse.Result);
                    }

                    throw new Exception($"Transaction verification failed with status {appleResponse.Status}");


                case Store.Google:
                    // Create Google request
                    var googleBody = new VerifyGoogleTransactionRequestBody(transactionRepresentation);
                    var googleRequest = new VerifyGoogleTransactionRequest(m_ProjectId, m_EnvironmentId, googleBody);

                    // Send request
                    var googleResponse = await m_Service.GoogleApi.VerifyGoogleTransactionAsync(googleRequest);

                    // Convert google response to generic
                    if (googleResponse.Status == 200)
                    {
                        return new VerificationResponse(googleResponse.Result);
                    }

                    throw new Exception($"Transaction verification failed with status {googleResponse.Status}");

                default:
                    throw new Exception("Transaction verification failed due to unknown store type.");
            }
        }
    }
}
