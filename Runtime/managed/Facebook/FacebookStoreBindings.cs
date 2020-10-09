using System;
using System.Collections.Generic;
using Facebook.Unity;
using UnityEngine.Purchasing;

#if !UNITY_EDITOR
namespace UnityEngine.Purchasing
{
    // Not sure if we're going to keep these or not...
    //
    internal class fbMetadata
    {
        public fbMetadata(string priceString, string title, string description, string currencyCode, decimal centsPrice)
        {
            localizedPriceString = priceString;
            localizedTitle = title;
            localizedDescription = description;
            isoCurrencyCode = currencyCode;
            localizedPrice = centsPrice / 100m;
        }
        public string localizedPriceString { get; internal set; }
        public string localizedTitle { get; internal set; }
        public string localizedDescription { get; internal set; }
        public string isoCurrencyCode { get; internal set; }
        public decimal localizedPrice { get; internal set; }

        public override string ToString()
        {
            return localizedTitle + " @ " + localizedPrice + " [" + isoCurrencyCode + "]";
        }
    }

    internal class fbProduct
    {
        public fbProduct(string id)
        {
            storeSpecificId = id;
        }
        public fbProduct(string id, string priceString, string title, string description, string currencyCode, decimal centsPrice)
        {
            storeSpecificId = id;
            metadata = new fbMetadata(priceString, title, description, currencyCode, centsPrice);
        }
        public string storeSpecificId { get; internal set; }
        public fbMetadata metadata { get; internal set; }
    }


    //  OK, these are not really bindings, they're the actual implementation. Need to refactor a bit once this is
    //  all working.
    internal class FacebookStoreBindings : INativeFacebookStore
    {
        UnityPurchasingCallback m_callback;
        bool m_fbReady = false;
        string m_prodJSON;
        string m_purcItem;
        int m_prodErrs = 0;

        // This keeps a prodID / type xref for us
        Dictionary<string,string> m_iaProd = new Dictionary<string, string>();

        // This stores a copy of the returned FB product metadata
        // (beause the purchase history doesn't include it)
        Dictionary<string,fbProduct> m_fbProd = new Dictionary<string,fbProduct>();

        //  Keep a list for all items (available and purchased)
        List<object> m_itemList = new List<object>();

        bool m_purchasesRetrieved = false;

        // to simplify testing of pagination support. Normal FB value is 25
        const int k_FacebookPageSize = 25;

        // Check to see if Facebook store is really available
        public bool Check()
        {
            // This should check to see if FB is actually present. However, the editor script
            // may actually take care of that to some extent here...
            return true;
        }


        // Potential refactor: just push this into RetrieveProducts() and mangle accordingly
        //
        public void Init ()
        {
            if(!FB.IsInitialized)
            {
                Console.WriteLine("UnityIAP FB: calling FB.Init()");
                FB.Init(InitComplete);
            } else {
                Console.WriteLine("UnityIAP FB: Facebook already Initialized, checking...");
                InitComplete();
            }
        }

        public void SetUnityPurchasingCallback (UnityPurchasingCallback AsyncCallback)
        {
            m_callback = AsyncCallback;
        }

        public void RetrieveProducts (string json)
        {
            // Due to the way IAP initializes we need to cache the RetrieveProducts
            // JSON coming from the first attempt for subsequent call from our init callbacks
            // We could also refactor this to be two separate functions
            //
            if(!m_fbReady)
            {
                if(!string.IsNullOrEmpty(json))
                {
                    m_prodJSON = json;
                }
                return;
            }

            if(string.IsNullOrEmpty(json))
            {
                if(string.IsNullOrEmpty(m_prodJSON))
                {
                    // Made it here with an empty JSON and no saved copy. That should
                    // only happen if we're still in the init path via InstantiateStore
                    // so just...
                    return;
                }
                else
                {
                    json = m_prodJSON;
                }
            }

            PagedGraphRequest("products", ProductListCallback, k_FacebookPageSize, null);
        }


        private void PagedGraphRequest(string request, FacebookDelegate<IGraphResult> callback, int pageSize, string nextPage)
        {
            if(!String.IsNullOrEmpty(request))
            {
                var tokenString = AccessToken.CurrentAccessToken.TokenString;

                var requestParams = new Dictionary<string, string>
                {
                    // {"product_ids", itemList},
                    {"access_token", tokenString}
                };
                if(pageSize > 0)
                {
                    requestParams.Add("limit", pageSize.ToString());
                }
                if(!String.IsNullOrEmpty(nextPage))
                {
                    requestParams.Add("after", nextPage);
                }

                //  Build the request field
                string requestUrl = "/" + FB.AppId + "/" + request;
                // Console.WriteLine("UnityIAP FB: request = {0}", requestUrl);

                FB.API(requestUrl, HttpMethod.GET, callback, requestParams);
            }
            else
            {
                Console.WriteLine("UnityIAP FB: PagedGraphRequest error");
            }
        }

        public void RetrievePurchases()
        {
            // We should only retrieve purchases _once_ (at init)
            if (m_purchasesRetrieved == false)
            {
                Console.WriteLine("UnityIAP FB: RetrievePurchases()");
                PagedGraphRequest("purchases", PurchaseListCallback, k_FacebookPageSize, null);
            }
            else
            {
                // Just pass the products on to Unity for subsequent calls
                string jsonData = MiniJson.JsonEncode(m_itemList);
                m_callback("OnProductsRetrieved", jsonData, "", "");
            }
        }

        public void Purchase (string productJSON, string developerPayload)
        {
            // Console.WriteLine("UnityIAP FB: Purchase({0})", productJSON);

            var dic = (Dictionary<string, object>) MiniJson.JsonDecode(productJSON);
            object obj;
            string myID;
            dic.TryGetValue("storeSpecificId", out obj);
            myID = (string)obj;

            Console.WriteLine("UnityIAP FB: Purchase, Product ID {0}", myID);

            // Purchase item via FB simplified IAP API
            m_purcItem = myID;  //  Save for later
            FB.Canvas.PayWithProductId(myID, callback: PurchaseCallback);

        }

        public void FinishTransaction (string productJSON, string transactionID)
        {
            Console.WriteLine("UnityIAP FB: FinishTransaction, Transaction ID {0}", transactionID);
            var dic = (Dictionary<string, object>) MiniJson.JsonDecode(productJSON);
            object obj;
            dic.TryGetValue("type", out obj);

            if("Consumable" == (string)obj)
            {
                if(!ConsumeItem(transactionID))
                {
                    Console.WriteLine("UnityIAP FB: !!! consume FAILED");
                    // is there anything we need to do in the event of a consume failure?
                }
            }
        }

        public bool ConsumeItem (string purchaseToken)
        {
            Console.WriteLine("UnityIAP FB: ConsumeItem()");
            string pRequest = "/" + purchaseToken + "/consume";
            var pList = new Dictionary<string, string>
            {
                {"access_token", AccessToken.CurrentAccessToken.TokenString}
            };

            // Facebook Graph API Call to Consume Purchase
            FB.API(pRequest, HttpMethod.POST, HandleResult, pList);

            // This could cause a problem if the consume graph request fails
            return true;
        }

        //  Callbacks and such
        //
        protected void HandleResult(IResult result)
        {
            Console.WriteLine("UnityIAP FB: HandleResult() RawResult " + result.RawResult);
        }

        private void InitComplete()
        {
            if(FB.IsInitialized)
            {
                Console.WriteLine("UnityIAP FB: Init OK");
                Console.WriteLine("UnityIAP FB: AppId `{0}`", FB.AppId);
            } else {
                Console.WriteLine("UnityIAP FB: Init ERROR");
                m_callback("OnSetupFailed", "PurchasingUnavailable", "", "");
                return;
            }

            if(FB.IsLoggedIn)
            {
                Console.WriteLine("UnityIAP FB: already logged in");

                // 2016-10 Should we force a refresh of some kind here to avoid failures
                //
                var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
                Console.WriteLine("UnityIAP FB: token UserId {0}", aToken.UserId);
                Console.WriteLine("UnityIAP FB: token ExpirationTime {0}", aToken.ExpirationTime);
                // Console.WriteLine("UnityIAP FB: token `{0}` {1}", aToken.TokenString, aToken);

                m_fbReady = true;
                RetrieveProducts("");
            } else {
                Console.WriteLine("UnityIAP FB: not logged in, calling LogInWithReadPermissions()");
                var perms = new List<string>(){"public_profile", "email", "user_friends"};
                FB.LogInWithReadPermissions(perms, AuthCallback);
            }
            return;
        }

        private void AuthCallback (ILoginResult result)
        {
            Console.WriteLine("UnityIAP FB: AuthCallback()");
            if (FB.IsLoggedIn)
            {
                // AccessToken class will have session details
                var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
                Console.WriteLine("UnityIAP FB: token UserId {0}", aToken.UserId);
                Console.WriteLine("UnityIAP FB: token ExpirationTime {0}", aToken.ExpirationTime);
                // Console.WriteLine("UnityIAP FB: token `{0}` {1}", aToken.TokenString, aToken);

                // Kick-off the rest of the Unity IAP Initialization
                //
                m_fbReady = true;
                RetrieveProducts("");
            } else {
                Console.WriteLine("UnityIAP FB: User cancelled login");
                m_callback("OnSetupFailed", "PurchasingUnavailable", "", "");
            }
        }

        private void ProductListCallback(IGraphResult result)
        {
            if(result == null)
            {
                // Not sure if this is really a possibility or not
                Console.WriteLine("UnityIAP FB: ProductListCallback() result is null");
                // No longer calling this a failed case (with paging enabled)
                // m_callback("OnSetupFailed", "NoProductsAvailable", "", "");
                return;
            }

            // Console.WriteLine("UnityIAP FB: ProductListCallback RawResult `{0}`", result.RawResult);

            var dic = (Dictionary<string, object>) MiniJson.JsonDecode(result.RawResult);

            if(!String.IsNullOrEmpty(result.Error))
            {
                Console.WriteLine("UnityIAP FB: ProductListCallback Error `{0}`", result.Error);

                // do we want to have a count-down / retry mechanism with an extra login attempt here?
                // we can certainly cycle through the process once again easily enough

                if((m_prodErrs == 0)&&(m_itemList.Count == 0))
                {
                    Console.WriteLine("UnityIAP FB: attempting LogInWithReadPermissions()");
                    var perms = new List<string>(){"public_profile", "email", "user_friends"};
                    m_prodErrs++;
                    FB.LogInWithReadPermissions(perms, AuthCallback);
                    return;
                }
                else
                {
                    // 20170719 this is no longer guaranteed to be a failure...
                    if(m_itemList.Count == 0)
                    {
                        m_callback("OnSetupFailed", "NoProductsAvailable", "", "");
                        return;
                    }
                    else
                    {
                        // go ahead with purchases
                        RetrievePurchases();
                        return;
                    }
                }
            }

            object myData;
            dic.TryGetValue("data", out myData);
            if(myData == null)
            {
                Console.WriteLine("UnityIAP FB: ProductListCallback() RawResult contains no \"data\" ");
                m_callback("OnSetupFailed", "NoProductsAvailable", "", "");
                return;
            }

            List<object> myList = (List<object>)myData;
            Dictionary<string,object> myItem;
            foreach(var obj in myList)
            {
                object myID, myTitle, myDesc, myPrice, myCents, myCurrency;
                myItem = obj as Dictionary<string,object>;
                myItem.TryGetValue("product_id", out myID);
                myItem.TryGetValue("title", out myTitle);
                myItem.TryGetValue("description", out myDesc);
                myItem.TryGetValue("price", out myPrice);
                myItem.TryGetValue("price_amount_cents", out myCents);
                myItem.TryGetValue("price_currency_code", out myCurrency);

                fbProduct testProduct = new fbProduct((string)myID, (string)myPrice, (string)myTitle, (string)myDesc, (string)myCurrency, Convert.ToDecimal(myCents));

                // Skip this product if it has already been received and added
                // (This allows FetchAdditionalProducts() to "work" with FaceBook)
                if (m_fbProd.ContainsKey((string)myID))
                {
                    continue;
                }
                m_fbProd.Add((string)myID, testProduct);
                var metaDict = new Dictionary<string,object>
                {
                    {"localizedTitle", (string)myTitle},
                    {"localizedDescription", (string)myDesc},
                    {"localizedPriceString", (string)myPrice},
                    {"localizedPrice", (Convert.ToDecimal(myCents)/100.0m)},
                    {"isoCurrencyCode", (string)myCurrency},
                };
                var itemDict = new Dictionary<string,object>
                {
                    {"storeSpecificId", myID},
                    {"metadata", metaDict}
                };
                m_itemList.Add(itemDict);
            }

            // Check for new (20170321) empty data[] coming from FB and fail accordingly
            //
            if(m_itemList.Count == 0)
            {
                Console.WriteLine("UnityIAP FB: ProductListCallback() RawResult had an empty \"data[]\" ");
                // again, not sure we want to do this now...
                // m_callback("OnSetupFailed", "NoProductsAvailable", "", "");
                return;
            }

            // Handle Pagination Here...
            object myPaging;
            dic.TryGetValue("paging", out myPaging);
            if(myPaging != null)
            {
                var asDict = myPaging as Dictionary<string, object>;
                if(asDict.ContainsKey("next"))
                {
                    object cursors;
                    asDict.TryGetValue("cursors", out cursors);
                    if(cursors != null)
                    {
                        var myCursors = cursors as Dictionary<string, object>;
                        object myNext = null;
                        myCursors.TryGetValue("after", out myNext);
                        PagedGraphRequest("products", ProductListCallback, k_FacebookPageSize, myNext.ToString());
                    }
                    else
                    {
                        Console.WriteLine("UnityIAP FB: couldn't find cursors");
                    }
                }
                else
                {
                    // Move on to the purchases
                    // Get purchases next -- single notification to Unity IAP at the end of that callback
                    RetrievePurchases();
                }
            }
            else
            {
                Console.WriteLine("UnityIAP FB: error, couldn't find paging -- moving on with purchases");
                RetrievePurchases();
            }
        }

        private void PurchaseListCallback(IGraphResult result)
        {
            // Treating this as a try-once...
            m_purchasesRetrieved = true;

            // Console.WriteLine("UnityIAP FB: PurchaseListCallback RawResult `{0}`", result.RawResult);
            if(!String.IsNullOrEmpty(result.Error))
            {
                Console.WriteLine("UnityIAP FB: PurchaseListCallback Error `{0}`", result.Error);
                Console.WriteLine("UnityIAP FB: Sending available products to purchasing system");

                string jsonProducts = MiniJson.JsonEncode(m_itemList);
                m_callback("OnProductsRetrieved", jsonProducts, "", "");
                return;
            }

            var dic = (Dictionary<string, object>) MiniJson.JsonDecode(result.RawResult);
            object myData;
            dic.TryGetValue("data", out myData);
            if(myData == null)
            {
                Console.WriteLine("UnityIAP FB: PurchaseListCallback() RawResult contains no \"data\" ");
                Console.WriteLine("UnityIAP FB: Sending available products to purchasing system");

                string jsonProducts = MiniJson.JsonEncode(m_itemList);
                m_callback("OnProductsRetrieved", jsonProducts, "", "");
                return;
            }

            // Process the purchases. We haven't seen an empty data[] section here, but if it does
            // appear it should be fine with this
            //
            List<object> myList = (List<object>)myData;
            Dictionary<string,object> myItem;
            foreach(var obj in myList)
            {
                object myToken, myProdId, myAppId, myPurchaseTime, myPaymentId, mySignedRequest;
                myItem = obj as Dictionary<string,object>;
                myItem.TryGetValue("purchase_token", out myToken);
                myItem.TryGetValue("product_id", out myProdId);
                myItem.TryGetValue("app_id", out myAppId);
                myItem.TryGetValue("purchase_time", out myPurchaseTime);
                myItem.TryGetValue("payment_id", out myPaymentId);
                myItem.TryGetValue("signed_request", out mySignedRequest);

                fbProduct fbProd;
                string myTitle;
                string myDesc;
                string myPrice;
                decimal myNumPrice;
                string myCurrency;
                if(m_fbProd.TryGetValue((string)myProdId, out fbProd))
                {
                    myTitle = fbProd.metadata.localizedTitle;
                    myDesc = fbProd.metadata.localizedDescription;
                    myPrice = fbProd.metadata.localizedPriceString;
                    myNumPrice = fbProd.metadata.localizedPrice;
                    myCurrency = fbProd.metadata.isoCurrencyCode;
                }
                else
                {
                    // Deal with case where there was no matching product
                    myTitle = "Unknown Product";
                    myDesc = "This product definition was created from an existing purchase";
                    myPrice = "unknown";
                    myNumPrice = 0m;
                    myCurrency = "unknown";
                }

                var metaDict = new Dictionary<string,object>
                {
                    {"localizedTitle", (string)myTitle},
                    {"localizedDescription", (string)myDesc},
                    {"localizedPriceString", (string)myPrice},
                    {"localizedPrice", myNumPrice},
                    {"isoCurrencyCode", (string)myCurrency},
                };
                var recDict = new Dictionary<string,object>
                {
                    {"purchaseToken", (string)myToken},
                    {"paymentId", (string)myPaymentId},
                    {"purchaseTime", Convert.ToDecimal(myPurchaseTime)},
                    {"appId", (string)myAppId},
                    {"signedRequest", (string)mySignedRequest}
                };
                // Match the purchase case so pending will work better
                var recJSON = new Dictionary<string,object>
                {
                    {"json", recDict}
                };
                string jsonReceipt = MiniJson.JsonEncode(recJSON);
                var itemDict = new Dictionary<string,object>
                {
                    {"storeSpecificId", (string)myProdId},
                    {"transactionId", (string)myToken}, // need to decide if this is the best ID here...
                    {"receipt", jsonReceipt},
                    {"metadata", metaDict}
                };
                // Console.WriteLine("Adding purchase of {0} ID {1}", myProdId, myToken);
                m_itemList.Add(itemDict);
            }

            // Handle Pagination Here...
            object myPaging;
            dic.TryGetValue("paging", out myPaging);
            if(myPaging != null)
            {
                var asDict = myPaging as Dictionary<string, object>;
                if(asDict.ContainsKey("next"))
                {
                    object cursors;
                    asDict.TryGetValue("cursors", out cursors);
                    if(cursors != null)
                    {
                        var myCursors = cursors as Dictionary<string, object>;
                        object myNext = null;
                        myCursors.TryGetValue("after", out myNext);
                        PagedGraphRequest("purchases", PurchaseListCallback, k_FacebookPageSize, myNext.ToString());
                    }
                    else
                    {
                        Console.WriteLine("UnityIAP FB: couldn't find cursors");
                    }
                }
                else
                {
                    // No Next, so done
                    string jsonData = MiniJson.JsonEncode(m_itemList);
                    Console.WriteLine("UnityIAP FB: end of paged purchases, sending {0}", jsonData);
                    m_callback("OnProductsRetrieved", jsonData, "", "");
                }
            }
            else
            {
                Console.WriteLine("UnityIAP FB: couldn't find paging in purchases response");
                string jsonData = MiniJson.JsonEncode(m_itemList);
                m_callback("OnProductsRetrieved", jsonData, "", "");
            }
        }

        private void PurchaseCallback(IPayResult result)
        {

            if(result == null)
            {
                Console.WriteLine("UnityIAP FB: PurchaseCallback() result is null");
                return;
            }

            // Console.WriteLine("UnityIAP FB: PurchaseCallback() RawResult " + result.RawResult);
            if(!String.IsNullOrEmpty(result.Error))
            {
                string reason = "Unknown";
                Console.WriteLine("UnityIAP FB: PurchaseCallback Error `{0}` (ErrorCode {1})", result.Error, result.ErrorCode);

                // This is currently the only error we really know...
                if(result.Cancelled == true)
                {
                    reason = "UserCancelled";
                }

                // need to check for other error values _here_ now
                //
                // 2016-10-30
                // Kevin @ FB indicates they are switching error_code in response from string to number
                // so let's wait for that to be implemented... Old codes follow:
                    // NB: This is all outdated now due to changes in the API / response JSON.
                    // Keeping here for reference until things stabilize...
                    // case "1383066":
                    //     reason = "ExistingPurchasePending";
                    //     break;

                    // case "1383010":
                    //     reason = "UserCancelled";
                    //     break;

                    // case "1383001":
                    //     // this is the "unable to get payment request lock flow_name" version seen 2016-10-19
                    //     reason = "UserCancelled";
                    //     break;

                // 2017-03-06
                // The error_code is now present in IPayResult as ErrorCode but the only example we can
                // force is the Cancelled case. Pending purchase case has a code of 0 at present
                // Going to log errors via custom event to see if there's anything useful

                // Notify error here
                var errDict = new Dictionary<string, object>
                {
                    {"productId", m_purcItem},
                    {"reason", reason},
                    {"message", result.Error}
                };
                var errJson = MiniJson.JsonEncode(errDict);
                m_callback("OnPurchaseFailed", errJson, "", "");
                // Using this to log error messages
                m_callback("SendPurchasingEvent", "FacebookError", "Code: " + result.ErrorCode, result.Error);
                return;
            }

            // Handle successful purchase
            //
            var myResp = result.ResultDictionary;
            object myToken, myProdId, myAppId, myPurchaseTime, myPaymentId, mySignedRequest;

            if(myResp.TryGetValue("purchase_token", out myToken) &&
                myResp.TryGetValue("product_id", out myProdId) &&
                myResp.TryGetValue("app_id", out myAppId) &&
                myResp.TryGetValue("purchase_time", out myPurchaseTime) &&
                myResp.TryGetValue("payment_id", out myPaymentId) &&
                myResp.TryGetValue("signed_request", out mySignedRequest) &&
                (myToken != null))
            {
                // We are now passing these through  without any cast. This is due to
                // ongoing changes on the FB side with inconsistencies between what's in the
                // JSON for WebGL vs Windows (Gameroom)
                //
                var recDict = new Dictionary<string,object>
                {
                    {"purchaseToken", myToken},
                    {"paymentId", myPaymentId},
                    {"purchaseTime", myPurchaseTime},
                    {"appId", myAppId},
                    {"signedRequest", mySignedRequest}
                };
                var recJSON = new Dictionary<string,object>
                {
                    {"json", recDict}
                };
                string jsonReceipt = MiniJson.JsonEncode(recJSON);

                m_callback("OnPurchaseSucceeded", (string)myProdId, jsonReceipt, (string)myToken);
                return;
            }
            else
            {
                // Something is wrong with the JSON from Facebook (again)
                var errDict = new Dictionary<string, object>
                {
                    {"productId", m_purcItem},
                    {"reason", "Unknown"},
                    {"message", "Problem with response JSON from server"}
                };
                var errJson = MiniJson.JsonEncode(errDict);
                m_callback("OnPurchaseFailed", errJson, "", "");
                return;
            }
        }
    }
}
#endif
