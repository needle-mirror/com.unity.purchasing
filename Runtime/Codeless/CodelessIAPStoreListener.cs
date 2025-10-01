using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Initializes Unity IAP with the <typeparamref name="Product"/>s defined in the default IAP <see cref="ProductCatalog"/>.
    /// Automatically initializes at runtime load when enabled in the GUI. <seealso cref="ProductCatalog.enableCodelessAutoInitialization"/>
    /// Manages <typeparamref name="IAPButton"/>s and <typeparamref name="IAPListener"/>s.
    /// </summary>
    public class CodelessIAPStoreListener : IDetailedStoreListener
    {
        private static CodelessIAPStoreListener instance;

//disable Warning CS0618  IAPButton is deprecated, please use CodelessIAPButton instead.
#pragma warning disable 0618
        private readonly List<IAPButton> activeButtons = new List<IAPButton>();
        private readonly List<CodelessIAPButton> activeCodelessButtons = new List<CodelessIAPButton>();
        private readonly List<IAPListener> activeListeners = new List<IAPListener>();
        private static bool unityPurchasingInitialized;

        /// <summary>
        /// For advanced scripted IAP actions, use this session's <typeparamref name="IStoreController"/> after initialization.
        /// </summary>
        /// <see cref="StoreController"/>
        protected IStoreController controller;
        /// <summary>
        /// For advanced scripted store-specific IAP actions, use this session's <typeparamref name="IExtensionProvider"/> after initialization.
        /// </summary>
        /// <see cref="ExtensionProvider"/>
        protected IExtensionProvider extensions = null!;

        ConfigurationBuilder m_Builder = null!;

        /// <summary>
        /// For adding <typeparamref name="ProductDefinition"/> this default <typeparamref name="ProductCatalog"/> is
        /// loaded from the Project
        /// when I am instantiated.
        /// </summary>
        /// <see cref="Instance"/>
        protected ProductCatalog catalog;

        /// <summary>
        /// Allows outside sources to know whether the successful initialization has completed.
        /// </summary>
        public static bool initializationComplete;

        [RuntimeInitializeOnLoadMethod]
        static void InitializeCodelessPurchasingOnLoad()
        {
            var catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog.enableCodelessAutoInitialization && !catalog.IsEmpty() && instance == null)
            {
                CreateCodelessIAPStoreListenerInstance();
            }
        }

        private static void InitializePurchasing()
        {
            var module = StandardPurchasingModule.Instance();
            module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

            var builder = ConfigurationBuilder.Instance(module);

            IAPConfigurationHelper.PopulateConfigurationBuilder(ref builder, Instance.catalog);
            Instance.m_Builder = builder;

            UnityPurchasing.Initialize(Instance, builder);

            unityPurchasingInitialized = true;
        }

        /// <summary>
        /// For advanced scripted store-specific IAP actions, use this session's <typeparamref name="IStoreConfiguration"/>s.
        /// Note, these instances are only available after initialization through Codeless IAP, currently.
        /// </summary>
        /// <typeparam name="T">A subclass of <typeparamref name="IStoreConfiguration"/> such as <typeparamref name="IAppleConfiguration"/></typeparam>
        /// <returns></returns>
        public T GetStoreConfiguration<T>() where T : IStoreConfiguration
        {
            return m_Builder.Configure<T>();
        }

        /// <summary>
        /// For advanced scripted store-specific IAP actions, use this session's <typeparamref name="IStoreExtension"/>s after initialization.
        /// </summary>
        /// <typeparam name="T">A subclass of <typeparamref name="IStoreExtension"/> such as <typeparamref name="IAppleExtensions"/></typeparam>
        /// <returns></returns>
        public T GetStoreExtensions<T>() where T : IStoreExtension
        {
            return extensions.GetExtension<T>();
        }

        private CodelessIAPStoreListener()
        {
            catalog = ProductCatalog.LoadDefaultCatalog();
        }

        /// <summary>
        /// Singleton of me. Initialized on first access.
        /// Also initialized by RuntimeInitializeOnLoadMethod if <typeparamref name="ProductCatalog.enableCodelessAutoInitialization"/>
        /// is true.
        /// </summary>
        public static CodelessIAPStoreListener Instance
        {
            get
            {
                if (instance == null)
                {
                    CreateCodelessIAPStoreListenerInstance();
                }

                return instance!;
            }
        }

        /// <summary>
        /// Creates the static instance of CodelessIAPStoreListener and initializes purchasing
        /// </summary>
        private static async void CreateCodelessIAPStoreListenerInstance()
        {
            instance = new CodelessIAPStoreListener();
            if (!unityPurchasingInitialized)
            {
                await AutoInitializeUnityGamingServicesIfEnabled();
                InitializePurchasing();
            }
        }

        private static Task AutoInitializeUnityGamingServicesIfEnabled()
        {
            return ShouldAutoInitUgs()
                ? UnityServices.InitializeAsync()
                : Task.CompletedTask;
        }

        private static bool ShouldAutoInitUgs()
        {
            return Instance.catalog.enableCodelessAutoInitialization &&
                Instance.catalog.enableUnityGamingServicesAutoInitialization;
        }

        /// <summary>
        /// For advanced scripted IAP actions, use this session's <typeparamref name="IStoreController"/> after
        /// initialization.
        /// </summary>
        /// <see cref="StoreController"/>
        public IStoreController StoreController => controller;

        /// <summary>
        /// Inspect my <typeparamref name="ProductCatalog"/> for a product identifier.
        /// </summary>
        /// <param name="productID">Product identifier to look for in <see cref="catalog"/>. Note this is not the
        /// store-specific identifier.</param>
        /// <returns>Whether this identifier exists in <see cref="catalog"/></returns>
        /// <see cref="catalog"/>
        public bool HasProductInCatalog(string productID)
        {
            foreach (var product in catalog.allProducts)
            {
                if (product.id == productID)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Access a <typeparamref name="Product"/> for this app.
        /// </summary>
        /// <param name="productID">A product identifier to find as a <typeparamref name="Product"/></param>
        /// <returns>A <typeparamref name="Product"/> corresponding to <paramref name="productID"/>, or <c>null</c> if
        /// the identifier is not available.</returns>
        public Product GetProduct(string productID)
        {
            if (controller != null && controller.products != null && !string.IsNullOrEmpty(productID))
            {
                return controller.products.WithID(productID);
            }

            Debug.LogError("CodelessIAPStoreListener attempted to get unknown product " + productID);
            return null;
        }

        /// <summary>
        /// Register an <typeparamref name="IAPButton"/> to send IAP initialization and purchasing events.
        /// Use to making IAP functionality visible to the user.
        /// </summary>
        /// <param name="button">The <typeparamref name="IAPButton"/></param>
        [Obsolete("CodelessIAPStoreListener.AddButton(IAPButton button) is deprecated, please use CodelessIAPStoreListener.AddButton(CodelessIAPButton button) instead.", false)]
        public void AddButton(IAPButton button)
        {
            activeButtons.Add(button);
        }

        /// <summary>
        /// Register an <typeparamref name="CodelessIAPButton"/> to send IAP initialization and purchasing events.
        /// Use to making IAP functionality visible to the user.
        /// </summary>
        /// <param name="button"></param>
        public void AddButton(CodelessIAPButton button)
        {
            activeCodelessButtons.Add(button);
        }

        /// <summary>
        /// Stop sending initialization and purchasing events to an <typeparamref name="IAPButton"/>. Use when disabling
        /// the button, e.g. when closing a scene containing that button and wanting to prevent the user from making any
        /// IAP events for its product.
        /// </summary>
        /// <param name="button"></param>
        [Obsolete("CodelessIAPStoreListener.RemoveButton(IAPButton button) is deprecated, please use CodelessIAPStoreListener.RemoveButton(CodelessIAPButton button) instead.", false)]
        public void RemoveButton(IAPButton button)
        {
            activeButtons.Remove(button);
        }

        /// <summary>
        /// Stop sending initialization and purchasing events to an <typeparamref name="CodelessIAPButton"/>. Use when disabling
        /// the button, e.g. when closing a scene containing that button and wanting to prevent the user from making any
        /// IAP events for its product.
        /// </summary>
        /// <param name="button"></param>
        public void RemoveButton(CodelessIAPButton button)
        {
            activeCodelessButtons.Remove(button);
        }

        /// <summary>
        /// Register an <typeparamref name="IAPListener"/> to send IAP purchasing events.
        /// </summary>
        /// <param name="listener">Listener to receive IAP purchasing events</param>
        public void AddListener(IAPListener listener)
        {
            activeListeners.Add(listener);
        }

        /// <summary>
        /// Unregister an <typeparamref name="IAPListener"/> from IAP purchasing events.
        /// </summary>
        /// <param name="listener">Listener to no longer receive IAP purchasing events</param>
        public void RemoveListener(IAPListener listener)
        {
            activeListeners.Remove(listener);
        }

        /// <summary>
        /// Purchase a product by its identifier.
        /// Sends purchase failure event with <typeparamref name="PurchaseFailureReason.PurchasingUnavailable"/>
        /// to all registered IAPButtons if not yet successfully initialized.
        /// </summary>
        /// <param name="productID">Product identifier of <typeparamref name="Product"/> to be purchased</param>
        public void InitiatePurchase(string productID)
        {
            if (controller == null)
            {
                Debug.LogError("Purchase failed because Purchasing was not initialized correctly");

                SendPurchaseFailedEventsToAllButtons(productID);

                return;
            }

            controller.InitiatePurchase(productID);
        }

        void SendPurchaseFailedEventsToAllButtons(string productID)
        {
            foreach (var button in activeButtons.Where(button => button.productId == productID))
            {
                button.OnPurchaseFailed(null, PurchaseFailureReason.PurchasingUnavailable);
            }

            foreach (var button in activeCodelessButtons.Where(button => button.productId == productID))
            {
                var purchaseFailureDescription = new PurchaseFailureDescription(productID, PurchaseFailureReason.PurchasingUnavailable, "PurchasingUnavailable");
                button.OnPurchaseFailed(null, purchaseFailureDescription);
            }
        }

        /// <summary>
        /// Implementation of <typeparamref name="UnityEngine.Purchasing.IStoreListener.OnInitialized"/> which captures
        /// successful IAP initialization results and refreshes all registered <typeparamref name="IAPButton"/>s.
        /// </summary>
        /// <param name="controller">Set as the current IAP session's single <typeparamref name="IStoreController"/></param>
        /// <param name="extensions">Set as the current IAP session's single <typeparamref name="IExtensionProvider"/></param>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            initializationComplete = true;
            this.controller = controller;
            this.extensions = extensions;

            foreach (var iapListener in activeListeners)
            {
                iapListener.onProductsFetched.Invoke(controller.products);
            }

            HandleOnInitForAllButtons();
        }

        void HandleOnInitForAllButtons()
        {
            foreach (var button in activeButtons)
            {
                button.OnInitCompleted();
            }

            foreach (var button in activeCodelessButtons)
            {
                button.OnInitCompleted();
            }
        }

        /// <summary>
        /// Implementation of <typeparamref name="UnityEngine.Purchasing.IStoreListener.OnInitializeFailed"/> which
        /// logs the failure reason.
        /// </summary>
        /// <param name="error">Reported in the app log. </param>
        [Obsolete("OnInitializeFailed(InitializationFailureReason error) is deprecated, please use OnInitializeFailed(InitializationFailureReason error, string message) instead.")]
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, null);
        }

        /// <summary>
        /// Implementation of <typeparamref name="UnityEngine.Purchasing.IStoreListener.OnInitializeFailed"/> which
        /// logs the failure reason.
        /// </summary>
        /// <param name="error">Reported in the app log. </param>
        /// <param name="message"> More information on the failure reason. </param>
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            var errorMessage = $"Purchasing failed to initialize. Reason: {error.ToString()}.";

            if (message != null)
            {
                errorMessage += $" More details: {message}";
            }

            Debug.LogError(errorMessage);
        }

        /// <summary>
        /// Implementation of <typeparamref name="UnityEngine.Purchasing.IStoreListener.ProcessPurchase"/> which forwards
        /// this successful purchase event to any appropriate registered <typeparamref name="IAPButton"/>s and
        /// <typeparamref name="IAPListener"/>s. Logs an error if there are no appropriate registered handlers.
        /// </summary>
        /// <param name="e">Data for this purchase</param>
        /// <returns>Any indication of whether this purchase has been completed by any of my appropriate registered
        /// <typeparamref name="IAPButton"/>s or <typeparamref name="IAPListener"/>s</returns>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            PurchaseProcessingResult result;

            // if any receiver consumed this purchase we return the status
            var consumePurchase = false;
            var resultProcessed = false;

            foreach (var button in activeButtons.Where(button => button.productId == e.purchasedProduct.definition.id))
            {
                result = button.ProcessPurchase(e);

                if (result == PurchaseProcessingResult.Complete)
                {
                    consumePurchase = true;
                }

                resultProcessed = true;
            }

            foreach (var button in activeCodelessButtons.Where(button => button.productId == e.purchasedProduct.definition.id))
            {
                result = button.ProcessPurchase(e);

                if (result == PurchaseProcessingResult.Complete)
                {
                    consumePurchase = true;
                }

                resultProcessed = true;
            }

            foreach (var listener in activeListeners)
            {
                result = listener.ProcessPurchase(e);

                if (result == PurchaseProcessingResult.Complete)
                {
                    consumePurchase = true;
                }

                resultProcessed = true;
            }

            // we expect at least one receiver to get this message
            if (!resultProcessed)
            {
                Debug.LogError("Purchase not correctly processed for product \"" +
                    e.purchasedProduct.definition.id +
                    "\". Add an active IAPButton to process this purchase, or add an IAPListener to receive any unhandled purchase events.");
            }

            return consumePurchase ? PurchaseProcessingResult.Complete : PurchaseProcessingResult.Pending;
        }

        /// <summary>
        /// Implementation of <typeparamref name="UnityEngine.Purchasing.IStoreListener.OnPurchaseFailed"/> indicating
        /// a purchase failed with specified reason. Send this event to any appropriate registered
        /// <typeparamref name="IAPButton"/>s and <typeparamref name="IAPListener"/>s.
        /// Logs an error if there are no appropriate registered handlers.
        /// </summary>
        /// <param name="product">What failed to purchase</param>
        /// <param name="reason">Why the purchase failed</param>
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            var resultProcessed = false;

            foreach (var button in activeButtons.Where(button => button.productId == product.definition.id))
            {
                button.OnPurchaseFailed(product, reason);

                resultProcessed = true;
            }

            if (activeCodelessButtons.Exists(button => button.productId == product.definition.id))
            {
                resultProcessed = true;
            }

            foreach (var listener in activeListeners)
            {
                listener.OnPurchaseFailed(product, reason);

                resultProcessed = true;
            }

            // we expect at least one receiver to get this message
            if (!resultProcessed)
            {
                Debug.LogError("Failed purchase not correctly handled for product \"" + product.definition.id +
                    "\". Add an active IAPButton to handle this failure, or add an IAPListener to receive any unhandled purchase failures.");
            }
        }

        /// <summary>
        /// Implementation of <typeparamref name="UnityEngine.Purchasing.IDetailedStoreListener.OnPurchaseFailed"/> indicating
        /// a purchase failed with a detailed failure description. Send this event to any appropriate registered
        /// <typeparamref name="IAPButton"/>s and <typeparamref name="IAPListener"/>s.
        /// Logs an error if there are no appropriate registered handlers.
        /// </summary>
        /// <param name="product">What failed to purchase</param>
        /// <param name="description">Why the purchase failed</param>
        public void OnPurchaseFailed(Product product, PurchaseFailureDescription description)
        {
            OnPurchaseFailed(product, description.reason);

            foreach (var button in activeCodelessButtons.Where(button => button.productId == product.definition.id))
            {
                button.OnPurchaseFailed(product, description);
            }

            foreach (var listener in activeListeners)
            {
                listener.OnPurchaseFailed(product, description);
            }
        }
    }
}
