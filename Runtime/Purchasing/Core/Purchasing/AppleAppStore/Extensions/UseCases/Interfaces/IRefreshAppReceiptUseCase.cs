using System;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    // TODO: IAP-3929
    interface IRefreshAppReceiptUseCase
    {
        void RefreshAppReceipt(Action<string> successCallback, Action<string> errorCallback);
        void SetRefreshAppReceipt(bool refreshAppReceipt);
    }
}
