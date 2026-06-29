using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using AuthenticationException = System.Security.Authentication.AuthenticationException;

namespace Samples.Purchasing.IAP5.Demo
{
    static class IAPService
    {
        const string k_Environment = "production";

        public static async void Initialize(Action onSuccess, Action<string> onError)
        {
            try
            {
                var options = new InitializationOptions()
                    .SetEnvironmentName(k_Environment);

                await UnityServices.InitializeAsync(options);

                await SignInAnonymouslyAsync();

                onSuccess();
            }
            catch (Exception exception)
            {
                onError(exception.Message);
            }
        }

        private static async Task SignInAnonymouslyAsync()
        {

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");

            // Shows how to get the playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        }
    }
}
