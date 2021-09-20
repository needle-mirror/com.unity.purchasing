using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Samples.Purchasing.AppleAppStore.UpgradeDowngradeSubscription
{
    /// <summary>
    /// A subscription group is a list of subscriptions where a user can only be subscribed to a single subscription in the group at one time.
    /// The subscriptions in the group are ordered by their tier, meaning a user can upgrade or downgrade from one subscription to another in the group.
    /// </summary>
    public class SubscriptionGroup
    {
        IStoreController m_StoreController;
        IExtensionProvider m_ExtensionsProvider;

        string[] m_SubscriptionIds;

        public SubscriptionGroup(IStoreController storeController, IExtensionProvider extensionsProvider, params string[] subscriptionIds)
        {
            m_StoreController = storeController;
            m_ExtensionsProvider = extensionsProvider;
            m_SubscriptionIds = subscriptionIds;
        }

        public void BuySubscription(string newSubscriptionId)
        {
            m_StoreController.InitiatePurchase(newSubscriptionId);
        }

        public string CurrentSubscriptionId()
        {
            return m_SubscriptionIds.FirstOrDefault(IsSubscribedTo);
        }

        bool IsSubscribedTo(string subscriptionId)
        {
            var subscriptionProduct = m_StoreController.products.WithID(subscriptionId);
            return IsSubscribedTo(subscriptionProduct);
        }

        static bool IsSubscribedTo(Product subscription)
        {
            // If the product doesn't have a receipt, then it wasn't purchased and the user is therefore not subscribed.
            if (subscription?.receipt == null)
            {
                return false;
            }

            var subscriptionManager = new SubscriptionManager(subscription, null);
            var subscriptionInfo = subscriptionManager.getSubscriptionInfo();
            return subscriptionInfo.isSubscribed() == Result.True;
        }
    }
}
