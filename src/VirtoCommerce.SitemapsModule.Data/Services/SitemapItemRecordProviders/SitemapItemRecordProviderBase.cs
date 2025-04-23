using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CatalogModule.Core.Extensions;
using VirtoCommerce.CoreModule.Core.Outlines;
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
            var record = GetMainRecord(store, options, urlTemplate, baseUrl, entity, outline: null);

            return record != null ? [record] : [];
        }

        public IList<SitemapItemRecord> GetSitemapItemRecords(Store store, SitemapItemOptions options, string urlTemplate, string baseUrl, IEntity entity, string parentCategoryId)
        {
            if (string.IsNullOrEmpty(parentCategoryId) || entity is not IHasOutlines hasOutlines || hasOutlines.Outlines.IsNullOrEmpty())
            {
                return [GetMainRecord(store, options, urlTemplate, baseUrl, entity, outline: null)];
            }

            var outlines = hasOutlines.Outlines
                .Where(outline =>
                    outline.Items.Any(item =>
                        item.IsCategory() && item.Id.EqualsIgnoreCase(parentCategoryId)));

            return outlines
                .Select(outline => GetMainRecord(store, options, urlTemplate, baseUrl, entity, outline))
                .Where(record => record != null)
                .ToList();
        }

        public SitemapItemRecord GetMainRecord(Store store, SitemapItemOptions options, string urlTemplate, string baseUrl, IEntity entity, Outline outline)
        {
            var url = _urlBuilder.BuildStoreUrl(store, store.DefaultLanguage, urlTemplate, baseUrl, entity, outline);

            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            var auditableEntity = entity as AuditableEntity;

            var result = new SitemapItemRecord
            {
                ModifiedDate = auditableEntity?.ModifiedDate ?? DateTime.UtcNow,
                Priority = options.Priority,
                UpdateFrequency = options.UpdateFrequency,
                Url = url,
            };

            if (entity is ISeoSupport seoSupport)
            {
                var alternateLanguages = seoSupport.SeoInfos
                    .Where(seo => seo.IsActive)
                    .Select(seo => seo.LanguageCode)
                    .Where(language => store.Languages.Contains(language) && !store.DefaultLanguage.EqualsIgnoreCase(language));

                var alternateRecords = alternateLanguages
                    .Select(x => GetAlternateRecord(store, x, urlTemplate, baseUrl, entity, outline))
                    .Where(x => x != null);

                result.Alternates.AddRange(alternateRecords);
            }

            return result;
        }

        public SitemapItemAlternateLinkRecord GetAlternateRecord(Store store, string language, string urlTemplate, string baseUrl, IEntity entity, Outline outline)
        {
            var url = _urlBuilder.BuildStoreUrl(store, language, urlTemplate, baseUrl, entity, outline);

            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            return new SitemapItemAlternateLinkRecord
            {
                Url = url,
                Language = language,
                Type = "alternate",
            };
        }
    }
}
