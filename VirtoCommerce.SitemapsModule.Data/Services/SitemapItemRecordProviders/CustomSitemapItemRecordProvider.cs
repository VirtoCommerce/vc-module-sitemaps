using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class CustomSitemapItemRecordProvider : SitemapItemRecordProviderBase, ISitemapItemRecordProvider
    {
        public CustomSitemapItemRecordProvider(
            ISitemapUrlBuilder sitemapUrlBuilder,
            ISettingsManager settingsManager)
            : base(settingsManager, sitemapUrlBuilder)
        {
        }

        public virtual ICollection<SitemapItemRecord> GetSitemapItemRecords(Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var customSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Custom));
            foreach (var customSitemapItem in customSitemapItems)
            {
                var sitemapItemRecord = CreateSitemapItemRecords(sitemap, customSitemapItem.UrlTemplate, SitemapItemTypes.Custom).FirstOrDefault();
                if (sitemapItemRecord != null)
                {
                    sitemapItemRecords.Add(sitemapItemRecord);
                }
            }

            return sitemapItemRecords;
        }
    }
}