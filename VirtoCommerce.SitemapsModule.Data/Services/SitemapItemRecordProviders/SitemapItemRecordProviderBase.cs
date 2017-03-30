using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
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
            var sitemapItemRecords = new List<SitemapItemRecord>();
            var urlBuilderStore = ToUrlBuilderStore(store, baseUrl);
            var urlBuilderContext = new Tools.Models.UrlBuilderContext
            {
                AllStores = new[] { urlBuilderStore },
                CurrentLanguage = urlBuilderStore.DefaultLanguage,
                CurrentStore = urlBuilderStore
            };

            if (entity != null)
            {
                if (entity is IHasOutlines)
                {
                    // Categories and products
                    var sitemapItemRecord = GetOutlineSupportItemRecord(urlBuilderContext, entity as IHasOutlines, options);
                    if (sitemapItemRecord != null)
                    {
                        sitemapItemRecords.Add(sitemapItemRecord);
                    }
                }
                else if (entity is ISeoSupport)
                {
                    // Vendors
                    var sitemapItemRecord = GetSeoSupportItemRecord(urlBuilderContext, entity as ISeoSupport, options);
                    sitemapItemRecords.Add(sitemapItemRecord);
                }
            }
            else
            {
                // Pages and custom items
                var sitemapItemRecord = GetCustomItemRecord(urlBuilderContext, entity, options, urlTemplate);
                sitemapItemRecords.Add(sitemapItemRecord);
            }

            return sitemapItemRecords;
        }

        private SitemapItemRecord GetOutlineSupportItemRecord(Tools.Models.UrlBuilderContext context, IHasOutlines entity, SitemapItemOptions options)
        {
            SitemapItemRecord sitemapItemRecord = null;

            if (entity.Outlines != null)
            {
                var urlBuilderOutlines = ToUrlBuilderOutlines(entity.Outlines);
                var defaultRelativeUrl = urlBuilderOutlines.GetSeoPath(context.CurrentStore, context.CurrentLanguage, null);
                var defaultAbsoluteUrl = UrlBuilder.BuildStoreUrl(context, defaultRelativeUrl);
                sitemapItemRecord = BuildSitemapItemRecord(entity as AuditableEntity, options, defaultAbsoluteUrl);

                foreach (var language in context.CurrentStore.Languages)
                {
                    var languageOutline = urlBuilderOutlines.FirstOrDefault(o => o.Items.Where(i => i.SeoInfos.Any()).All(i => i.SeoInfos.Any(si => si.LanguageCode == language)));
                    if (languageOutline != null && language != context.CurrentStore.DefaultLanguage)
                    {
                        var relativeUrl = new[] { languageOutline }.GetSeoPath(context.CurrentStore, language, null);
                        context.CurrentLanguage = language;
                        var absoluteUrl = UrlBuilder.BuildStoreUrl(context, relativeUrl);
                        var alternate = BuildSitemapItemAlternateLinkRecord(language, absoluteUrl);
                        sitemapItemRecord.Alternates.Add(alternate);
                    }
                }
            }

            return sitemapItemRecord;
        }

        private SitemapItemRecord GetSeoSupportItemRecord(Tools.Models.UrlBuilderContext context, ISeoSupport entity, SitemapItemOptions options)
        {
            var defaultSeoInfo = entity.SeoInfos.FirstOrDefault(si => si.LanguageCode == context.CurrentStore.DefaultLanguage);
            var defaultAbsoluteUrl = UrlBuilder.BuildStoreUrl(context, defaultSeoInfo.SemanticUrl);
            var sitemapItemRecord = BuildSitemapItemRecord(entity as AuditableEntity, options, defaultAbsoluteUrl);

            var seoInfos = entity.SeoInfos.Where(si => si.IsActive && context.CurrentStore.Languages.Contains(si.LanguageCode)).Except(new[] { defaultSeoInfo });
            foreach (var seoInfo in seoInfos)
            {
                if (seoInfo.LanguageCode != defaultSeoInfo.LanguageCode)
                {
                    context.CurrentLanguage = seoInfo.LanguageCode;
                    var absoluteUrl = UrlBuilder.BuildStoreUrl(context, seoInfo.SemanticUrl);
                    var alternate = BuildSitemapItemAlternateLinkRecord(seoInfo.LanguageCode, absoluteUrl);
                    sitemapItemRecord.Alternates.Add(alternate);
                }
            }

            return sitemapItemRecord;
        }

        private SitemapItemRecord GetCustomItemRecord(Tools.Models.UrlBuilderContext context, IEntity entity, SitemapItemOptions options, string relativeUrl)
        {
            var absoluteUrl = UrlBuilder.BuildStoreUrl(context, relativeUrl);
            var sitemapItemRecord = BuildSitemapItemRecord(entity as AuditableEntity, options, absoluteUrl);

            return sitemapItemRecord;
        }

        private SitemapItemRecord BuildSitemapItemRecord(AuditableEntity auditableEntity, SitemapItemOptions options, string absoluteUrl)
        {
            return new SitemapItemRecord
            {
                ModifiedDate = auditableEntity != null ? auditableEntity.ModifiedDate.Value : DateTime.UtcNow,
                Priority = options.Priority,
                UpdateFrequency = options.UpdateFrequency,
                Url = absoluteUrl
            };
        }

        private SitemapItemAlternateLinkRecord BuildSitemapItemAlternateLinkRecord(string language, string absoluteUrl)
        {
            return new SitemapItemAlternateLinkRecord
            {
                Language = language,
                Type = "alternate",
                Url = absoluteUrl
            };
        }

        private Tools.Models.Store ToUrlBuilderStore(Store store, string baseUrl)
        {
            return new Tools.Models.Store
            {
                Catalog = store.Catalog,
                DefaultLanguage = store.DefaultLanguage,
                Id = store.Id,
                Languages = store.Languages.ToList(),
                SecureUrl = !string.IsNullOrEmpty(store.SecureUrl) ? store.SecureUrl : baseUrl,
                SeoLinksType = GetSeoLinksType(store),
                Url = !string.IsNullOrEmpty(store.Url) ? store.Url : baseUrl
            };
        }

        private ICollection<Tools.Models.Outline> ToUrlBuilderOutlines(ICollection<Outline> outlines)
        {
            var urlBuilderOutlines = new List<Tools.Models.Outline>();
            foreach (var outline in outlines)
            {
                var urlBuilderOutline = new Tools.Models.Outline
                {
                    Items = new List<Tools.Models.OutlineItem>()
                };
                foreach (var outlineItem in outline.Items)
                {
                    var urlBuilderOutlineItem = new Tools.Models.OutlineItem
                    {
                        HasVirtualParent = outlineItem.HasVirtualParent,
                        Id = outlineItem.Id,
                        SeoObjectType = outlineItem.SeoObjectType,
                        SeoInfos = new List<Tools.Models.SeoInfo>()
                    };
                    foreach (var seoInfo in outlineItem.SeoInfos)
                    {
                        urlBuilderOutlineItem.SeoInfos.Add(new Tools.Models.SeoInfo
                        {
                            IsActive = seoInfo.IsActive,
                            LanguageCode = seoInfo.LanguageCode,
                            ObjectId = seoInfo.ObjectId,
                            ObjectType = seoInfo.ObjectType,
                            SemanticUrl = seoInfo.SemanticUrl,
                            StoreId = seoInfo.StoreId
                        });
                    }
                    urlBuilderOutline.Items.Add(urlBuilderOutlineItem);
                }
                urlBuilderOutlines.Add(urlBuilderOutline);
            }

            return urlBuilderOutlines;
        }

        private Tools.Models.SeoLinksType GetSeoLinksType(Store store)
        {
            var seoLinksType = Tools.Models.SeoLinksType.Collapsed;

            if (store.Settings != null)
            {
                var seoLinksTypeSetting = store.Settings.FirstOrDefault(s => s.Name == "Stores.SeoLinksType");
                if (seoLinksTypeSetting != null)
                {
                    seoLinksType = EnumUtility.SafeParse(seoLinksTypeSetting.Value, Tools.Models.SeoLinksType.Collapsed);
                }
            }

            return seoLinksType;
        }
    }
}