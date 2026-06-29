using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Purchasing.Editor.Shared.Assets;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using UnityEditor.Purchasing.Editor.Authoring.IO;
using UnityEditor.Purchasing.Shared.EditorUtils;
using UnityEngine;
using ILogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger.ILogger;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    /// <summary>
    /// This class serves to track creation and deletion of assets of the
    /// associated service type
    /// </summary>
    sealed class ObservableCatalogItemAssets : ObservableCollection<CatalogItemAsset>, IDisposable
    {
        const string k_DeserializationError = "DeserializationException";
        readonly ICatalogLoader m_ResourceLoader;
        readonly ILiveContentConfigClient m_Client;
        readonly IEnvironmentsApi m_EnvironmentsApi;
        readonly ILogger m_Logger;
        readonly ObservableAssets<CatalogItemAsset> m_CatalogItemAssets;
        List<CatalogItem> m_RemoteResources;
        DateTime m_LastFetched;
        Task m_ProjectReady;
        Task m_RemoteResourcesTask;

        public ObservableCollection<IDeploymentItem> DeploymentItems { get; } =
            new ObservableCollection<IDeploymentItem>();

        public ObservableCatalogItemAssets(
            ICatalogLoader resourceLoader,
            ILiveContentConfigClient client,
            IEnvironmentsApi environmentsApi,
            ILogger logger)
        {
            m_ResourceLoader = resourceLoader;
            m_Client = client;
            m_EnvironmentsApi = environmentsApi;
            m_Logger = logger;
            m_CatalogItemAssets = new ObservableAssets<CatalogItemAsset>(new[] { Constants.FileExtension });

            foreach (var asset in m_CatalogItemAssets)
            {
                OnNewAsset(asset);
                DeploymentItems.Add(asset.CatalogEntryDeploymentItem);
            }

            m_CatalogItemAssets.CollectionChanged += CatalogItemAssetsOnCollectionChanged;

            m_LastFetched = DateTime.MinValue;
#if UNITY_2022_1_OR_NEWER
            m_ProjectReady = Sync.WaitForEventAsync(
                h => CloudProjectSettingsEventManager.instance.projectStateChanged += h,
                h => CloudProjectSettingsEventManager.instance.projectStateChanged -= h);
#else
            m_ProjectReady = Task.CompletedTask;
#endif
        }

        public void Dispose()
        {
            m_CatalogItemAssets.CollectionChanged -= CatalogItemAssetsOnCollectionChanged;
        }

        void OnNewAsset(CatalogItemAsset itemAsset)
        {
            PopulateModel(itemAsset);
            Add(itemAsset);
        }

        void PopulateModel(CatalogItemAsset itemAsset, string assetPath = null)
        {
            var serializedSuccessfully = true;
            try
            {
                assetPath ??= itemAsset.Path;
                itemAsset.CatalogEntryDeploymentItem.ClearTypedStates(k_DeserializationError);
                m_ResourceLoader.DeserializeAndPopulateFromPath(
                    itemAsset.CatalogEntryDeploymentItem,
                    assetPath);
            }
            catch (AggregateException ex) when (ex.InnerException is CatalogDeserializationException e)
            {
                itemAsset.CatalogEntryDeploymentItem.States.Add(
                    new AssetState(e.ErrorMessage, e.Details, SeverityLevel.Error, k_DeserializationError));
                serializedSuccessfully = false;
            }
            catch (CatalogDeserializationException e)
            {
                itemAsset.CatalogEntryDeploymentItem.States.Add(
                    new AssetState(e.ErrorMessage, e.Details, SeverityLevel.Error, k_DeserializationError));
                serializedSuccessfully = false;
            }

            if (serializedSuccessfully)
            {
                Sync.RunNextUpdateOnMain(() =>
                {
                    Sync.SafeAsync(async () =>
                    {
                        // Need to run the validation later on startup as it runs before project linking is finished
                        // so we have no project id
                        if (string.IsNullOrEmpty(CloudProjectSettings.projectId))
                        {
                            await Task.WhenAny(m_ProjectReady, Task.Delay(TimeSpan.FromSeconds(10)));
                            if (!m_ProjectReady.IsCompleted)
                                m_Logger.LogVerbose("Cannot fully validate items because project is not linked");
                        }

                        await ValidateAsync(itemAsset);
                    });
                });
            }
        }

        async Task ValidateAsync(CatalogItemAsset itemAsset)
        {
            if (m_EnvironmentsApi.ActiveEnvironmentId == Guid.Empty)
            {
                return;
            }

            if (DateTime.Now - m_LastFetched > TimeSpan.FromSeconds(10))
            {
                m_LastFetched = DateTime.Now;
                var t = new TaskCompletionSource<bool>();
                m_RemoteResourcesTask = t.Task;
                try
                {
                    var environmentId = m_EnvironmentsApi.ActiveEnvironmentId.ToString();
                    await m_Client.Initialize(environmentId, CloudProjectSettings.projectId, CancellationToken.None);
                    m_RemoteResources = await m_Client.List(CancellationToken.None);
                    t.TrySetResult(true);
                }
                catch (Exception e)
                {
                    t.TrySetException(e);
                }
            }
            else
            {
                await m_RemoteResourcesTask;
            }

            CatalogItem previousItem = m_RemoteResources?.FirstOrDefault(r =>
                r.uSku == itemAsset.CatalogEntryDeploymentItem.CatalogItem.uSku);

            itemAsset.CatalogEntryDeploymentItem.Validate(previousItem);
        }

        void CatalogItemAssetsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems.Cast<CatalogItemAsset>())
                {
                    DeploymentItems.Remove(oldItem.CatalogEntryDeploymentItem);
                    Remove(oldItem);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems.Cast<CatalogItemAsset>())
                {
                    DeploymentItems.Add(newItem.CatalogEntryDeploymentItem);
                    OnNewAsset(newItem);
                }
            }
        }

        CatalogItemAsset RegenAsset(CatalogItemAsset itemAsset)
        {
            var newAsset = ScriptableObject.CreateInstance<CatalogItemAsset>();

            //Keep the DI reference
            newAsset.CatalogEntryDeploymentItem = itemAsset.CatalogEntryDeploymentItem;

            //new asset path hasnt been assigned
            PopulateModel(newAsset, itemAsset.Path);
            return newAsset;
        }

        public CatalogItemAsset GetOrCreateInstance(string assetPath)
        {
            foreach (var a in m_CatalogItemAssets)
            {
                if (assetPath == a.Path)
                {
                    return a == null ? RegenAsset(a) : a;
                }
            }

            var asset = ScriptableObject.CreateInstance<CatalogItemAsset>();
            asset.Path = assetPath;
            PopulateModel(asset);
            return asset;
        }
    }
}
