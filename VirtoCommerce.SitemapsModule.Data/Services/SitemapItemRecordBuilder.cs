using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapItemRecordBuilder : ISitemapItemRecordBuilder
    {
        public SitemapItemRecordBuilder(ISettingsManager settingsManager, ISitemapUrlBuilder sitemapUrlBuilder)
        {
            SettingsManager = settingsManager;
            SitemapUrlBuilder = sitemapUrlBuilder;
        }

        protected ISettingsManager SettingsManager { get; private set; }
        protected ISitemapUrlBuilder SitemapUrlBuilder { get; private set; }

        public virtual ICollection<SitemapItemRecord> CreateSitemapItemRecords(Store store, string urlTemplate, string sitemapItemType, ISeoSupport seoSupportItem = null)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var sitemapItemOptions = GetSitemapItemOptions(sitemapItemType);
            var sitemapItemRecord = new SitemapItemRecord
            {
                ModifiedDate = DateTime.UtcNow,
                Priority = sitemapItemOptions.Priority,
                UpdateFrequency = sitemapItemOptions.UpdateFrequency,
                Url = SitemapUrlBuilder.CreateAbsoluteUrl(store, urlTemplate, store.DefaultLanguage, seoSupportItem != null ? seoSupportItem.Id : null)
            };

            if (seoSupportItem != null && !seoSupportItem.SeoInfos.IsNullOrEmpty())
            {
                var seoInfos = seoSupportItem.SeoInfos.Where(si => si.IsActive && store.Languages.Contains(si.LanguageCode)).ToList();
                foreach (var seoInfo in seoInfos)
                {
                    sitemapItemRecord.Url = SitemapUrlBuilder.CreateAbsoluteUrl(store, urlTemplate, seoInfo.LanguageCode, seoInfo.SemanticUrl);
                    sitemapItemRecords.Add(sitemapItemRecord);
                }
            }
            else
            {
                sitemapItemRecords.Add(sitemapItemRecord);
            }

            return sitemapItemRecords;
        }

        private SitemapItemOptions GetSitemapItemOptions(string sitemapItemType)
        {
            var sitemapItemOptions = new SitemapItemOptions();

            if (sitemapItemType.EqualsInvariant("category"))
            {
                sitemapItemOptions.Priority = SettingsManager.GetValue("Sitemap.CategoryPagePriority", .7M);
                sitemapItemOptions.UpdateFrequency = SettingsManager.GetValue("Sitemap.CategoryPageUpdateFrequency", UpdateFrequency.Weekly);
            }
            else if (sitemapItemType.EqualsInvariant("product"))
            {
                sitemapItemOptions.Priority = SettingsManager.GetValue("Sitemap.ProductPagePriority", 1.0M);
                sitemapItemOptions.UpdateFrequency = SettingsManager.GetValue("Sitemap.ProductPageUpdateFrequency", UpdateFrequency.Daily);
            }

            return sitemapItemOptions;
        }
    }
}