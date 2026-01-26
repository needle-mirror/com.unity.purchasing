#nullable enable

using System;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    interface IFetchStorefrontUseCase
    {
        void FetchStorefront(Action<AppleStorefront> successCallback, Action<string> errorCallback);
    }
}
