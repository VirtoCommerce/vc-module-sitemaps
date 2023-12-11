using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.ListEntry;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class CatalogSitemapItemRecordProvider : SitemapItemRecordProviderBase, ISitemapItemRecordProvider
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IItemService _itemService;
        private readonly IListEntrySearchService _listEntrySearchService;

        public CatalogSitemapItemRecordProvider(
            ISitemapUrlBuilder urlBuilder,
            ISettingsManager settingsManager,
            IItemService itemService,
            IListEntrySearchService listEntrySearchService)
            : base(urlBuilder)
        {
            _settingsManager = settingsManager;
            _itemService = itemService;
            _listEntrySearchService = listEntrySearchService;
        }

        #region ISitemapItemRecordProvider members
        public virtual async Task LoadSitemapItemRecordsAsync(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            if (sitemap == null)
            {
                throw new ArgumentNullException(nameof(sitemap));
            }

            await LoadCategoriesSitemapItemRecordsAsync(store, sitemap, baseUrl, progressCallback);
            await LoadProductsSitemapItemRecordsAsync(store, sitemap, baseUrl, progressCallback);
        }

        #endregion

        protected virtual async Task LoadCategoriesSitemapItemRecordsAsync(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var shouldIncludeImages = store.Settings.GetValue<bool>(ModuleConstants.Settings.ProductLinks.IncludeImages);

            var progressInfo = new ExportImportProgressInfo();
            var categoryOptions = GetCategoryOptions(store);
            var batchSize = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.SearchBunchSize);

            var categorySitemapItems = sitemap.Items.Where(x => x.ObjectType.EqualsInvariant(SitemapItemTypes.Category)).ToList();
            if (categorySitemapItems.Count > 0)
            {
                progressInfo.Description = $"Catalog: Starting records generation for {categorySitemapItems.Count} category items";
                progressCallback?.Invoke(progressInfo);

                foreach (var categorySiteMapItem in categorySitemapItems)
                {
                    int totalCount;
                    var listEntrySearchCriteria = AbstractTypeFactory<CatalogListEntrySearchCriteria>.TryCreateInstance();
                    listEntrySearchCriteria.CategoryId = categorySiteMapItem.ObjectId;
                    listEntrySearchCriteria.Take = batchSize;
                    listEntrySearchCriteria.HideDirectLinkedCategories = true;
                    listEntrySearchCriteria.SearchInChildren = true;
                    listEntrySearchCriteria.WithHidden = false;
                    listEntrySearchCriteria.SearchInVariations = true;

                    do
                    {
                        var result = await _listEntrySearchService.SearchAsync(listEntrySearchCriteria);
                        totalCount = result.TotalCount;
                        listEntrySearchCriteria.Skip += batchSize;

                        // Only used if should include images
                        List<CatalogProduct> products = new List<CatalogProduct>();
                        if (shouldIncludeImages)
                        {
                            // If images need to be included - run a search for picked products to get variations with images
                            var productIds = result.Results.Where(x => x is ProductListEntry).Select(x => x.Id).ToArray();
                            products = await SearchProductsWithVariations(productIds);
                        }

                        foreach (var listEntry in result.Results)
                        {
                            var itemRecords = GetSitemapItemRecords(store, categoryOptions, sitemap.UrlTemplate, baseUrl, listEntry).ToList();

                            if (shouldIncludeImages && listEntry is ProductListEntry)
                            {
                                var item = products.FirstOrDefault(x => x.Id == listEntry.Id);

                                if (item != null)
                                {
                                    // for each record per product add image urls to sitemap
                                    foreach (var record in itemRecords)
                                    {
                                        record.Images.AddRange(item.Images.Select(x => new SitemapItemImageRecord
                                        {
                                            Loc = x.Url
                                        }));
                                    }
                                }
                            }

                            categorySiteMapItem.ItemsRecords.AddRange(itemRecords);
                        }
                        progressInfo.Description = $"Catalog: Have been generated  {Math.Min(listEntrySearchCriteria.Skip, totalCount)} of {totalCount} records for category {categorySiteMapItem.Title} item";
                        progressCallback?.Invoke(progressInfo);

                    }
                    while (listEntrySearchCriteria.Skip < totalCount);
                }
            }
        }

        /// <summary>
        /// This helps keeping the imageless flow untouched for products
        /// </summary>
        /// <param name="store"></param>
        /// <param name="sitemap"></param>
        /// <param name="baseUrl"></param>
        /// <param name="progressCallback"></param>
        /// <returns></returns>
        protected virtual async Task LoadProductsSitemapItemRecordsAsync(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var shouldIncludeImages = store.Settings.GetValue<bool>(ModuleConstants.Settings.ProductLinks.IncludeImages);

            if (shouldIncludeImages)
            {
                await LoadProductsWithImages(store, sitemap, baseUrl, progressCallback);
            }
            else
            {
                await LoadProductsWithoutImages(store, sitemap, baseUrl, progressCallback);
            }
        }

        private async Task<List<CatalogProduct>> SearchProductsWithVariations(string[] productIds = null)
        {
            var products = (await _itemService.GetAsync(productIds, (ItemResponseGroup.Seo | ItemResponseGroup.Outlines | ItemResponseGroup.WithImages).ToString()))
    .Where(p => !p.IsActive.HasValue || p.IsActive.Value).ToList();

            return products;
        }

        /// <summary>
        /// This is used to load products with images
        /// Images are attached to corresponding product per item record
        /// </summary>
        /// <param name="store"></param>
        /// <param name="sitemap"></param>
        /// <param name="baseUrl"></param>
        /// <param name="progressCallback"></param>
        /// <returns></returns>
        private async Task LoadProductsWithImages(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var productSitemapItems = sitemap.Items.Where(x => x.ObjectType.EqualsInvariant(SitemapItemTypes.Product)).ToList();

            var productOptions = GetProductOptions(store);

            var productIds = productSitemapItems.Select(x => x.ObjectId).ToArray();

            var products = await SearchProductsWithVariations(productIds);

            var progressInfo = new ExportImportProgressInfo();

            var count = 0;

            foreach (var product in products)
            {
                var productSitemapItem = productSitemapItems.FirstOrDefault(x => x.ObjectId.EqualsInvariant(product.Id));
                if (productSitemapItem != null)
                {
                    var itemRecords = GetSitemapItemRecords(store, productOptions, sitemap.UrlTemplate, baseUrl, product);

                    foreach (var item in itemRecords)
                    {
                        var existingImages = product.Images.Where(x => !string.IsNullOrWhiteSpace(x.Url)).ToList();
                        if (existingImages.Count > 0)
                        {
                            item.Images.AddRange(existingImages.Select(x => new SitemapItemImageRecord
                            {
                                Loc = x.Url
                            }));
                        }
                    }

                    productSitemapItem.ItemsRecords.AddRange(itemRecords);
                }

                count++;
                progressInfo.Description = $"Catalog: Have been generated  {count} of {products.Count} records for products items";
                progressCallback?.Invoke(progressInfo);
            }
        }

        private async Task LoadProductsWithoutImages(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var batchSize = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.SearchBunchSize);

            var productSitemapItems = sitemap.Items.Where(x => x.ObjectType.EqualsInvariant(SitemapItemTypes.Product)).ToList();
            var skip = 0;
            var productOptions = GetProductOptions(store);

            var progressInfo = new ExportImportProgressInfo();

            do
            {
                var productIds = productSitemapItems.Select(x => x.ObjectId).Skip(skip).Take(batchSize).ToArray();

                var products = (await _itemService.GetAsync(productIds, (ItemResponseGroup.Seo | ItemResponseGroup.Outlines).ToString()))
                    .Where(p => !p.IsActive.HasValue || p.IsActive.Value);

                skip += batchSize;

                foreach (var product in products)
                {
                    var productSitemapItem = productSitemapItems.FirstOrDefault(x => x.ObjectId.EqualsInvariant(product.Id));
                    if (productSitemapItem != null)
                    {
                        var itemRecords = GetSitemapItemRecords(store, productOptions, sitemap.UrlTemplate, baseUrl, product);

                        productSitemapItem.ItemsRecords.AddRange(itemRecords);
                    }
                }
                progressInfo.Description = $"Catalog: Have been generated  {Math.Min(skip, productSitemapItems.Count)} of {productSitemapItems.Count} records for products items";
                progressCallback?.Invoke(progressInfo);
            }
            while (skip < productSitemapItems.Count);
        }

        private SitemapItemOptions GetProductOptions(Store store)
        {
            var storeOptionProductPriority = store.Settings.GetValue<decimal>(ModuleConstants.Settings.ProductLinks.ProductPagePriority);
            var storeOptionProductUpdateFrequency = store.Settings.GetValue<string>(ModuleConstants.Settings.ProductLinks.ProductPageUpdateFrequency);

            return new SitemapItemOptions
            {
                Priority = storeOptionProductPriority,
                UpdateFrequency = storeOptionProductUpdateFrequency
            };
        }

        private SitemapItemOptions GetCategoryOptions(Store store)
        {
            var storeOptionCategoryPriority = store.Settings.GetValue<decimal>(ModuleConstants.Settings.CategoryLinks.CategoryPagePriority);
            var storeOptionCategoryUpdateFrequency = store.Settings.GetValue<string>(ModuleConstants.Settings.CategoryLinks.CategoryPageUpdateFrequency);

            return new SitemapItemOptions
            {
                Priority = storeOptionCategoryPriority,
                UpdateFrequency = storeOptionCategoryUpdateFrequency
            };
        }
    }
}
