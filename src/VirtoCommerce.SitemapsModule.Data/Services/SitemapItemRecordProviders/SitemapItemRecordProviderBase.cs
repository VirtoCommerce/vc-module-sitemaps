using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CatalogModule.Core.Extensions;
using VirtoCommerce.CatalogModule.Core.Outlines;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Seo.Core.Models;
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

        public IList<SitemapItemRecord> GetSitemapItemRecords(Store store, SitemapItemOptions options, string urlTemplate, string baseUrl, IEntity entity = null)
        {
            return GetSitemapItemRecords(store, options, urlTemplate, baseUrl, entity, parentCategoryId: null);
        }

        public IList<SitemapItemRecord> GetSitemapItemRecords(Store store, SitemapItemOptions options, string urlTemplate, string baseUrl, IEntity entity, string parentCategoryId)
        {
            if (string.IsNullOrEmpty(parentCategoryId) || entity is not IHasOutlines hasOutlines || hasOutlines.Outlines.IsNullOrEmpty())
            {
                var record = GetMainRecord(store, options, urlTemplate, baseUrl, entity, outline: null);
                return record != null ? [record] : [];
            }

            var outlines = hasOutlines.Outlines
                .Where(outline =>
                    outline.Items.ContainsCatalog(store.Catalog) &&
                    outline.Items.ContainsCategory(parentCategoryId));

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

            var result = new SitemapItemRecord
            {
                Url = url,
                ModifiedDate = (entity as AuditableEntity)?.ModifiedDate ?? DateTime.UtcNow,
                UpdateFrequency = options.UpdateFrequency,
                Priority = options.Priority,
            };

            if (entity is ISeoSupport seoSupport)
            {
                var alternateLanguages = seoSupport.SeoInfos
                    .Where(x => x.IsActive && IsMatchingStore(x, store) && IsMatchingLanguage(x, store))
                    .Select(x => x.LanguageCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase);

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


        private static bool IsMatchingStore(SeoInfo seo, Store store)
        {
            return seo.StoreId is null || seo.StoreId.EqualsIgnoreCase(store.Id);
        }

        private static bool IsMatchingLanguage(SeoInfo seo, Store store)
        {
            return !seo.LanguageCode.EqualsIgnoreCase(store.DefaultLanguage) &&
                   store.Languages.Contains(seo.LanguageCode);
        }
    }
}
