using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class PurchasingDeploymentProvider : DeploymentProvider
    {
        public override string Service => L10n.Tr("Purchasing");
        public override Command DeployCommand { get; }
        public Command DeleteRemoteCommand { get; }

        public PurchasingDeploymentProvider(
            DeployCommandWrapper deployCommandWrapper,
            DeleteRemoteCommandWrapper deleteRemoteCommandWrapper,
            CatalogOpenDashboardCommand openDashboardCommand,
            ObservableCatalogItemAssets ucatAssets,
            ObservableCatalogCsvAssets csvAssets)
        {
            DeployCommand = deployCommandWrapper;
            DeleteRemoteCommand = deleteRemoteCommandWrapper;
            Commands.Add(openDashboardCommand);
            Commands.Add(DeleteRemoteCommand);

            Forward(ucatAssets.DeploymentItems);
            Forward(csvAssets.DeploymentItems);
        }

        void Forward(ObservableCollection<IDeploymentItem> source)
        {
            var fromSource = new HashSet<IDeploymentItem>();

            foreach (var item in source)
            {
                DeploymentItems.Add(item);
                fromSource.Add(item);
            }

            source.CollectionChanged += (_, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                        if (e.OldItems is not null)
                        {
                            foreach (IDeploymentItem o in e.OldItems)
                            {
                                DeploymentItems.Remove(o);
                                fromSource.Remove(o);
                            }
                        }
                        if (e.NewItems is not null)
                        {
                            foreach (IDeploymentItem n in e.NewItems)
                            {
                                DeploymentItems.Add(n);
                                fromSource.Add(n);
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        // Defensive: sources do not Clear() today. Drop this source's contributions
                        // then mirror its current state, leaving items from other sources untouched.
                        foreach (var stale in fromSource)
                        {
                            DeploymentItems.Remove(stale);
                        }
                        fromSource.Clear();
                        foreach (var item in source)
                        {
                            DeploymentItems.Add(item);
                            fromSource.Add(item);
                        }
                        break;
                }
            };
        }
    }
}
