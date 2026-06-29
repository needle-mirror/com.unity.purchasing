#nullable enable

using System;
using System.Threading.Tasks;
using Stores.Android.GooglePlay.AAR.Models;
using Uniject;
using UnityEngine.Purchasing.GoogleBilling.Interfaces;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.GoogleBilling.Models
{
    /// <summary>
    /// A billing client for use with Google's External Billing Programs.
    /// This class has the necessary methods for compliance with [external content links](https://developer.android.com/google/play/billing/externalcontentlinks).
    /// </summary>
    public class ExternalBillingProgramClient
    {
        readonly IExternalBillingProgramClientInternal billingClient;
        readonly IUtil m_Util;

        /// <summary>
        /// Creates a new <see cref="ExternalBillingProgramClient"/> that will auto-detect the
        /// available billing program when <see cref="IsBillingProgramAvailableAsync"/> is called.
        /// </summary>
        public ExternalBillingProgramClient()
        {
            var factory = BillingClientFactory.Instance();
            m_Util = factory.m_Util;
            billingClient = factory.CreateExternalBillingProgramClient();
        }

        // This method is used for internal testing
        internal ExternalBillingProgramClient(IBillingClientFactory factory, IUtil util)
        {
            m_Util = util;
            billingClient = factory.CreateExternalBillingProgramClient();
        }

        // This method is used for internal testing
        internal ExternalBillingProgramClient(IBillingClientFactory factory, IUtil util, BillingProgram billingProgram)
        {
            m_Util = util;
            billingClient = factory.CreateExternalBillingProgramClient(billingProgram);
        }

        /// <summary>
        /// Starts up billing client setup process asynchronously.
        /// </summary>
        /// <param name="onConnected">A callback that will be triggered upon successfully connecting to the client.</param>
        /// <param name="onDisconnected">A callback that will be triggered when the client disconnects.</param>
        public void StartConnection(Action? onConnected = null, Action<GoogleBillingResponseCode>? onDisconnected = null)
        {
            StartConnectionAsync(onConnected, onDisconnected);
        }

        async void StartConnectionAsync(Action? onConnected, Action<GoogleBillingResponseCode>? onDisconnected)
        {
            try
            {
                await TryConnect(billingClient, onConnected, onDisconnected);
            }
            catch (Exception)
            {
                onDisconnected?.Invoke(GoogleBillingResponseCode.FatalError);
            }
        }

        internal virtual IBillingClientStateListener CreateStateListener()
        {
            return new BillingClientStateListener(m_Util);
        }

        /// <summary>
        /// Closes the connection and releases all held resources such as service connections.
        /// </summary>
        public void EndConnection()
        {
            billingClient.EndConnection();
        }

        /// <summary>
        /// Checks if the client is currently connected to the service, so that requests to other methods will succeed.
        /// </summary>
        /// <returns>True if the client is currently connected to the service, false otherwise.</returns>
        public bool IsReady()
        {
            return billingClient.IsReady();
        }

        /// <summary>
        /// Get the current billing client connection state.
        /// </summary>
        /// <returns>A <see cref="GoogleBillingConnectionState"/>.</returns>
        public GoogleBillingConnectionState GetConnectionState()
        {
            return billingClient.GetConnectionState();
        }

        /// <summary>
        /// This function interfaces with the BillingClient's IsBillingProgramAvailableAsync method.
        /// This returns if the external content link program is available for the current user.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient#isBillingProgramAvailableAsync(int,com.android.billingclient.api.BillingProgramAvailabilityListener)">See more</a>
        /// </summary>
        public Task<GoogleBillingResponseCode> IsBillingProgramAvailableAsync()
        {
            return CheckAvailability(billingClient);
        }

        /// <summary>
        /// Initiates linking out of the app for an external offer or app install.
        /// </summary>
        /// <param name="externalLinkUrl">The link to the external website where the digital content or app download is offered.
        /// For app downloads, this link must be registered and approved in the Play Developer Console.</param>
        /// <param name="linkType">The link type for the external offer as defined in <see cref="LinkType"/></param>
        /// <param name="launchMode">The launch mode for the external offer as defined in <see cref="LaunchMode"/></param>
        /// <returns>A <see cref="GoogleBillingResponseCode"/> representing the response to attempting to launch the link.</returns>
        public Task<GoogleBillingResponseCode> LaunchExternalLink(
            string externalLinkUrl,
            LinkType linkType,
            LaunchMode launchMode
        )
        {
            var taskCompletion = new TaskCompletionSource<GoogleBillingResponseCode>();

            billingClient.LaunchExternalLink(
                externalLinkUrl,
                (googleBillingResult) =>
                {
                    taskCompletion.TrySetResult(googleBillingResult.responseCode);
                },
                linkType,
                launchMode
            );

            return taskCompletion.Task;
        }

        /// <summary>
        /// This function interfaces with the BillingClient's createBillingProgramReportingDetailsAsync method.
        /// This returns the BillingProgramReportingDetails which contains the external transaction token when the response is successful or empty when unsuccessful.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient#createBillingProgramReportingDetailsAsync(com.android.billingclient.api.BillingProgramReportingDetailsParams,com.android.billingclient.api.BillingProgramReportingDetailsListener)">See more</a>
        /// </summary>
        public Task<BillingProgramReportingDetails> CreateBillingProgramReportingDetailsAsync()
        {
            var taskCompletion = new TaskCompletionSource<BillingProgramReportingDetails>();

            billingClient.CreateBillingProgramReportingDetailsAsync(
                (googleBillingResult, externalTransactionToken) =>
                {
                    var billingProgramReportingDetails = new BillingProgramReportingDetails
                    {
                        responseCode = googleBillingResult.responseCode,
                        externalTransactionToken = externalTransactionToken
                    };
                    taskCompletion.TrySetResult(billingProgramReportingDetails);
                }
            );

            return taskCompletion.Task;
        }

        Task<bool> TryConnect(
            IExternalBillingProgramClientInternal client,
            Action? onConnected = null,
            Action<GoogleBillingResponseCode>? onDisconnected = null)
        {
            var tcs = new TaskCompletionSource<bool>();
            var listener = CreateStateListener();
            listener.RegisterOnConnected(() =>
            {
                tcs.TrySetResult(true);
                onConnected?.Invoke();
            });
            listener.RegisterOnDisconnected((code) =>
            {
                tcs.TrySetResult(false);
                onDisconnected?.Invoke(code);
            });
            client.StartConnection(listener);
            return tcs.Task;
        }

        static Task<GoogleBillingResponseCode> CheckAvailability(IExternalBillingProgramClientInternal client)
        {
            var tcs = new TaskCompletionSource<GoogleBillingResponseCode>();
            client.IsBillingProgramAvailableAsync(result => tcs.TrySetResult(result.responseCode));
            return tcs.Task;
        }
    }
}
