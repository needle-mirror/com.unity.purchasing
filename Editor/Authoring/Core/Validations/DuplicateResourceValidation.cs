using System.Collections.Generic;
using System.Linq;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
namespace UnityEditor.Purchasing.Editor.Authoring.Core.Validations
{
    static class DuplicateResourceValidation
    {
        public static List<T> FilterDuplicateResources<T>(
            IReadOnlyList<T> resources,
            out IReadOnlyList<IGrouping<string, T>> duplicateGroups)
            where T : CatalogEntryDeploymentItem
        {
            duplicateGroups = resources
                .GroupBy(r => r.CatalogItem.CatalogListingId)
                .Where(g => g.Count() > 1)
                .ToList();

            var hashset = new HashSet<string>(duplicateGroups.Select(g => g.Key));

            return resources
                .Where(r => !hashset.Contains(r.CatalogItem.CatalogListingId))
                .ToList();
        }

        public static (string, string) GetDuplicateResourceErrorMessages(
            CatalogEntryDeploymentItem targetCatalogEntryDeploymentItem,
            IReadOnlyList<CatalogEntryDeploymentItem> group)
        {
            var duplicates = group
                .Except(new[] { targetCatalogEntryDeploymentItem })
                .ToList();

            var duplicatesStr = string.Join(", ", duplicates.Select(d => $"'{d.Path}'"));
            var shortMessage = $"'{targetCatalogEntryDeploymentItem.Path}' was found duplicated in other files: {duplicatesStr}";
            var message = $"Multiple resources with the same catalog listing ID '{targetCatalogEntryDeploymentItem.CatalogItem.CatalogListingId}' were found. "
                + "Only a single resource for a given catalog listing ID may be deployed/fetched at the same time. "
                + "Give all resources unique catalog listing IDs or deploy/fetch them separately to proceed.\n"
                + shortMessage;
            return (shortMessage, message);
        }
    }
}
