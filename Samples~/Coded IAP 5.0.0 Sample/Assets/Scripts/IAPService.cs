using System;
using Unity.Services.Core;
using Unity.Services.Core.Environments;

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

                onSuccess();
            }
            catch (Exception exception)
            {
                onError(exception.Message);
            }
        }
    }
}
