using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public abstract class SitemapItemRecordProviderBase
    {
        public SitemapItemRecordProviderBase(ISettingsManager settingsManager, ISitemapUrlBuilder sitemapUrlBuilder)
        {
            SettingsManager = settingsManager;
            SitemapUrlBuilder = sitemapUrlBuilder;
        }

        protected ISettingsManager SettingsManager { get; private set; }
        protected ISitemapUrlBuilder SitemapUrlBuilder { get; private set; }

        public ICollection<SitemapItemRecord> GetSitemapItemRecords(SitemapItemOptions options, string urlTemplate, string baseUrl, ISeoSupport seoSupportObj = null)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();
            if (seoSupportObj != null)
            {
                var seoInfos = seoSupportObj.SeoInfos.Where(x => x.IsActive).ToList();
                foreach (var seoInfo in seoInfos)
                {
                    var record = GetNewRecord(options, urlTemplate, baseUrl);
                    record.Url = SitemapUrlBuilder.CreateAbsoluteUrl(urlTemplate, baseUrl, seoInfo.LanguageCode, seoInfo.SemanticUrl);
                    sitemapItemRecords.Add(record);
                }
            }
            else
            {
                sitemapItemRecords.Add(GetNewRecord(options, urlTemplate, baseUrl));
            }

            return sitemapItemRecords;
        }

        private SitemapItemRecord GetNewRecord(SitemapItemOptions options, string urlTemplate, string baseUrl, ISeoSupport seoSupportObj = null)
        {
            var auditableEntity = seoSupportObj as AuditableEntity;
            return new SitemapItemRecord
            {
                ModifiedDate = auditableEntity != null ? auditableEntity.ModifiedDate.Value : DateTime.UtcNow,
                Priority = options.Priority,
                UpdateFrequency = options.UpdateFrequency,
                Url = SitemapUrlBuilder.CreateAbsoluteUrl(urlTemplate, baseUrl)
            };
        }
    }
}