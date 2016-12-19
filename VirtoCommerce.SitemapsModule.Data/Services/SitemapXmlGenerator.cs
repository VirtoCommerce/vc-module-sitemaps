using System;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using System.Collections.Generic;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.Domain.Commerce.Model;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Customer.Services;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.SitemapsModule.Data.Models.Xml;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapXmlGenerator : ISitemapXmlGenerator
    {
        public SitemapXmlGenerator(
            ISitemapService sitemapService,
            ISitemapItemService sitemapItemService,
            IStoreService storeService,
            IItemService itemService,
            ICategoryService categoryService,
            ICatalogSearchService catalogSearchService,
            IMemberService memberService,
            ISettingsManager settingsManager)
        {
            SitemapService = sitemapService;
            SitemapItemService = sitemapItemService;
            StoreService = storeService;
            ItemService = itemService;
            CategoryService = categoryService;
            CatalogSearchService = catalogSearchService;
            MemberService = memberService;
            SettingsManager = settingsManager;

            _sitemapOptions = GetSitemapOptions();
        }

        private SitemapOptions _sitemapOptions;

        protected ISitemapService SitemapService { get; private set; }
        protected ISitemapItemService SitemapItemService { get; private set; }
        protected IStoreService StoreService { get; private set; }
        protected IItemService ItemService { get; private set; }
        protected ICategoryService CategoryService { get; private set; }
        protected ICatalogSearchService CatalogSearchService { get; private set; }
        protected IMemberService MemberService { get; private set; }
        protected ISettingsManager SettingsManager { get; private set; }

        public virtual SitemapIndexRecord GetSitemapSchema(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                throw new ArgumentException("storeId");
            }

            var sitemapSchema = new SitemapIndexRecord();

            var store = StoreService.GetById(storeId);
            if (store != null)
            {
                var sitemaps = GetSitemaps(store);
                foreach (var sitemap in sitemaps)
                {
                    sitemap.Items = GetFormalSitemapItems(sitemap.Id);

                    var sitemapIndexItemRecords = GetSitemapIndexItemRecords(store, sitemap);
                    sitemapSchema.Sitemaps.AddRange(sitemapIndexItemRecords);
                }
            }

            return sitemapSchema;
        }

        public virtual Stream GenerateSitemapXml(string storeId, SitemapIndexRecord sitemapSchema, string sitemapFilename)
        {
            var stream = new MemoryStream();

            XmlSerializer xmlSerializer = null;
            var xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add("", "http://www.sitemaps.org/schemas/sitemap/0.9");

            var store = StoreService.GetById(storeId);
            if (store != null)
            {
                if (sitemapFilename.EqualsInvariant("sitemap.xml"))
                {
                    xmlSerializer = new XmlSerializer(sitemapSchema.GetType());
                    xmlSerializer.Serialize(stream, sitemapSchema, xmlNamespaces);
                }
                else
                {
                    int partNumber = 0;
                    var sitemapPart = sitemapFilename.Replace(".xml", "").Split(new[] { _sitemapOptions.FilenameSeparator }, StringSplitOptions.None).LastOrDefault();
                    int.TryParse(sitemapPart, out partNumber);
                    var sitemapIndexItemRecord = sitemapSchema.Sitemaps.FirstOrDefault(s => s.Filename == sitemapFilename);
                    if (sitemapIndexItemRecord != null)
                    {
                        var sitemap = SitemapService.GetById(sitemapIndexItemRecord.SitemapId);
                        if (sitemap != null)
                        {
                            sitemap.Items = GetFormalSitemapItems(sitemapIndexItemRecord.SitemapId);
                            var sitemapItemRecords = GetSitemapItemRecords(store, sitemap);
                            var sitemapRecord = new SitemapRecord();
                            if (partNumber == 0)
                            {
                                sitemapRecord.Items = sitemapItemRecords.ToList();
                            }
                            else
                            {
                                sitemapRecord.Items = sitemapItemRecords.Skip((partNumber - 1) * _sitemapOptions.RecordsLimitPerFile).Take(_sitemapOptions.RecordsLimitPerFile).ToList();
                            }

                            xmlSerializer = new XmlSerializer(sitemapRecord.GetType());
                            xmlSerializer.Serialize(stream, sitemapRecord, xmlNamespaces);
                        }
                    }
                }
            }

            stream.Position = 0;

            return stream;
        }

        private ICollection<Sitemap> GetSitemaps(Store store)
        {
            var sitemaps = new List<Sitemap>();

            var sitemapSearchCriteria = new SitemapSearchCriteria
            {
                StoreId = store.Id,
                Skip = 0,
                Take = _sitemapOptions.RecordsLimitPerFile
            };
            var sitemapSearchResult = SitemapService.Search(sitemapSearchCriteria);
            var partsCount = sitemapSearchResult.TotalCount / _sitemapOptions.RecordsLimitPerFile + 1;
            for (var i = 1; i <= partsCount; i++)
            {
                sitemaps.AddRange(sitemapSearchResult.Results);
                if (partsCount > 1)
                {
                    sitemapSearchCriteria.Skip = _sitemapOptions.RecordsLimitPerFile * i;
                    sitemapSearchResult = SitemapService.Search(sitemapSearchCriteria);
                }
            }

            return sitemaps;
        }

        private ICollection<SitemapItem> GetFormalSitemapItems(string sitemapId)
        {
            var formalSitemapItems = new List<SitemapItem>();

            var sitemapItemSearchCriteria = new SitemapItemSearchCriteria
            {
                SitemapId = sitemapId,
                Skip = 0,
                Take = _sitemapOptions.RecordsLimitPerFile
            };
            var sitemapItemSearchResult = SitemapItemService.Search(sitemapItemSearchCriteria);
            var partsCount = sitemapItemSearchResult.TotalCount / _sitemapOptions.RecordsLimitPerFile + 1;
            for (var i = 1; i <= partsCount; i++)
            {
                formalSitemapItems.AddRange(sitemapItemSearchResult.Results);
                if (partsCount > 1)
                {
                    sitemapItemSearchCriteria.Skip = _sitemapOptions.RecordsLimitPerFile * i;
                    sitemapItemSearchResult = SitemapItemService.Search(sitemapItemSearchCriteria);
                }
            }

            return formalSitemapItems;
        }

        private ICollection<SitemapIndexItemRecord> GetSitemapIndexItemRecords(Store store, Sitemap sitemap)
        {
            var sitemapRecords = new List<SitemapIndexItemRecord>();

            var sitemapItemRecords = GetSitemapItemRecords(store, sitemap);
            var partsCount = sitemapItemRecords.Count() / _sitemapOptions.RecordsLimitPerFile + 1;
            for (var i = 1; i <= partsCount; i++)
            {
                var filename = partsCount > 1 ?
                    string.Format("{0}{1}{2}.xml", sitemap.Filename.Replace(".xml", ""), _sitemapOptions.FilenameSeparator, i) :
                    sitemap.Filename;
                sitemapRecords.Add(new SitemapIndexItemRecord
                {
                    Filename = filename,
                    ModifiedDate = DateTime.UtcNow,
                    SitemapId = sitemap.Id,
                    Url = GetAbsoluteUrl(store, sitemap.UrlTemplate, null, filename)
                });
            }

            return sitemapRecords;
        }

        private IEnumerable<SitemapItemRecord> GetSitemapItemRecords(Store store, Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var catalogSitemapItems = sitemap.Items.Where(si =>
                si.ObjectType.EqualsInvariant(SitemapItemTypes.Category) || si.ObjectType.EqualsInvariant(SitemapItemTypes.Product));
            if (catalogSitemapItems.Any())
            {
                var catalogSitemapItemRecords = GetCatalogSitemapItemRecords(store, sitemap);
                sitemapItemRecords.AddRange(catalogSitemapItemRecords);
            }

            var vendorSitemapItemRecords = GetVendorSitemapItemRecords(store, sitemap);
            sitemapItemRecords.AddRange(vendorSitemapItemRecords);

            var customSitemapItemRecords = GetCustomSitemapItemRecords(store, sitemap);
            sitemapItemRecords.AddRange(customSitemapItemRecords);

            return sitemapItemRecords.GroupBy(m => m.Url).Select(i => i.First());
        }

        private ICollection<SitemapItemRecord> GetCatalogSitemapItemRecords(Store store, Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var categorySitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Category));
            var categoryIds = categorySitemapItems.Select(si => si.ObjectId).ToArray();
            var categories = CategoryService.GetByIds(categoryIds, CategoryResponseGroup.WithSeo);
            foreach (var category in categories)
            {
                var categorySitemapItemRecords = CreateSitemapItemRecords(store, sitemap.UrlTemplate, SitemapItemTypes.Category, category);
                sitemapItemRecords.AddRange(categorySitemapItemRecords);
            }

            var productSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Product));
            var productIds = productSitemapItems.Select(si => si.ObjectId).ToArray();
            var products = ItemService.GetByIds(productIds, ItemResponseGroup.Seo);
            foreach (var product in products)
            {
                var productSitemapItemRecords = CreateSitemapItemRecords(store, sitemap.UrlTemplate, SitemapItemTypes.Product, product);
                sitemapItemRecords.AddRange(productSitemapItemRecords);
            }

            var catalogSearchCriteria = new Domain.Catalog.Model.SearchCriteria
            {
                CatalogId = store.Catalog,
                CategoryIds = categoryIds,
                ResponseGroup = SearchResponseGroup.WithCategories | SearchResponseGroup.WithProducts,
                Skip = 0,
                Take = _sitemapOptions.SearchBunchSize
            };
            var catalogSearchResult = CatalogSearchService.Search(catalogSearchCriteria);
            foreach (var category in catalogSearchResult.Categories)
            {
                var categorySitemapItemRecords = CreateSitemapItemRecords(store, sitemap.UrlTemplate, SitemapItemTypes.Category, category);
                sitemapItemRecords.AddRange(categorySitemapItemRecords);
            }

            var partsCount = catalogSearchResult.ProductsTotalCount / _sitemapOptions.SearchBunchSize + 1;
            var cbProducts = new ConcurrentBag<CatalogProduct>();
            Parallel.For(1, partsCount + 1, new ParallelOptions { MaxDegreeOfParallelism = 5 }, i =>
            {
                foreach (var product in catalogSearchResult.Products)
                {
                    cbProducts.Add(product);
                }
                if (partsCount > 1)
                {
                    catalogSearchCriteria.Skip = _sitemapOptions.SearchBunchSize * i;
                    catalogSearchResult = CatalogSearchService.Search(catalogSearchCriteria);
                }
            });
            foreach (var product in cbProducts)
            {
                var productSitemapItemRecords = CreateSitemapItemRecords(store, sitemap.UrlTemplate, SitemapItemTypes.Product, product);
                sitemapItemRecords.AddRange(productSitemapItemRecords);
            }

            return sitemapItemRecords;
        }

        private ICollection<SitemapItemRecord> GetVendorSitemapItemRecords(Store store, Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var vendorSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Custom));
            var vendorIds = vendorSitemapItems.Select(si => si.ObjectId).ToArray();
            var members = MemberService.GetByIds(vendorIds);
            foreach (var member in members)
            {
                var vendor = member as Vendor;
                if (vendor != null)
                {
                    var vendorSitemapItemRecords = CreateSitemapItemRecords(store, sitemap.UrlTemplate, SitemapItemTypes.Vendor, vendor);
                    sitemapItemRecords.AddRange(vendorSitemapItemRecords);
                }
            }

            return sitemapItemRecords;
        }

        private ICollection<SitemapItemRecord> GetCustomSitemapItemRecords(Store store, Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var customSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Custom));
            foreach (var customSitemapItem in customSitemapItems)
            {
                var sitemapItemRecord = CreateSitemapItemRecords(store, customSitemapItem.UrlTemplate, SitemapItemTypes.Custom).FirstOrDefault();
                sitemapItemRecords.Add(sitemapItemRecord);
            }

            return sitemapItemRecords;
        }

        private ICollection<SitemapItemRecord> CreateSitemapItemRecords(Store store, string urlTemplate, string sitemapItemType, ISeoSupport seoSupportItem = null)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var sitemapItemOptions = GetSitemapItemOptions(sitemapItemType);
            var sitemapItemRecord = new SitemapItemRecord
            {
                ModifiedDate = DateTime.UtcNow,
                Priority = sitemapItemOptions.Priority,
                UpdateFrequency = sitemapItemOptions.UpdateFrequency,
                Url = GetAbsoluteUrl(store, urlTemplate, store.DefaultLanguage, seoSupportItem != null ? seoSupportItem.Id : null)
            };

            if (seoSupportItem != null && !seoSupportItem.SeoInfos.IsNullOrEmpty())
            {
                var seoInfos = seoSupportItem.SeoInfos.Where(si => si.IsActive && store.Languages.Contains(si.LanguageCode)).ToList();
                foreach (var seoInfo in seoInfos)
                {
                    sitemapItemRecord.Url = GetAbsoluteUrl(store, urlTemplate, seoInfo.LanguageCode, seoInfo.SemanticUrl);
                    sitemapItemRecords.Add(sitemapItemRecord);
                }
            }
            else
            {
                sitemapItemRecords.Add(sitemapItemRecord);
            }

            return sitemapItemRecords;
        }

        private string GetAbsoluteUrl(Store store, string urlTemplate, string language = null, string semanticUrl = null)
        {
            string relativeUrl = urlTemplate;
            if (urlTemplate.IsAbsoluteUrl())
            {
                return urlTemplate;
            }

            if (!string.IsNullOrEmpty(store.Url))
            {
                relativeUrl = relativeUrl.Replace(UrlTemplatePatterns.StoreUrl, store.Url);
            }

            if (!string.IsNullOrEmpty(store.SecureUrl))
            {
                relativeUrl = relativeUrl.Replace(UrlTemplatePatterns.StoreSecureUrl, store.SecureUrl);
            }

            if (!string.IsNullOrEmpty(language))
            {
                relativeUrl = relativeUrl.Replace(UrlTemplatePatterns.Language, language);
            }

            if (!string.IsNullOrEmpty(semanticUrl))
            {
                relativeUrl = relativeUrl.Replace(UrlTemplatePatterns.Slug, semanticUrl);
            }

            Uri uri = null;
            if (relativeUrl != urlTemplate)
            {
                Uri.TryCreate(relativeUrl, UriKind.Absolute, out uri);
            }

            return uri != null ? Uri.UnescapeDataString(uri.AbsoluteUri) : null;
        }

        private SitemapItemOptions GetSitemapItemOptions(string sitemapItemType)
        {
            var sitemapItemOptions = new SitemapItemOptions
            {
                Priority = .5M,
                UpdateFrequency = PageUpdateFrequency.Weekly
            };

            if (sitemapItemType.EqualsInvariant("category"))
            {
                sitemapItemOptions = _sitemapOptions.CategoryOptions;
            }
            else if (sitemapItemType.EqualsInvariant("product"))
            {
                sitemapItemOptions = _sitemapOptions.ProductOptions;
            }

            return sitemapItemOptions;
        }

        private SitemapOptions GetSitemapOptions()
        {
            var sitemapOptions = new SitemapOptions
            {
                CategoryOptions = new SitemapItemOptions
                {
                    Priority = SettingsManager.GetValue("Sitemap.CategoryPagePriority", .7M),
                    UpdateFrequency = SettingsManager.GetValue("Sitemap.CategoryPageUpdateFrequency", PageUpdateFrequency.Weekly)
                },
                ProductOptions = new SitemapItemOptions
                {
                    Priority = SettingsManager.GetValue("Sitemap.ProductPagePriority", 1.0M),
                    UpdateFrequency = SettingsManager.GetValue("Sitemap.ProductPageUpdateFrequency", PageUpdateFrequency.Daily)
                },
                SearchBunchSize = SettingsManager.GetValue("Sitemap.CatalogSearchBunchSize", 1000),
                FilenameSeparator = SettingsManager.GetValue("Sitemap.FilenameSeparator", "--"),
                RecordsLimitPerFile = SettingsManager.GetValue("Sitemap.RecordsLimitPerFile", 10000)
            };

            return sitemapOptions;
        }
    }
}