using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Logger;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using UnityEditor.Purchasing.Editor.Authoring.Core.Validations;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Deploy
{
    class CatalogDeploymentHandler : CatalogFetchDeployBase, ICatalogDeploymentHandler
    {
        public CatalogDeploymentHandler(ILiveContentConfigClient client, ILogger logger)
            : base(client, logger) {}

        public async Task<DeployResult> DeployAsync(
            IReadOnlyList<CatalogEntryDeploymentItem> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new DeployResult();

            var remoteResources = await GetRemoteItems(localResources, cancellationToken: token);

            var filteredLocalResources = FilterInvalidItems(localResources, remoteResources);

            SetupMaps(filteredLocalResources, remoteResources);

            var toCreate = filteredLocalResources
                .Where(DoesNotExistRemotely)
                .ToList();

            var toUpdate = filteredLocalResources
                .Where(ExistsRemotely)
                .ToList();

            var remoteOnlyResources = remoteResources
                .Where(DoesNotExistLocally)
                .ToList();

            var toDelete = new List<CatalogEntryDeploymentItem>();
            if (reconcile)
            {
                toDelete = remoteOnlyResources;
            }

            res.Deployed = localResources.Concat(toDelete).ToList();

            if (dryRun)
            {
                UpdateDryRunResult(toUpdate, toDelete, toCreate);
                return res;
            }

            if (!filteredLocalResources.Any())
                return res;

            filteredLocalResources.ForEach(l => l.Progress = 50);
            filteredLocalResources.ForEach(l => l.Status = Statuses.GetDeploying());

            const int maxConcurrency = 8;
            using var gate = new SemaphoreSlim(maxConcurrency);

            var upsertTasks = filteredLocalResources.Select(async item =>
            {
                await gate.WaitAsync(token);
                try
                {
                    await Client.Upsert(item.CatalogItem, token);
                    item.Progress = 100;
                    item.Status = toCreate.Contains(item)
                        ? DeploymentStatus.GetDeployed("Created")
                        : DeploymentStatus.GetDeployed("Updated");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ClientException e)
                {
                    item.Status = DeploymentStatus.GetFailedToDeploy(
                        $"Failed to deploy item. An error happened while communicating with the server. {e.Message}");
                    Logger.LogError(e);
                }
                catch (Exception e)
                {
                    item.Status = DeploymentStatus.GetFailedToDeploy(
                        $"Failed to deploy item. An unexpected error happened. Reason: {e.Message}");
                    Logger.LogError(e);
                }
                finally
                {
                    gate.Release();
                }
            }).ToList();

            await Task.WhenAll(upsertTasks);

            if (reconcile && toDelete.Any())
            {
                using var deleteGate = new SemaphoreSlim(maxConcurrency);
                var deleteTasks = toDelete.Select(async item =>
                {
                    await deleteGate.WaitAsync(token);
                    try
                    {
                        await Client.Delete(item.CatalogItem, token);
                        item.Status = DeploymentStatus.GetDeployed("Deleted");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (ClientException e)
                    {
                        item.Status = DeploymentStatus.GetFailedToDeploy(
                            $"Failed to delete item. An error happened while communicating with the server. {e.Message}");
                        Logger.LogError(e);
                    }
                    catch (Exception e)
                    {
                        item.Status = DeploymentStatus.GetFailedToDeploy(
                            $"Failed to delete item. An unexpected error happened. Reason: {e.Message}");
                        Logger.LogError(e);
                    }
                    finally
                    {
                        deleteGate.Release();
                    }
                }).ToList();

                await Task.WhenAll(deleteTasks);
            }

            return res;
        }

        List<CatalogEntryDeploymentItem> FilterInvalidItems(
            IReadOnlyList<CatalogEntryDeploymentItem> localResources,
            IReadOnlyList<CatalogEntryDeploymentItem> remoteResources)
        {
            var filteredLocalResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            UpdateDuplicateResourceStatus(duplicateGroups);

            //Validate and return only those that are valid
            filteredLocalResources = filteredLocalResources.Where(f =>
            {
                var previousResource = remoteResources.FirstOrDefault(r =>
                    r.CatalogItem.CatalogListingId == f.CatalogItem.CatalogListingId)?.CatalogItem;
                var valid = f.Validate(previousResource);
                if (!valid)
                    f.Status = DeploymentStatus.GetFailedToDeploy("Catalog item is invalid and will not be deployed");
                return valid;
            }).ToList();
            return filteredLocalResources;
        }

        protected override DeploymentStatus GetSuccessStatus(string message)
        {
            return Statuses.GetDeployed(message);
        }

        protected override DeploymentStatus GetFailedStatus(string message, IReadOnlyList<IDeploymentItem> failedItems = null)
        {
            return Statuses.GetFailedToDeploy(message);
        }
    }
}
