using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapXmlGenerator : ISitemapXmlGenerator
    {
        public SitemapXmlGenerator(
            ISitemapService sitemapService,
            ISitemapItemService sitemapItemService,
            IStoreService storeService,
            IItemService catalogItemService,
            ICategoryService catalogCategoryService,
            ISitemapUrlBuilder sitemapUrlBuilder)
        {
            SitemapService = sitemapService;
            SitemapItemService = sitemapItemService;
            StoreService = storeService;
            CatalogItemService = catalogItemService;
            CatalogCategoryService = catalogCategoryService;
            SitemapUrlBuilder = sitemapUrlBuilder;
        }

        protected ISitemapService SitemapService { get; private set; }
        protected ISitemapItemService SitemapItemService { get; private set; }
        protected IStoreService StoreService { get; private set; }
        protected IItemService CatalogItemService { get; private set; }
        protected ICategoryService CatalogCategoryService { get; private set; }
        protected ISitemapUrlBuilder SitemapUrlBuilder { get; private set; }

        public virtual SitemapIndexXmlRecord GenerateSitemapIndex(string storeId, SitemapOptions options)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                throw new ArgumentException("storeId");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            SitemapIndexXmlRecord xmlRecord = null;

            var store = StoreService.GetById(storeId);
            if (store != null)
            {
                var sitemapsSearchResponse = SitemapService.Search(new SitemapSearchRequest
                {
                    StoreId = store.Id,
                    Skip = 0,
                    Take = options.RecordsLimitPerFile
                });

                xmlRecord = new SitemapIndexXmlRecord
                {
                    Sitemaps = sitemapsSearchResponse.Items.Where(i => i.ItemsTotalCount > 0).Select(i => new SitemapIndexItemXmlRecord
                    {
                        ModifiedDate = DateTime.UtcNow,
                        Url = SitemapUrlBuilder.ToAbsoluteUrl(store, i.Filename)
                    }).ToList()
                };
            }

            return xmlRecord;
        }

        public virtual SitemapXmlRecord GenerateSitemap(string storeId, string sitemapFilename, SitemapOptions options)
        {
            if (string.IsNullOrEmpty("storeId"))
            {
                throw new ArgumentException("storeId");
            }
            if (string.IsNullOrEmpty(sitemapFilename))
            {
                throw new ArgumentException("sitemapFilename");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            SitemapXmlRecord xmlRecord = null;

            var store = StoreService.GetById(storeId);
            if (store != null)
            {
                xmlRecord = GetSitemapXmlRecord(store, sitemapFilename, options);
            }

            return xmlRecord;
        }

        private SitemapXmlRecord GetSitemapXmlRecord(Store store, string sitemapFilename, SitemapOptions options)
        {
            var sitemapXmlRecord = new SitemapXmlRecord();

            var sitemapSearchResponse = SitemapService.Search(new SitemapSearchRequest
            {
                Filename = sitemapFilename,
                StoreId = store.Id,
                Skip = 0,
                Take = 1
            });

            var sitemap = sitemapSearchResponse.Items.FirstOrDefault();
            if (sitemap == null)
            {
                return sitemapXmlRecord;
            }

            var sitemapItemsSearchResponse = SitemapItemService.Search(new SitemapItemSearchRequest
            {
                SitemapId = sitemap.Id,
                Skip = 0,
                Take = options.RecordsLimitPerFile
            });

            sitemap.Items = sitemapItemsSearchResponse.Items;

            var seoInfos = new List<SeoInfo>();

            var productSeoInfos = GetProductSeoInfos(sitemap.Items);
            seoInfos.AddRange(productSeoInfos);

            var categorySeoInfos = GetCategorySeoInfos(sitemap.Items);
            seoInfos.AddRange(categorySeoInfos);

            foreach (var seoInfo in seoInfos)
            {
                var sitemapItemXmlRecord = GetSitemapItemXmlRecord(store, seoInfo, options);
                if (sitemapItemXmlRecord != null)
                {
                    sitemapXmlRecord.Items.Add(sitemapItemXmlRecord);
                }
            }

            return sitemapXmlRecord;
        }

        private ICollection<SeoInfo> GetProductSeoInfos(ICollection<SitemapItem> sitemapItems)
        {
            var seoInfos = new List<SeoInfo>();

            var productIds = sitemapItems.Where(i => i.ObjectType.Equals("product", StringComparison.OrdinalIgnoreCase)).Select(i => i.ObjectId).ToArray();
            var products = CatalogItemService.GetByIds(productIds, ItemResponseGroup.Seo);
            if (products != null && products.Any())
            {
                var productSeoInfos = products.Where(p => p.SeoInfos != null && p.SeoInfos.Any()).SelectMany(p => p.SeoInfos).ToArray();
                seoInfos.AddRange(productSeoInfos);
            }

            return seoInfos;
        }

        private ICollection<SeoInfo> GetCategorySeoInfos(ICollection<SitemapItem> sitemapItems)
        {
            var seoInfos = new List<SeoInfo>();

            var categoryIds = sitemapItems.Where(i => i.ObjectType.Equals("category", StringComparison.OrdinalIgnoreCase)).Select(i => i.ObjectId).ToArray();
            var categories = CatalogCategoryService.GetByIds(categoryIds, CategoryResponseGroup.WithSeo);
            if (categories != null && categories.Any())
            {
                var categorySeoInfos = categories.Where(c => c.SeoInfos != null && c.SeoInfos.Any()).SelectMany(c => c.SeoInfos).ToArray();
                seoInfos.AddRange(categorySeoInfos);
            }

            return seoInfos;
        }

        private SitemapItemXmlRecord GetSitemapItemXmlRecord(Store store, SeoInfo seoInfo, SitemapOptions options)
        {
            SitemapItemXmlRecord sitemapItem = null;

            var absoluteUrl = SitemapUrlBuilder.ToAbsoluteUrl(store, seoInfo);
            if (!string.IsNullOrEmpty(absoluteUrl))
            {
                sitemapItem = new SitemapItemXmlRecord
                {
                    ModifiedDate = DateTime.UtcNow,
                    Priority = GetPagePriority(seoInfo.ObjectType, options),
                    UpdateFrequency = GetPageUpdateFrequency(seoInfo.ObjectType, options),
                    Url = absoluteUrl
                };
            }

            return sitemapItem;
        }

        private decimal GetPagePriority(string objectType, SitemapOptions options)
        {
            var priority = 0.5M;

            if (objectType.Equals("category", StringComparison.OrdinalIgnoreCase))
            {
                priority = options.CategoryPagePriority;
            }
            else if (objectType.Equals("product", StringComparison.OrdinalIgnoreCase))
            {
                priority = options.ProductPagePriority;
            }

            return priority;
        }

        private string GetPageUpdateFrequency(string objectType, SitemapOptions options)
        {
            var frequency = PageUpdateFrequency.Weekly;

            if (objectType.Equals("category", StringComparison.OrdinalIgnoreCase))
            {
                frequency = options.CategoryPageUpdateFrequency;
            }
            else if (objectType.Equals("product", StringComparison.OrdinalIgnoreCase))
            {
                frequency = options.ProductPageUpdateFrequency;
            }

            return frequency;
        }
    }
}