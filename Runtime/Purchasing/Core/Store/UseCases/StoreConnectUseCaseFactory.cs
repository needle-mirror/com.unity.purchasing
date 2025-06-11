#nullable enable

using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    //This class might need to be changed/removed to conform to a future design, but the CreateRetryPolicy logic should be kept.
    class StoreConnectUseCaseFactory
    {
        public IStoreConnectUseCase CreateUseCase(IStoreWrapper storeWrapper, IRetryService retryService)
        {
            var retryPolicy = CreateRetryPolicy(storeWrapper.name);
            return new StoreConnectUseCase(storeWrapper.instance, retryService, retryPolicy);
        }

        static IRetryPolicy CreateRetryPolicy(string storeName)
        {
            if (storeName == GooglePlay.Name)
            {
                return new MaximumNumberOfAttemptsRetryPolicy(3);
            }

            return new NoRetriesPolicy();
        }
    }
}
