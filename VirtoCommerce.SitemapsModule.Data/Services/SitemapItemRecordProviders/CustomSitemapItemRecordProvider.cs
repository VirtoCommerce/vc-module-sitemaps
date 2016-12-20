using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class CustomSitemapItemRecordProvider : ISitemapItemRecordProvider
    {
        public CustomSitemapItemRecordProvider(ISitemapItemRecordBuilder sitemapItemRecordBuilder)
        {
            SitemapItemRecordBuilder = sitemapItemRecordBuilder;
        }

        protected ISitemapItemRecordBuilder SitemapItemRecordBuilder { get; private set; }

        public virtual ICollection<SitemapItemRecord> GetSitemapItemRecords(Store store, Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var customSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Custom));
            foreach (var customSitemapItem in customSitemapItems)
            {
                var sitemapItemRecord = SitemapItemRecordBuilder.CreateSitemapItemRecords(store, customSitemapItem.UrlTemplate, SitemapItemTypes.Custom).FirstOrDefault();
                if (sitemapItemRecord != null)
                {
                    sitemapItemRecords.Add(sitemapItemRecord);
                }
            }

            return sitemapItemRecords;
        }
    }
}