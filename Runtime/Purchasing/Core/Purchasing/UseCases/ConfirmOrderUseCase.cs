#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    internal class ConfirmOrderUseCase : IConfirmOrderUseCase, IStorePurchaseConfirmCallback
    {
        readonly IStore m_Store;
        readonly List<ConfirmOrderRequest> m_ConfirmationRequests = new List<ConfirmOrderRequest>();

        [Preserve]
        internal ConfirmOrderUseCase(IStore storeResponsible)
        {
            m_Store = storeResponsible;
            m_Store.SetPurchaseConfirmCallback(this);
        }

        public void ConfirmOrder(PendingOrder order, Action<PendingOrder, ConfirmedOrder> confirmationSuccessAction, Action<PendingOrder, FailedOrder> confirmationFailedAction)
        {
            if (order == null)
            {
                throw new ConfirmOrderException("Invalid order requested for confirmation. No callbacks will be sent for this call. Please pass a valid `PendingOrder` object.");
            }

            if (FindExistingConfirmationRequest(order))
            {
                throw new ConfirmOrderException("Duplicate order requested for confirmation. No callbacks will be sent for this call. Please refrain from passing the same `PendingOrder` multiple times.");
            }
            else
            {
                AddAndSendFinishTransactionRequest(order, confirmationSuccessAction, confirmationFailedAction);
            }
        }

        bool FindExistingConfirmationRequest(PendingOrder orderToCheckFor)
        {
            return m_ConfirmationRequests.Exists(request => request.OrderToConfirm == orderToCheckFor);
        }

        void AddAndSendFinishTransactionRequest(PendingOrder order, Action<PendingOrder, ConfirmedOrder> confirmationSuccessAction, Action<PendingOrder, FailedOrder> confirmationFailedAction)
        {
            m_ConfirmationRequests.Add(new ConfirmOrderRequest(order, confirmationSuccessAction, confirmationFailedAction));
            m_Store.FinishTransaction(order);
        }

        public void OnConfirmOrderSucceeded(string transactionId)
        {
            var matchingRequest = GetMatchingRequest(transactionId);

            if (matchingRequest != null)
            {
                var confirmedOrder = new ConfirmedOrder(matchingRequest.OrderToConfirm.CartOrdered,
                    matchingRequest.OrderToConfirm.Info);
                matchingRequest.SuccessAction?.Invoke(matchingRequest.OrderToConfirm, confirmedOrder);

                m_ConfirmationRequests.Remove(matchingRequest);
            }
            else
            {
                throw new ConfirmOrderException($"Cannot find matching confirmation request for transaction id: {transactionId}. The List of orders may have become corrupt. No callbacks will be sent for this call.");
            }
        }

        ConfirmOrderRequest? GetMatchingRequest(string transactionIdentifier)
        {
            return m_ConfirmationRequests.FirstOrDefault(request => request.OrderToConfirm.Info.TransactionID == transactionIdentifier);
        }

        public void OnConfirmOrderFailed(FailedOrder failedOrder, string transactionId)
        {
            var matchingRequest = GetMatchingRequest(transactionId);

            if (matchingRequest != null)
            {
                matchingRequest.FailureAction?.Invoke(matchingRequest.OrderToConfirm, failedOrder);

                m_ConfirmationRequests.Remove(matchingRequest);
            }
            else
            {
                throw new ConfirmOrderException($"Cannot find matching confirmation request for transaction id: {transactionId}. The List of orders may have become corrupt. No callbacks will be sent for this call.");
            }
        }
    }
}
