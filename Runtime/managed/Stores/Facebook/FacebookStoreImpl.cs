using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Uniject;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using AOT;

namespace UnityEngine.Purchasing
{
    // internal class FacebookStoreImpl : NativeJSONStore, IFacebookExtensions, IFacebookConfiguration
    internal class FacebookStoreImpl : JSONStore
    {
        private INativeFacebookStore m_Native;

        private static IUtil util;
        private static FacebookStoreImpl instance;

        public FacebookStoreImpl(IUtil util) {
            FacebookStoreImpl.util = util;
            instance = this;
        }

        public void SetNativeStore(INativeFacebookStore facebook) {
            base.SetNativeStore (facebook);
            this.m_Native = facebook;
            facebook.Init();
            facebook.SetUnityPurchasingCallback (MessageCallback);
        }

        public bool consumeItem(string item) {
            return m_Native.ConsumeItem(item);
        }

        [MonoPInvokeCallback(typeof(UnityPurchasingCallback))]
        private static void MessageCallback(string subject, string payload, string receipt, string transactionId) {
            util.RunOnMainThread(() => {
                instance.ProcessMessage (subject, payload, receipt, transactionId);
            });
        }

        private void ProcessMessage(string subject, string payload, string receipt, string transactionId) {
            switch (subject) {
            case "OnSetupFailed":
                OnSetupFailed (payload);
                break;
            case "OnProductsRetrieved":
                OnProductsRetrieved (payload);
                break;
            case "OnPurchaseSucceeded":
                OnPurchaseSucceeded (payload, receipt, transactionId);
                break;
            case "OnPurchaseFailed":
                OnPurchaseFailed (payload);
                break;
            case "SendPurchasingEvent":
                // SendFBPurchasingEvent (payload, receipt, transactionId);
                // add new event hook when available
                break;
            }
        }


    }
}
