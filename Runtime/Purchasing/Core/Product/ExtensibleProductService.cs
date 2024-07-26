using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// An abstract store service to extend an existing Product Service which will handle all of the basic IProductService implementations
    /// The main purpose of this is to allow a custom store to add implementations of extended features to this service.
    /// The calls to IProductService are kept virtual so that the derivations of the base store implementing them can be added to or overridden.
    /// </summary>
    public abstract class ExtensibleProductService : IProductService
    {
        IProductService m_BaseInternalProductService;

        /// <summary>
        /// Constructor to be used by derived classes
        /// </summary>
        /// <param name="baseProductService"> The base service implementation which implements IProductService </param>
        protected ExtensibleProductService(IProductService baseProductService)
        {
            m_BaseInternalProductService = baseProductService;
        }

        /// <summary>
        /// Apple Specific Product Extensions
        /// </summary>
        public virtual IAppleStoreExtendedProductService Apple => m_BaseInternalProductService.Apple;

        /// <summary>
        /// Amazon Specific Product Extensions
        /// </summary>
        public virtual IAmazonAppsExtendedProductService Amazon => m_BaseInternalProductService.Amazon;
        public virtual void FetchProductsWithNoRetries(List<ProductDefinition> productDefinitions)
        {
            m_BaseInternalProductService.FetchProductsWithNoRetries(productDefinitions);
        }

        public virtual void FetchProducts(List<ProductDefinition> productDefinitions, IRetryPolicy retryPolicy)
        {
            m_BaseInternalProductService.FetchProducts(productDefinitions, retryPolicy);
        }

        public virtual ReadOnlyObservableCollection<Product> GetProducts()
        {
            return m_BaseInternalProductService.GetProducts();
        }

        public virtual void AddProductsUpdatedAction(Action<List<Product>> updatedAction)
        {
            m_BaseInternalProductService.AddProductsUpdatedAction(updatedAction);
        }

        public virtual void AddProductsFetchFailedAction(Action<ProductFetchFailed> failedAction)
        {
            m_BaseInternalProductService.AddProductsFetchFailedAction(failedAction);
        }

        public virtual void RemoveProductsUpdatedAction(Action<List<Product>> updatedAction)
        {
            m_BaseInternalProductService.RemoveProductsUpdatedAction(updatedAction);
        }

        public virtual void RemoveProductsFetchFailedAction(Action<ProductFetchFailed> failedAction)
        {
            m_BaseInternalProductService.RemoveProductsFetchFailedAction(failedAction);
        }
    }
}
