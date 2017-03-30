using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.Tools;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class CustomSitemapItemRecordProvider : SitemapItemRecordProviderBase, ISitemapItemRecordProvider
    {
        public CustomSitemapItemRecordProvider(
            IUrlBuilder urlBuilder,
            ISettingsManager settingsManager)
            : base(settingsManager, urlBuilder)
        {
        }

        public virtual void LoadSitemapItemRecords(Store store, Sitemap sitemap, string baseUrl)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();
            var customOptions = new SitemapItemOptions();
            var customSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Custom));
            foreach (var customSitemapItem in customSitemapItems)
            {
                customSitemapItem.ItemsRecords = GetSitemapItemRecords(store, customOptions, customSitemapItem.UrlTemplate, baseUrl);
            }
        }
    }
}