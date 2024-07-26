#nullable enable
using System;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing.UseCases
{
    class AmazonAppsGetAmazonUserIdUseCase : IAmazonAppsGetAmazonUserIdUseCase
    {

        private IAmazonJavaStore m_AmazonJavaStore;

        public AmazonAppsGetAmazonUserIdUseCase(IAmazonJavaStore amazonJavaStore)
        {
            m_AmazonJavaStore = amazonJavaStore;
        }

        public string GetAmazonUserId()
        {
            return m_AmazonJavaStore.GetAmazonUserId();
        }
    }
}
