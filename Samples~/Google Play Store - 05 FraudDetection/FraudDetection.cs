using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.GooglePlay.FraudDetection
{
    public class FraudDetection : MonoBehaviour
    {
        PurchasingController m_PurchasingController;
        string m_StoreName;

        public User user;

        public string goldProductId = "com.mycompany.mygame.gold1";
        public ProductType goldType = ProductType.Consumable;

        public Text goldCountText;

        int m_GoldCount;

        void Awake()
        {
            InitializeIAP();
        }

        void Start()
        {
            UpdateUI();
        }

        async void InitializeIAP()
        {
            m_StoreName = UnityIAPServices.GetDefaultStore();
            m_PurchasingController = new PurchasingController(m_StoreName);

            m_PurchasingController.OnPurchasePending += OnPurchasePending;
            m_PurchasingController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_PurchasingController.OnPurchaseFailed += OnPurchaseFailed;

            await m_PurchasingController.Connect();

            ConfigureGoogleFraudDetection(m_PurchasingController.GooglePlayStoreExtendedService);
            FetchProducts();
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new(goldProductId, goldType)
            };

            m_PurchasingController.FetchProducts(initialProductsToFetch, m_StoreName);
        }

        void ConfigureGoogleFraudDetection(IGooglePlayStoreExtendedService googlePlayStoreExtendedService)
        {
            if (googlePlayStoreExtendedService == null)
            {
                Debug.Log("Google Play Store Extended Service is not available. Please make sure the project is being built for Android and the Google Play Store.");
                return;
            }
            //To make sure the account id and profile id do not contain personally identifiable information, we obfuscate this information by hashing it.
            var obfuscatedAccountId = HashString(user.AccountId);
            var obfuscatedProfileId = HashString(user.ProfileId);

            googlePlayStoreExtendedService.SetObfuscatedAccountId(obfuscatedAccountId);
            googlePlayStoreExtendedService.SetObfuscatedProfileId(obfuscatedProfileId);
        }

        static string HashString(string input)
        {
            var stringBuilder = new StringBuilder();
            foreach (var b in GetHash(input))
                stringBuilder.Append(b.ToString("X2"));

            return stringBuilder.ToString();
        }

        static IEnumerable<byte> GetHash(string input)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        public void BuyGold()
        {
            m_PurchasingController.PurchaseProduct(goldProductId, m_StoreName);
        }

        void OnPurchasePending(string storeName, PendingOrder order)
        {
            m_PurchasingController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(string storeName, Order order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("Could not find product in purchase confirmation.");
            }
            switch (order)
            {
                case ConfirmedOrder:
                    Debug.Log($"Order confirmed - Product: {GetIdFromProduct(product)}");
                    UnlockContent(product);
                    break;
                case FailedOrder:
                    Debug.Log($"Confirmation failed - Product: {GetIdFromProduct(product)}");
                    break;
                default:
                    Debug.Log("Unknown OnPurchaseConfirmed result.");
                    break;
            }
        }

        void OnPurchaseFailed(string storeName, FailedOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("Could not find product in failed order.");
            }

            Debug.Log($"Purchase failed - Product: '{GetIdFromProduct(product)}'," +
                      $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                      + $"Purchase Failure Details: {order.Details}");
        }

        void UnlockContent(Product product)
        {
            if (product is not null && product.definition.id == goldProductId)
            {
                AddGold();
            }

            UpdateUI();
        }

        void AddGold()
        {
            m_GoldCount++;
        }

        void UpdateUI()
        {
            goldCountText.text = $"Your Gold: {m_GoldCount}";
        }

        Product GetFirstProductInOrder(Order order)
        {
            return order.CartOrdered.Items().FirstOrDefault()?.Product;
        }

        string GetIdFromProduct(Product product)
        {
            return product?.definition.id ?? "no product found";
        }
    }
}
