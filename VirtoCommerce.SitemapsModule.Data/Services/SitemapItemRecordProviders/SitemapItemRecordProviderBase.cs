using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Data.Converters;
using VirtoCommerce.SitemapsModule.Data.Extensions;
using VirtoCommerce.Tools;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public abstract class SitemapItemRecordProviderBase
    {
        public SitemapItemRecordProviderBase(ISettingsManager settingsManager, IUrlBuilder urlBuilider)
        {
            SettingsManager = settingsManager;
            UrlBuilder = urlBuilider;
        }

        protected ISettingsManager SettingsManager { get; private set; }
        protected IUrlBuilder UrlBuilder { get; private set; }

        public ICollection<SitemapItemRecord> GetSitemapItemRecords(Store store, SitemapItemOptions options, string urlTemplate, string baseUrl, IEntity entity = null)
        {            
            var auditableEntity = entity as AuditableEntity;
         
            var result = new SitemapItemRecord
            {
                ModifiedDate = auditableEntity != null ? auditableEntity.ModifiedDate.Value : DateTime.UtcNow,
                Priority = options.Priority,
                UpdateFrequency = options.UpdateFrequency,
                Url = GetSemanticUrl(store, store.DefaultLanguage, urlTemplate, baseUrl, entity)
            };
            var seoSupport = entity as ISeoSupport;
            if (seoSupport != null)
            {
                foreach (var seoInfo in seoSupport.SeoInfos.Where(x => x.IsActive))
                {
                    if (store.Languages.Contains(seoInfo.LanguageCode) && !store.DefaultLanguage.EqualsInvariant(seoInfo.LanguageCode))
                    {
                        var alternate = new SitemapItemAlternateLinkRecord
                        {
                            Language = seoInfo.LanguageCode,
                            Type = "alternate",
                            Url = GetSemanticUrl(store, seoInfo.LanguageCode, urlTemplate, baseUrl, entity)
                        };
                        result.Alternates.Add(alternate);
                    }
                }
            }
            return new[] { result }.ToList();
        }

        protected virtual string GetSemanticUrl(Store store, string language, string urlTemplate, string baseUrl, IEntity entity)
        {
            var toolsStore = store.ToToolsStore(baseUrl);
                    
            var seoSupport = entity as ISeoSupport;
            var realativeUrl = urlTemplate;

            if (seoSupport != null)
            {
                var hasOutlines = entity as IHasOutlines;
                var seoInfos = seoSupport.SeoInfos.Select(x => x.JsonConvert<Tools.Models.SeoInfo>());
                seoInfos = seoInfos.GetBestMatchingSeoInfos(toolsStore.Id, toolsStore.DefaultLanguage, language, null);
                if (!seoInfos.IsNullOrEmpty())
                {
                    realativeUrl = seoInfos.Select(x => x.SemanticUrl).FirstOrDefault();
                }
                if (hasOutlines != null && !hasOutlines.Outlines.IsNullOrEmpty())
                {
                    var outlines = hasOutlines.Outlines.Select(x => x.JsonConvert<Tools.Models.Outline>());
                    realativeUrl = outlines.GetSeoPath(toolsStore, language, realativeUrl);
                }
            }
            var toolsContext = new Tools.Models.UrlBuilderContext
            {
                AllStores = new[] { toolsStore },
                CurrentLanguage = language,
                CurrentStore = toolsStore
            };
            return UrlBuilder.BuildStoreUrl(toolsContext, realativeUrl);
        }               

       
    }
}