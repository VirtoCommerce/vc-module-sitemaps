using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.Tools;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class CatalogSitemapItemRecordProvider : SitemapItemRecordProviderBase, ISitemapItemRecordProvider
    {
        public CatalogSitemapItemRecordProvider(
            ICategoryService categoryService,
            IItemService itemService,
            ICatalogSearchService catalogSearchService,
            IUrlBuilder urlBuilder,
            ISettingsManager settingsManager)
            : base(settingsManager, urlBuilder)
        {
            CategoryService = categoryService;
            ItemService = itemService;
            CatalogSearchService = catalogSearchService;
        }

        protected ICategoryService CategoryService { get; private set; }
        protected IItemService ItemService { get; private set; }
        protected ICatalogSearchService CatalogSearchService { get; private set; }

        public virtual void LoadSitemapItemRecords(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var progressInfo = new ExportImportProgressInfo();

            var categoryOptions = new SitemapItemOptions
            {
                Priority = SettingsManager.GetValue("Sitemap.CategoryPagePriority", .7M),
                UpdateFrequency = SettingsManager.GetValue("Sitemap.CategoryPageUpdateFrequency", UpdateFrequency.Weekly)
            };
            var productOptions = new SitemapItemOptions
            {
                Priority = SettingsManager.GetValue("Sitemap.ProductPagePriority", 1.0M),
                UpdateFrequency = SettingsManager.GetValue("Sitemap.ProductPageUpdateFrequency", UpdateFrequency.Daily)
            };
            var searchBunchSize = SettingsManager.GetValue("Sitemap.SearchBunchSize", 500);

            var categorySitemapItems = sitemap.Items.Where(x => x.ObjectType.EqualsInvariant(SitemapItemTypes.Category));
            var categoryIds = categorySitemapItems.Select(x => x.ObjectId).ToArray();
            var categories = CategoryService.GetByIds(categoryIds, CategoryResponseGroup.WithSeo | CategoryResponseGroup.WithOutlines).Where(c => !c.IsActive.HasValue || c.IsActive.Value);
            Parallel.ForEach(categorySitemapItems, new ParallelOptions { MaxDegreeOfParallelism = 5 }, (sitemapItem =>
            {
                var itemsCount = 0;

                var category = categories.FirstOrDefault(x => x.Id == sitemapItem.ObjectId);
                if (category != null)
                {
                    sitemapItem.ItemsRecords = GetSitemapItemRecords(store, categoryOptions, sitemap.UrlTemplate, baseUrl, category);
                    if (category != null)
                    {
                        var catalogSearchCriteria = new Domain.Catalog.Model.SearchCriteria
                        {
                            CategoryId = category.Id,
                            ResponseGroup = SearchResponseGroup.WithCategories | SearchResponseGroup.WithOutlines,
                            Skip = 0,
                            Take = searchBunchSize,
                            HideDirectLinkedCategories = true,
                            SearchInChildren = true
                        };
                        var catalogSearchResult = CatalogSearchService.Search(catalogSearchCriteria);

                        Interlocked.Exchange(ref itemsCount, catalogSearchResult.Categories.Count);
                        progressInfo.Description = string.Format("Generating sitemap items for category \"{0}\": {1}...", category.Name, itemsCount);
                        if (progressCallback != null)
                        {
                            progressCallback(progressInfo);
                        }

                        foreach (var seoObj in catalogSearchResult.Categories.Where(c => !c.IsActive.HasValue || c.IsActive.Value))
                        {
                            sitemapItem.ItemsRecords.AddRange(GetSitemapItemRecords(store, categoryOptions, sitemap.UrlTemplate, baseUrl, seoObj));
                        }

                        //Load all category products
                        catalogSearchCriteria.Take = 1;
                        catalogSearchCriteria.ResponseGroup = SearchResponseGroup.WithProducts | SearchResponseGroup.WithOutlines;
                        var productTotalCount = CatalogSearchService.Search(catalogSearchCriteria).ProductsTotalCount;
                        var itemRecords = new ConcurrentBag<SitemapItemRecord>();
                        Parallel.For(0, (int)Math.Ceiling(productTotalCount / (double)searchBunchSize), new ParallelOptions { MaxDegreeOfParallelism = 5 }, (i) =>
                        {
                            var productSearchCriteria = new Domain.Catalog.Model.SearchCriteria
                            {
                                CategoryId = category.Id,
                                ResponseGroup = SearchResponseGroup.WithProducts | SearchResponseGroup.WithOutlines,
                                Skip = i * searchBunchSize,
                                Take = searchBunchSize,
                                HideDirectLinkedCategories = true,
                                SearchInChildren = true,
                                OnlyBuyable = true
                            };
                            var productSearchResult = CatalogSearchService.Search(productSearchCriteria);

                            Interlocked.Exchange(ref itemsCount, productSearchResult.Products.Count);
                            progressInfo.Description = string.Format("Generating sitemap items for category \"{0}\": {1}...", category.Name, itemsCount);
                            if (progressCallback != null)
                            {
                                progressCallback(progressInfo);
                            }

                            foreach (var product in productSearchResult.Products.Where(p => !p.IsActive.HasValue || p.IsActive.Value))
                            {
                                foreach (var record in GetSitemapItemRecords(store, productOptions, sitemap.UrlTemplate, baseUrl, product))
                                {
                                    itemRecords.Add(record);
                                }
                            }
                        });
                        sitemapItem.ItemsRecords.AddRange(itemRecords);
                    }
                }
            }));

            var productSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Product));
            var productIds = productSitemapItems.Select(si => si.ObjectId).ToArray();
            var products = ItemService.GetByIds(productIds, ItemResponseGroup.Seo | ItemResponseGroup.Outlines).Where(p => !p.IsActive.HasValue || p.IsActive.Value);
            foreach (var sitemapItem in productSitemapItems)
            {
                var product = products.FirstOrDefault(x => x.Id == sitemapItem.ObjectId);
                var itemRecords = GetSitemapItemRecords(store, productOptions, sitemap.UrlTemplate, baseUrl, product);
                sitemapItem.ItemsRecords.AddRange(itemRecords);
            }
        }

    }
}