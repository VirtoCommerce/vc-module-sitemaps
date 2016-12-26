using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class CatalogSitemapItemRecordProvider : SitemapItemRecordProviderBase, ISitemapItemRecordProvider
    {
        public CatalogSitemapItemRecordProvider(
            ICategoryService categoryService,
            IItemService itemService,
            ICatalogSearchService catalogSearchService,
            ISitemapUrlBuilder sitemapUrlBuilder,
            ISettingsManager settingsManager)
            : base(settingsManager, sitemapUrlBuilder)
        {
            CategoryService = categoryService;
            ItemService = itemService;
            CatalogSearchService = catalogSearchService;
        }

        protected ICategoryService CategoryService { get; private set; }
        protected IItemService ItemService { get; private set; }
        protected ICatalogSearchService CatalogSearchService { get; private set; }

        public virtual void LoadSitemapItemRecords(Sitemap sitemap, string baseUrl)
        {
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
            var categories = CategoryService.GetByIds(categoryIds, CategoryResponseGroup.WithSeo);

            Parallel.ForEach(categorySitemapItems, new ParallelOptions { MaxDegreeOfParallelism = 5 }, (sitemapItem =>
            {
                var category = categories.FirstOrDefault(x => x.Id == sitemapItem.ObjectId);
                if (category != null)
                {
                    sitemapItem.ItemsRecords = GetSitemapItemRecords(categoryOptions, sitemap.UrlTemplate, baseUrl, category);
                    if (category != null)
                    {
                        var catalogSearchCriteria = new Domain.Catalog.Model.SearchCriteria
                        {
                            CategoryId = category.Id,
                            ResponseGroup = SearchResponseGroup.WithCategories,
                            Skip = 0,
                            Take = searchBunchSize,
                            HideDirectLinkedCategories = true,
                            SearchInChildren = true
                        };
                        var catalogSearchResult = CatalogSearchService.Search(catalogSearchCriteria);

                        foreach (var seoObj in catalogSearchResult.Categories)
                        {
                            sitemapItem.ItemsRecords.AddRange(GetSitemapItemRecords(categoryOptions, sitemap.UrlTemplate, baseUrl, seoObj));
                        }

                        //Load all category products
                        catalogSearchCriteria.Take = 1;
                        catalogSearchCriteria.ResponseGroup = SearchResponseGroup.WithProducts;
                        var productTotalCount  = CatalogSearchService.Search(catalogSearchCriteria).ProductsTotalCount;
                        var itemRecords = new ConcurrentBag<SitemapItemRecord>();
                        Parallel.For(0, (int)Math.Ceiling(catalogSearchResult.ProductsTotalCount / (double)searchBunchSize), new ParallelOptions { MaxDegreeOfParallelism = 5 }, (i) =>
                        {
                            var productSearchCriteria = new Domain.Catalog.Model.SearchCriteria
                            {
                                CategoryId = category.Id,
                                ResponseGroup = SearchResponseGroup.WithProducts,
                                Skip = i * searchBunchSize,
                                Take = searchBunchSize,
                                HideDirectLinkedCategories = true,
                                SearchInChildren = true
                            };                        
                            var productSearchResult = CatalogSearchService.Search(catalogSearchCriteria);
                            foreach (var product in productSearchResult.Products)
                            {
                                foreach(var record in GetSitemapItemRecords(productOptions, sitemap.UrlTemplate, baseUrl, product))
                                {
                                    itemRecords.Add(record);
                                }
                            }    
                        });
                        sitemapItem.ItemsRecords = itemRecords.ToList();
                    }
                }
            }));

            var productSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Product));
            var productIds = productSitemapItems.Select(si => si.ObjectId).ToArray();
            var products = ItemService.GetByIds(productIds, ItemResponseGroup.Seo);
            foreach (var sitemapItem in productSitemapItems)
            {
                var product = products.FirstOrDefault(x => x.Id == sitemapItem.ObjectId);
                sitemapItem.ItemsRecords = GetSitemapItemRecords(productOptions, sitemap.UrlTemplate, baseUrl, product);
            }
        }

    }
}