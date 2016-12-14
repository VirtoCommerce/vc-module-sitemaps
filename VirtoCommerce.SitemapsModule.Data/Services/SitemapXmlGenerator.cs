using System;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models.Xml;
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
            IMemberSearchService memberSearchService,
            ISettingsManager settingsManager)
        {
            SitemapService = sitemapService;
            SitemapItemService = sitemapItemService;
            StoreService = storeService;
            ItemService = itemService;
            CategoryService = categoryService;
            CatalogSearchService = catalogSearchService;
            MemberSearchService = memberSearchService;
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
        protected IMemberSearchService MemberSearchService { get; private set; }
        protected ISettingsManager SettingsManager { get; private set; }

        public virtual ICollection<SitemapMapping> GetSitemapSchema(string storeId, bool withItems = false)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                throw new ArgumentException("storeId");
            }

            var sitemapMappings = new List<SitemapMapping>();

            var store = StoreService.GetById(storeId);
            if (store != null)
            {
                var sitemaps = GetSitemaps(store);
                foreach (var sitemap in sitemaps)
                {
                    sitemapMappings.AddRange(GetSitemapMappings(store, sitemap, withItems));
                }
            }

            return sitemapMappings;
        }

        public virtual Stream GenerateSitemapXml(ICollection<SitemapMapping> sitemapMappings, string sitemapUrl)
        {
            if (sitemapMappings == null)
            {
                throw new ArgumentNullException("sitemapMappings");
            }

            var sitemapUrlParts = sitemapUrl.Split('/');
            var sitemapFilename = sitemapUrlParts.LastOrDefault();
            if (string.IsNullOrEmpty(sitemapFilename))
            {
                sitemapFilename = "sitemap.xml";
            }

            XmlSerializer xmlSerializer = null;
            var xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add("", "http://www.sitemaps.org/schemas/sitemap/0.9");

            var stream = new MemoryStream();
            if (sitemapFilename.EqualsInvariant("sitemap.xml"))
            {
                var sitemapIndexXmlRecord = GetSitemapIndexXmlRecord(sitemapMappings);
                xmlSerializer = new XmlSerializer(sitemapIndexXmlRecord.GetType());
                xmlSerializer.Serialize(stream, sitemapIndexXmlRecord, xmlNamespaces);
            }
            else
            {
                var sitemapMapping = sitemapMappings.FirstOrDefault(sm => sm.Filename.EqualsInvariant(sitemapFilename));
                if (sitemapMapping != null)
                {
                    var sitemapXmlRecord = GetSitemapXmlRecord(sitemapMapping);
                    xmlSerializer = new XmlSerializer(sitemapXmlRecord.GetType());
                    xmlSerializer.Serialize(stream, sitemapXmlRecord, xmlNamespaces);
                }
            }

            stream.Position = 0;

            return stream;
        }

        private SitemapIndexXmlRecord GetSitemapIndexXmlRecord(ICollection<SitemapMapping> sitemapMappings)
        {
            var sitemapIndexXmlRecord = new SitemapIndexXmlRecord();

            sitemapIndexXmlRecord.Sitemaps = sitemapMappings.Select(sm => new SitemapIndexItemXmlRecord
            {
                ModifiedDate = DateTime.UtcNow,
                Url = sm.Url
            }).ToList();

            return sitemapIndexXmlRecord;
        }

        private SitemapXmlRecord GetSitemapXmlRecord(SitemapMapping sitemapMapping)
        {
            var sitemapXmlRecord = new SitemapXmlRecord();

            foreach (var sitemapItemMapping in sitemapMapping.Items)
            {
                var options = GetSitemapItemMappingOptions(sitemapItemMapping);
                sitemapXmlRecord.Items.Add(new SitemapItemXmlRecord
                {
                    ModifiedDate = DateTime.UtcNow,
                    Priority = options.PagePriority,
                    UpdateFrequency = options.PageUpdateFrequency,
                    Url = sitemapItemMapping.Url
                });
            }

            return sitemapXmlRecord;
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
            var partsCount = (int)Math.Ceiling((double)sitemapSearchResult.TotalCount / _sitemapOptions.RecordsLimitPerFile);
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

        private ICollection<SitemapItem> GetFormalSitemapItems(Sitemap sitemap)
        {
            var formalSitemapItems = new List<SitemapItem>();

            var sitemapItemSearchCriteria = new SitemapItemSearchCriteria
            {
                SitemapId = sitemap.Id,
                Skip = 0,
                Take = _sitemapOptions.RecordsLimitPerFile
            };
            var sitemapItemSearchResult = SitemapItemService.Search(sitemapItemSearchCriteria);
            var partsCount = (int)Math.Ceiling((double)sitemapItemSearchResult.TotalCount / _sitemapOptions.RecordsLimitPerFile);
            for (var i = 1; i <= partsCount; i++)
            {
                foreach (var sitemapItem in sitemapItemSearchResult.Results)
                {
                    if (string.IsNullOrEmpty(sitemapItem.UrlTemplate))
                    {
                        sitemapItem.UrlTemplate = sitemap.UrlTemplate;
                    }
                }
                formalSitemapItems.AddRange(sitemapItemSearchResult.Results);
                if (partsCount > 1)
                {
                    sitemapItemSearchCriteria.Skip = _sitemapOptions.RecordsLimitPerFile * i;
                    sitemapItemSearchResult = SitemapItemService.Search(sitemapItemSearchCriteria);
                }
            }

            return formalSitemapItems;
        }

        private ICollection<SitemapMapping> GetSitemapMappings(Store store, Sitemap sitemap, bool withItems = false)
        {
            var sitemapMappings = new List<SitemapMapping>();

            sitemap.Items = GetFormalSitemapItems(sitemap);
            var sitemapItemMappings = GetSitemapItemMappings(store, sitemap);

            int partsCount = (int)Math.Ceiling((double)sitemapItemMappings.Count() / _sitemapOptions.RecordsLimitPerFile);
            for (var i = 1; i <= partsCount; i++)
            {
                var filename = partsCount > 1 ?
                    string.Format("{0}{1}{2}.xml", sitemap.Filename.Replace(".xml", string.Empty), _sitemapOptions.FilenameSeparator, i) :
                    sitemap.Filename;
                var sitemapMapping = new SitemapMapping
                {
                    Filename = filename,
                    SitemapId = sitemap.Id,
                    Url = GetAbsoluteUrl(store, sitemap.UrlTemplate, new SeoInfo { SemanticUrl = filename })
                };
                if (withItems)
                {
                    sitemapMapping.Items = sitemapItemMappings.Skip((i - 1) * _sitemapOptions.RecordsLimitPerFile).Take(_sitemapOptions.RecordsLimitPerFile);
                }
                sitemapMappings.Add(sitemapMapping);
            }

            return sitemapMappings;
        }

        private IEnumerable<SitemapItemMapping> GetSitemapItemMappings(Store store, Sitemap sitemap)
        {
            var sitemapItemMappings = new List<SitemapItemMapping>();

            var catalogSitemapItemMappings = GetCatalogSitemapItemMappings(store, sitemap);
            sitemapItemMappings.AddRange(catalogSitemapItemMappings);

            var customSitemapItemMappings = GetCustomSitemapItemMappings(store, sitemap);
            sitemapItemMappings.AddRange(customSitemapItemMappings);

            // TODO: Add vendor sitemap items

            // TODO: Add static content sitemap items

            return sitemapItemMappings.GroupBy(m => m.Url).Select(i => i.First());
        }

        private ICollection<SitemapItemMapping> GetCatalogSitemapItemMappings(Store store, Sitemap sitemap)
        {
            var catalogSitemapItemMappings = new List<SitemapItemMapping>();

            var categorySitemapItems = sitemap.Items.Where(i => i.ObjectType.EqualsInvariant(SitemapItemTypes.Category));
            var categoryIds = categorySitemapItems.Select(i => i.ObjectId).ToArray();
            var categories = CategoryService.GetByIds(categoryIds, CategoryResponseGroup.WithSeo);
            catalogSitemapItemMappings.AddRange(BuildSitemapItemMappings(categories, store, SitemapItemTypes.Category, sitemap.UrlTemplate));

            var productSitemapItems = sitemap.Items.Where(i => i.ObjectType.EqualsInvariant(SitemapItemTypes.Product));
            var productIds = productSitemapItems.Select(i => i.ObjectId).ToArray();
            var products = ItemService.GetByIds(productIds, ItemResponseGroup.Seo);
            catalogSitemapItemMappings.AddRange(BuildSitemapItemMappings(products, store, SitemapItemTypes.Product, sitemap.UrlTemplate));

            var catalogSearchCriteria = new Domain.Catalog.Model.SearchCriteria
            {
                CatalogId = store.Catalog,
                CategoryIds = categoryIds,
                ResponseGroup = SearchResponseGroup.WithCategories | SearchResponseGroup.WithProducts,
                Skip = 0,
                Take = _sitemapOptions.CatalogSearchBunchSize
            };
            var catalogSearchResult = CatalogSearchService.Search(catalogSearchCriteria);
            foreach (var category in catalogSearchResult.Categories)
            {
                catalogSitemapItemMappings.AddRange(BuildSitemapItemMappings(new[] { category }, store, SitemapItemTypes.Category, sitemap.UrlTemplate));
            }

            var productPartsCount = (int)Math.Ceiling((double)catalogSearchResult.ProductsTotalCount / _sitemapOptions.CatalogSearchBunchSize);
            var cbProducts = new ConcurrentBag<CatalogProduct>();
            Parallel.For(1, productPartsCount + 1, new ParallelOptions { MaxDegreeOfParallelism = 5 }, i =>
            {
                foreach (var product in catalogSearchResult.Products)
                {
                    cbProducts.Add(product);
                }
                if (productPartsCount > 1)
                {
                    catalogSearchCriteria.Skip = _sitemapOptions.CatalogSearchBunchSize * i;
                    catalogSearchResult = CatalogSearchService.Search(catalogSearchCriteria);
                }
            });
            foreach (var product in cbProducts)
            {
                catalogSitemapItemMappings.AddRange(BuildSitemapItemMappings(new[] { product }, store, SitemapItemTypes.Product, sitemap.UrlTemplate));
            }

            return catalogSitemapItemMappings;
        }

        private ICollection<SitemapItemMapping> GetCustomSitemapItemMappings(Store store, Sitemap sitemap)
        {
            var customSitemapItemMappings = new List<SitemapItemMapping>();

            var customSitemapItems = sitemap.Items.Where(i => i.ObjectType.EqualsInvariant(SitemapItemTypes.Custom));
            customSitemapItemMappings.AddRange(BuildSitemapItemMappings(customSitemapItems, store));

            return customSitemapItemMappings;
        }

        private ICollection<SitemapItemMapping> BuildSitemapItemMappings(IEnumerable<SitemapItem> sitemapItems, Store store)
        {
            var sitemapItemMappings = new List<SitemapItemMapping>();

            foreach (var sitemapItem in sitemapItems)
            {
                sitemapItemMappings.Add(new SitemapItemMapping
                {
                    ObjectId = sitemapItem.ObjectId,
                    ObjectType = sitemapItem.ObjectType,
                    Url = GetAbsoluteUrl(store, sitemapItem.UrlTemplate)
                });
            }

            return sitemapItemMappings;
        }

        private ICollection<SitemapItemMapping> BuildSitemapItemMappings(ICollection<ISeoSupport> seoSupportItems, Store store, string objectType, string urlTemplate)
        {
            var sitemapItemMappings = new List<SitemapItemMapping>();

            foreach (var seoSupportItem in seoSupportItems)
            {
                var seoInfos = seoSupportItem.SeoInfos.Where(si => si.IsActive && store.Languages.Contains(si.LanguageCode)).ToList();
                if (!seoInfos.Any())
                {
                    seoInfos.Add(new SeoInfo
                    {
                        IsActive = true,
                        LanguageCode = store.DefaultLanguage,
                        ObjectId = seoSupportItem.Id,
                        ObjectType = objectType,
                        SemanticUrl = seoSupportItem.Id,
                        StoreId = store.Id
                    });
                }
                foreach (var seoInfo in seoInfos)
                {
                    sitemapItemMappings.Add(new SitemapItemMapping
                    {
                        ObjectId = seoSupportItem.Id,
                        ObjectType = seoSupportItem.SeoObjectType,
                        Url = GetAbsoluteUrl(store, urlTemplate, seoInfo)
                    });
                }
            }

            return sitemapItemMappings;
        }

        private string GetAbsoluteUrl(Store store, string urlTemplate, SeoInfo seoInfo = null)
        {
            string relativeUrl = null;

            if (!string.IsNullOrEmpty(store.Url))
            {
                relativeUrl = urlTemplate.Replace(UrlTemplatePatterns.StoreUrl, store.Url);
            }

            if (!string.IsNullOrEmpty(store.SecureUrl))
            {
                relativeUrl = urlTemplate.Replace(UrlTemplatePatterns.StoreSecureUrl, store.SecureUrl);
            }

            if (seoInfo != null)
            {
                if (!string.IsNullOrEmpty(seoInfo.LanguageCode))
                {
                    relativeUrl = relativeUrl.Replace(UrlTemplatePatterns.Language, seoInfo.LanguageCode);
                }
                if (!string.IsNullOrEmpty(seoInfo.SemanticUrl))
                {
                    relativeUrl = relativeUrl.Replace(UrlTemplatePatterns.Slug, seoInfo.SemanticUrl);
                }
            }

            Uri uri = null;
            Uri.TryCreate(relativeUrl, UriKind.Absolute, out uri);

            return uri != null ? Uri.UnescapeDataString(uri.AbsoluteUri) : null;
        }

        private SitemapItemOptions GetSitemapItemMappingOptions(SitemapItemMapping sitemapItemMapping)
        {
            var sitemapItemOptions = new SitemapItemOptions
            {
                PagePriority = .5M,
                PageUpdateFrequency = PageUpdateFrequency.Weekly
            };

            if (sitemapItemMapping.ObjectType.EqualsInvariant("category"))
            {
                sitemapItemOptions = _sitemapOptions.CategoryOptions;
            }
            else if (sitemapItemMapping.ObjectType.EqualsInvariant("product"))
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
                    PagePriority = SettingsManager.GetValue("Sitemap.CategoryPagePriority", .7M),
                    PageUpdateFrequency = SettingsManager.GetValue("Sitemap.CategoryPageUpdateFrequency", PageUpdateFrequency.Weekly)
                },
                ProductOptions = new SitemapItemOptions
                {
                    PagePriority = SettingsManager.GetValue("Sitemap.ProductPagePriority", 1.0M),
                    PageUpdateFrequency = SettingsManager.GetValue("Sitemap.ProductPageUpdateFrequency", PageUpdateFrequency.Daily)
                },
                CatalogSearchBunchSize = SettingsManager.GetValue("Sitemap.CatalogSearchBunchSize", 10000),
                FilenameSeparator = SettingsManager.GetValue("Sitemap.FilenameSeparator", "--"),
                RecordsLimitPerFile = SettingsManager.GetValue("Sitemap.RecordsLimitPerFile", 10000)
            };

            return sitemapOptions;
        }
    }
}