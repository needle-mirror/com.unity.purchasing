using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class CatalogOpenDashboardCommand : Command
    {
        readonly ICatalogDashboardUrlResolver m_DashboardUrlResolver;
        public override string Name => L10n.Tr("Open in Dashboard");

        public CatalogOpenDashboardCommand(ICatalogDashboardUrlResolver dashboardUrlResolver)
        {
            m_DashboardUrlResolver = dashboardUrlResolver;
        }

        public override async Task ExecuteAsync(IEnumerable<IDeploymentItem> items, CancellationToken cancellationToken = default)
        {
            var materialized = items as IReadOnlyCollection<IDeploymentItem> ?? items.ToList();
            var sku = materialized.OfType<CatalogEntryDeploymentItem>().FirstOrDefault()?.CatalogItem?.uSku
                ?? materialized.OfType<CatalogCsvDeploymentItem>()
                    .SelectMany(c => c.EntryDeploymentItems ?? Enumerable.Empty<CatalogEntryDeploymentItem>())
                    .FirstOrDefault()?.CatalogItem?.uSku;
            Application.OpenURL(await m_DashboardUrlResolver.PurchasingDashboardUrlGetter(sku));
        }
    }
}
