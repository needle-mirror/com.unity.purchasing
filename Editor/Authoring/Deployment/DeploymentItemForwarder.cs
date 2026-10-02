using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Unity.Services.DeploymentApi.Editor;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    static class DeploymentItemForwarder
    {
        public static void Forward(
            ObservableCollection<IDeploymentItem> source,
            ObservableCollection<IDeploymentItem> target)
        {
            var fromSource = new HashSet<IDeploymentItem>();

            foreach (var item in source)
            {
                target.Add(item);
                fromSource.Add(item);
            }

            source.CollectionChanged += (_, eventArgs) =>
            {
                switch (eventArgs.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                        if (eventArgs.OldItems is not null)
                        {
                            foreach (IDeploymentItem oldItem in eventArgs.OldItems)
                            {
                                target.Remove(oldItem);
                                fromSource.Remove(oldItem);
                            }
                        }
                        if (eventArgs.NewItems is not null)
                        {
                            foreach (IDeploymentItem newItem in eventArgs.NewItems)
                            {
                                target.Add(newItem);
                                fromSource.Add(newItem);
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        foreach (var stale in fromSource)
                        {
                            target.Remove(stale);
                        }
                        fromSource.Clear();
                        foreach (var item in source)
                        {
                            target.Add(item);
                            fromSource.Add(item);
                        }
                        break;
                }
            };
        }
    }
}
