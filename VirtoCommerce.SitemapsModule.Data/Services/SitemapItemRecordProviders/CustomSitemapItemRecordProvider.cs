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

        public virtual void LoadSitemapItemRecords(Sitemap sitemap, string baseUrl)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();
            var customOptions = new SitemapItemOptions();
            var customSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Custom));
            foreach (var customSitemapItem in customSitemapItems)
            {
                customSitemapItem.ItemsRecords = GetSitemapItemRecords(customOptions, customSitemapItem.UrlTemplate, baseUrl);
            }
        }
    }
}