using UnityEngine;
using UnityEngine.UI;

namespace Samples.Purchasing.IAP5.Demo
{
    [RequireComponent(typeof(Button))]
    public class ProductPurchaseButtonHelper : MonoBehaviour
    {
        public PaywallManager paywallManager;

        public string productId;
        public bool consumePurchase = true;

        public Text titleText;
        public Text descriptionText;
        public Text priceText;

        void Start()
        {
            ConfigureButton();
        }

        void ConfigureButton()
        {
            var button = GetComponent<Button>();

            if (button)
            {
                button.onClick.AddListener(PurchaseProduct);
            }

            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogError("ProductPurchaseButtonHelper productId is empty");
            }

            if (paywallManager == null)
            {
                Debug.LogError("ProductPurchaseButtonHelper paywallManager is unassigned");
            }
        }

        void PurchaseProduct()
        {
            paywallManager.InitiatePurchase(productId);
        }

        void OnEnable()
        {
            paywallManager?.RegisterButton(this);

            UpdateText();
        }

        void OnDisable()
        {
            paywallManager?.UnregisterButton(this);
        }

        internal void UpdateText()
        {
            var product = paywallManager?.FindProduct(productId);
            if (product != null)
            {
                if (titleText != null)
                {
                    titleText.text = product.metadata.localizedTitle;
                }

                if (descriptionText != null)
                {
                    descriptionText.text = product.metadata.localizedDescription;
                }

                if (priceText != null)
                {
                    priceText.text = product.metadata.localizedPriceString;
                }
            }
        }
    }
}
