using System;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class CustomSitemapItemRecordProvider : SitemapItemRecordProviderBase, ISitemapItemRecordProvider
    {
        public CustomSitemapItemRecordProvider(
            ISitemapUrlBuilder urlBuilder)
            : base(urlBuilder)
        {
        }

        public virtual Task LoadSitemapItemRecordsAsync(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var progressInfo = new ExportImportProgressInfo();
            var customOptions = new SitemapItemOptions();
            var customSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsIgnoreCase(SitemapItemTypes.Custom)).ToList();
            var totalCount = customSitemapItems.Count;

            if (totalCount > 0)
            {
                var processedCount = 0;
                progressInfo.Description = $"Custom: Starting records generation for {totalCount} custom items";
                progressCallback?.Invoke(progressInfo);

                foreach (var customSitemapItem in customSitemapItems)
                {
                    customSitemapItem.ItemsRecords = GetSitemapItemRecords(store, customOptions, customSitemapItem.UrlTemplate, baseUrl);
                    processedCount++;
                    progressInfo.Description = $"Custom: Have been generated {processedCount} of {totalCount}  records for custom  items";
                    progressCallback?.Invoke(progressInfo);
                }
            }
            return Task.CompletedTask;
        }
    }
}
