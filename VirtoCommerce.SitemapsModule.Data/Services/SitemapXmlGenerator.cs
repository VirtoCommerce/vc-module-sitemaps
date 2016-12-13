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

        public virtual ICollection<SitemapMapping> GetSitemapSchema(string storeId, bool includeItems = false)
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
                    var sitemapItemMappings = GetSitemapItemMappings(store, sitemap);
                    var partsCount = (int)Math.Ceiling((double)sitemapItemMappings.Count / _sitemapOptions.RecordsLimitPerFile);
                    for (var i = 1; i <= partsCount; i++)
                    {
                        var sitemapFilename = partsCount > 1 ? string.Format("{0}{1}{2}.xml", sitemap.Filename, _sitemapOptions.FilenameSeparator, i) : sitemap.Filename;
                        Uri uri = null;
                        Uri.TryCreate(string.Format("{0}/{1}", store.Url, sitemap.UrlTemplate.Replace("{slug}", sitemapFilename)), UriKind.Absolute, out uri);
                        var sitemapMapping = new SitemapMapping
                        {
                            Filename = sitemapFilename,
                            SitemapId = sitemap.Id,
                            Url = uri != null ? uri.AbsoluteUri : null
                        };
                        if (includeItems)
                        {
                            sitemapMapping.Items = sitemapItemMappings.Skip((i - 1) * _sitemapOptions.RecordsLimitPerFile).Take(_sitemapOptions.RecordsLimitPerFile).ToList();
                        }
                        sitemapMappings.Add(sitemapMapping);
                    }
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
            if (string.IsNullOrEmpty(sitemapUrl))
            {
                throw new ArgumentException("sitemapUrl");
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
                var sitemapMapping = sitemapMappings.FirstOrDefault(sm => sm.Url.EqualsInvariant(sitemapUrl));
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
                var sitemapItemOptions = GetSitemapItemOptions(sitemapItemMapping);
                var sitemapItemXmlRecord = new SitemapItemXmlRecord
                {
                    ModifiedDate = DateTime.UtcNow,
                    Priority = sitemapItemOptions.PagePriority,
                    UpdateFrequency = sitemapItemOptions.PageUpdateFrequency,
                    Url = sitemapItemMapping.Url
                };
                sitemapXmlRecord.Items.Add(sitemapItemXmlRecord);
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

        private ICollection<SitemapItemMapping> GetSitemapItemMappings(Store store, Sitemap sitemap)
        {
            var sitemapItemMappings = new List<SitemapItemMapping>();

            var formalSitemapItems = GetFormalSitemapItems(sitemap);

            var catalogSitemapItemMappings = GetCatalogSitemapItemMappings(store, sitemap, formalSitemapItems);
            sitemapItemMappings.AddRange(catalogSitemapItemMappings);

            var customSitemapItemMappings = GetCustomSitemapItemMappings(store, formalSitemapItems);
            sitemapItemMappings.AddRange(customSitemapItemMappings);

            sitemapItemMappings = sitemapItemMappings.GroupBy(i => i.Url).Select(i => i.First()).ToList();

            return sitemapItemMappings;
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

        private ICollection<SitemapItemMapping> GetCatalogSitemapItemMappings(Store store, Sitemap sitemap, ICollection<SitemapItem> formalSitemapItems)
        {
            var catalogSitemapItemMappings = new List<SitemapItemMapping>();

            var categorySitemapItems = formalSitemapItems.Where(si => si.ObjectType.EqualsInvariant("category"));
            var categoryIds = categorySitemapItems.Select(si => si.ObjectId).ToArray();
            var categories = CategoryService.GetByIds(categoryIds, CategoryResponseGroup.WithSeo);
            foreach (var category in categories)
            {
                var categoryMappings = GetSitemapItemMappingsWithSeo(category, sitemap, store, "Category");
                catalogSitemapItemMappings.AddRange(categoryMappings);
            }

            var productSitemapItems = formalSitemapItems.Where(si => si.ObjectType.EqualsInvariant("product"));
            var productIds = productSitemapItems.Select(si => si.ObjectId).ToArray();
            var products = ItemService.GetByIds(productIds, ItemResponseGroup.Seo);
            foreach (var product in products)
            {
                var productMappings = GetSitemapItemMappingsWithSeo(product, sitemap, store, "Product");
                catalogSitemapItemMappings.AddRange(productMappings);
            }

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
                var categoryMappings = GetSitemapItemMappingsWithSeo(category, sitemap, store, "Category");
                catalogSitemapItemMappings.AddRange(categoryMappings);
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
                var productMappings = GetSitemapItemMappingsWithSeo(product, sitemap, store, "Product");
                catalogSitemapItemMappings.AddRange(productMappings);
            }

            return catalogSitemapItemMappings;
        }

        private ICollection<SitemapItemMapping> GetCustomSitemapItemMappings(Store store, ICollection<SitemapItem> formalSitemapItems)
        {
            var customSitemapItemMappings = formalSitemapItems.Where(si => si.ObjectType.EqualsInvariant("custom")).Select(si => new SitemapItemMapping
            {
                Language = store.DefaultLanguage,
                ObjectType = si.ObjectType,
                Url = si.UrlTemplate
            }).ToList();

            return customSitemapItemMappings;
        }

        private ICollection<SitemapItemMapping> GetSitemapItemMappingsWithSeo(ISeoSupport seoSupportItem, Sitemap sitemap, Store store, string objectType)
        {
            var sitemapItemMappings = new List<SitemapItemMapping>();

            var storeUrl = store.Url;
            if (string.IsNullOrEmpty(storeUrl))
            {
                storeUrl = store.SecureUrl;
            }

            Uri absoluteUrl = null;
            var relativeUrl = string.Format("{0}/{1}/{2}", storeUrl, objectType.ToLower(), sitemap.UrlTemplate.Replace("{slug}", seoSupportItem.Id));
            Uri.TryCreate(relativeUrl, UriKind.Absolute, out absoluteUrl);

            if (seoSupportItem.SeoInfos != null && seoSupportItem.SeoInfos.Any())
            {
                var seoInfos = seoSupportItem.SeoInfos.Where(si => si.IsActive && store.Languages.Contains(si.LanguageCode));
                foreach (var seoInfo in seoInfos)
                {
                    var sitemapItemMapping = new SitemapItemMapping
                    {
                        Language = seoInfo.LanguageCode,
                        ObjectId = seoInfo.ObjectId,
                        ObjectType = seoInfo.ObjectType
                    };
                    if (!string.IsNullOrEmpty(sitemap.UrlTemplate))
                    {
                        relativeUrl = string.Format("{0}/{1}/{2}", storeUrl, seoInfo.LanguageCode, sitemap.UrlTemplate.Replace("{slug}", seoInfo.SemanticUrl));
                        Uri.TryCreate(relativeUrl, UriKind.Absolute, out absoluteUrl);
                        sitemapItemMapping.Url =  absoluteUrl.AbsoluteUri;
                    }
                    sitemapItemMappings.Add(sitemapItemMapping);
                }
            }
            else
            {
                sitemapItemMappings.Add(new SitemapItemMapping
                {
                    Language = store.DefaultLanguage,
                    ObjectId = seoSupportItem.Id,
                    ObjectType = objectType,
                    Url = absoluteUrl != null ? absoluteUrl.AbsoluteUri : null
                });
            }

            return sitemapItemMappings;
        }

        private SitemapItemOptions GetSitemapItemOptions(SitemapItemMapping sitemapItemMapping)
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