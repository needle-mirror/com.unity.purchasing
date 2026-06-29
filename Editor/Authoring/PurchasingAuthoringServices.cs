using System;
using System.Collections.ObjectModel;
using Unity.Purchasing.Editor.Shared.Analytics;
using Unity.Purchasing.Editor.Shared.DependencyInversion;
using Unity.Purchasing.Editor.Shared.UI;
using Unity.Purchasing.Editor.Shared.WebApi;
using Unity.Purchasing.Editor.Shared.WebApi.Network;
using Unity.Services.Core.Editor;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.Core.Editor.OrganizationHandler;
using UnityEngine;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Deploy;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using UnityEditor.Purchasing.Editor.Authoring.Deployment;
using UnityEditor.Purchasing.Editor.Authoring.IO;
using UnityEditor.Purchasing.Editor.Authoring.LiveContentAdminApi;
using UnityEditor.Purchasing.Editor.Authoring.Model;
using static Unity.Purchasing.Editor.Shared.DependencyInversion.Factories;
using ILogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger.ILogger;
using Logger = UnityEditor.Purchasing.Editor.Authoring.Logging.Logger;

namespace UnityEditor.Purchasing.Editor.Authoring
{
    public static class PurchasingAuthoringServiceProvider
    {
        public static T GetService<T>()
        {
            return PurchasingAuthoringServices.Instance.GetService<T>();
        }
    }

    class PurchasingAuthoringServices : AbstractRuntimeServices<PurchasingAuthoringServices>
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            Instance.Initialize(new ServiceCollection());
            var deploymentItemProvider = Instance.GetService<DeploymentProvider>();
            Deployments.Instance.DeploymentProviders.Add(deploymentItemProvider);
        }

        public override void Register(ServiceCollection collection)
        {
            // This is the Dependency Inversion container for the assembly
            collection.Register(Default<ICommonAnalytics, CommonAnalytics>);
#if UNITY_2023_2_OR_NEWER
            collection.Register(Default<ICommonAnalyticProvider, CommonAnalyticProvider>);
#endif
            collection.RegisterSingleton(Default<ObservableCollection<CatalogItemAsset>, ObservableCatalogItemAssets>);
            collection.Register(Default<IProjectIdentifierProvider, ProjectIdentifierProvider>);
            collection.Register(c => (ObservableCatalogItemAssets)c.GetService(typeof(ObservableCollection<CatalogItemAsset>)));
            collection.Register(Default<DeployCommand>);
            collection.Register(Default<DeployCommandWrapper>);
            collection.Register(Default<CatalogOpenDashboardCommand>);
            collection.Register(Default<ICatalogDashboardUrlResolver, CatalogDashboardUrlResolver>);
            collection.Register(_ => OrganizationProvider.Organization);
            collection.Register(Default<DeleteRemoteCommand>);
            collection.Register(Default<DeleteRemoteCommandWrapper>);
            collection.Register(Default<ICatalogDeploymentHandler, CatalogDeploymentHandler>);
            //Command initializes it, but depended on by handler
            collection.Register<IRetryPolicy>(_ => null);
            collection.Register(Default<IApiClient, ApiClient>);
            collection.Register<IConfigsApi>(sp => new ConfigsApi(
                (IApiClient)sp.GetService(typeof(IApiClient)),
                new ApiConfiguration { BasePath = LiveContentAdminEnvironment.BasePath }));
            collection.RegisterSingleton(Default<ILiveContentConfigClient, LiveContentConfigClient>);
            collection.Register(Default<IAccessTokens, AccessTokens>);
            collection.Register(_ => EnvironmentsApi.Instance);
            collection.RegisterStartupSingleton(Default<DeploymentProvider, PurchasingDeploymentProvider>);
            collection.Register(Default<ICatalogLoader, CatalogItemLoader>);
            collection.Register(Default<ICatalogCsvParser, CatalogCsvParser>);
            collection.RegisterSingleton(Default<ObservableCatalogCsvAssets>);
            collection.Register(Default<IDisplayDialog, DisplayDialog>);

            collection.Register(Default<ILogger, Logger>);
            collection.Register(Default<IFileSystem, FileSystem>);
        }
    }
}
