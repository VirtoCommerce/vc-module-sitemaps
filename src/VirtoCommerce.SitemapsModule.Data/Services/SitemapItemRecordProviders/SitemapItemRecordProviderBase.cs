using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public abstract class SitemapItemRecordProviderBase
    {
        private readonly ISitemapUrlBuilder _urlBuilder;

        protected SitemapItemRecordProviderBase(ISitemapUrlBuilder urlBuilder)
        {
            _urlBuilder = urlBuilder;
        }

        public ICollection<SitemapItemRecord> GetSitemapItemRecords(Store store, SitemapItemOptions options, string urlTemplate, string baseUrl, IEntity entity = null)
        {
            var auditableEntity = entity as AuditableEntity;

            var result = new SitemapItemRecord
            {
                ModifiedDate = auditableEntity?.ModifiedDate ?? DateTime.UtcNow,
                Priority = options.Priority,
                UpdateFrequency = options.UpdateFrequency,
                Url = _urlBuilder.BuildStoreUrl(store, store.DefaultLanguage, urlTemplate, baseUrl, entity)
            };

            if (entity is ISeoSupport seoSupport)
            {
                foreach (var languageCode in seoSupport.SeoInfos.Where(x => x.IsActive).Select(x => x.LanguageCode))
                {
                    if (store.Languages.Contains(languageCode) && !store.DefaultLanguage.EqualsInvariant(languageCode))
                    {
                        var alternate = new SitemapItemAlternateLinkRecord
                        {
                            Language = languageCode,
                            Type = "alternate",
                            Url = _urlBuilder.BuildStoreUrl(store, languageCode, urlTemplate, baseUrl, entity)
                        };
                        result.Alternates.Add(alternate);
                    }
                }
            }

            return new List<SitemapItemRecord> { result };
        }
    }
}
