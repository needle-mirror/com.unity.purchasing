using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.FraudDetection
{
    public class FraudDetection : MonoBehaviour
    {
        StoreController m_StoreController;

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
            ConfigureAppleFraudDetection();
        }

        async void InitializeIAP()
        {
            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_StoreController.OnPurchaseFailed += OnPurchaseFailed;

            await m_StoreController.Connect();

            FetchProducts();
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new(goldProductId, goldType)
            };

            m_StoreController.FetchProducts(initialProductsToFetch);
        }

        void OnPurchasePending(PendingOrder order)
        {
            m_StoreController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
        {
            if (order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id == goldProductId)
            {
                AddGold();
                UpdateUI();
            }
            else
            {
                UpdateUI();
            }
        }

        void ConfigureAppleFraudDetection()
        {
            //To make sure the account id and profile id do not contain personally identifiable information, we obfuscate this information by hashing it.
            var hashedUsername = HashString(user.Username);

            m_StoreController.AppleStoreExtendedService?.SetAppAccountToken(new Guid(hashedUsername));
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
            {
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        public void BuyGold()
        {
            m_StoreController.PurchaseProduct(goldProductId);
        }

        void AddGold()
        {
            m_GoldCount++;
        }

        void UpdateUI()
        {
            goldCountText.text = $"Your Gold: {m_GoldCount}";
        }

        public void OnPurchaseFailed(FailedOrder order)
        {
            Debug.Log($"Purchase failed - Product: '{order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id}'," +
                $" Purchase failure reason: {order.FailureReason}," +
                $" Purchase failure details: {order.Details}");
        }
    }
}
