#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class ConfirmOrderUseCase : IConfirmOrderUseCase, IStorePurchaseConfirmCallback
    {
        readonly IStore m_Store;
        readonly List<ConfirmOrderRequest> m_ConfirmationRequests = new();

        [Preserve]
        internal ConfirmOrderUseCase(IStore storeResponsible)
        {
            m_Store = storeResponsible;
            m_Store.SetPurchaseConfirmCallback(this);
        }

        public void ConfirmOrder(PendingOrder order, Action<PendingOrder, Order> confirmationAction)
        {
            if (FindExistingConfirmationRequest(order))
            {
                confirmationAction(order,
                    new FailedOrder(
                        order,
                        PurchaseFailureReason.ExistingPurchasePending,
                        "Duplicate order requested for confirmation. Please refrain from passing the same `PendingOrder` multiple times."));
                return;
            }

            AddAndSendFinishTransactionRequest(order, confirmationAction);
        }

        bool FindExistingConfirmationRequest(PendingOrder orderToCheckFor)
        {
            return m_ConfirmationRequests.Exists(request => request.OrderToConfirm == orderToCheckFor);
        }

        void AddAndSendFinishTransactionRequest(PendingOrder order, Action<PendingOrder, Order> confirmationAction)
        {
            m_ConfirmationRequests.Add(new ConfirmOrderRequest(order, confirmationAction));
            m_Store.FinishTransaction(order);
        }

        public void OnConfirmOrderSucceeded(string transactionId)
        {
            var matchingRequest = GetMatchingRequest(transactionId);

            if (matchingRequest != null)
            {
                var confirmedOrder = new ConfirmedOrder(matchingRequest.OrderToConfirm.CartOrdered,
                    matchingRequest.OrderToConfirm.Info);
                matchingRequest.Action?.Invoke(matchingRequest.OrderToConfirm, confirmedOrder);
                m_ConfirmationRequests.Remove(matchingRequest);
            }
            else
            {
                Debug.unityLogger.LogIAPError($"Cannot find matching confirmation request for transaction id: {transactionId}. The List of orders may have become corrupt. No callbacks will be sent for this call.");
            }
        }

        public void OnConfirmOrderFailed(FailedOrder failedOrder)
        {
            var matchingRequest = GetMatchingRequest(failedOrder.Info.TransactionID);

            if (matchingRequest != null)
            {
                if (failedOrder.Info.Receipt == string.Empty)
                {
                    failedOrder = new FailedOrder(matchingRequest.OrderToConfirm, failedOrder.FailureReason, failedOrder.Details);
                }

                matchingRequest.Action?.Invoke(matchingRequest.OrderToConfirm, failedOrder);
                m_ConfirmationRequests.Remove(matchingRequest);
            }
            else
            {
                Debug.unityLogger.LogIAPError($"Cannot find matching confirmation request for transaction id: {failedOrder.Info.TransactionID}. The List of orders may have become corrupt. No callbacks will be sent for this call.");
            }
        }

        ConfirmOrderRequest? GetMatchingRequest(string transactionIdentifier)
        {
            return m_ConfirmationRequests.FirstOrDefault(request => request.OrderToConfirm.Info.TransactionID == transactionIdentifier);
        }
    }
}
