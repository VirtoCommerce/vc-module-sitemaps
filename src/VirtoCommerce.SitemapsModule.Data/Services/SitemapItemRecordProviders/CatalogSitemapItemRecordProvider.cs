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

        private const ItemResponseGroup _noImages = ItemResponseGroup.Seo | ItemResponseGroup.Outlines;
        private const ItemResponseGroup _withImages = ItemResponseGroup.Seo | ItemResponseGroup.Outlines | ItemResponseGroup.WithImages;

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
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(sitemap);

            await LoadCategoriesSitemapItemRecordsAsync(store, sitemap, baseUrl, progressCallback);
            await LoadProductsSitemapItemRecordsAsync(store, sitemap, baseUrl, progressCallback);
        }

        #endregion

        protected virtual async Task LoadCategoriesSitemapItemRecordsAsync(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var categoryItems = GetSitemapItems(sitemap, SitemapItemTypes.Category);
            if (categoryItems.Count == 0)
            {
                return;
            }

            var batchSize = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.SearchBunchSize);
            var includeImages = store.Settings.GetValue<bool>(ModuleConstants.Settings.ProductLinks.IncludeImages);

            var progressInfo = new ExportImportProgressInfo();
            var categoryOptions = GetCategoryOptions(store);

            progressInfo.Description = $"Catalog: Starting records generation for {categoryItems.Count} category items";
            progressCallback?.Invoke(progressInfo);

            foreach (var categoryItem in categoryItems)
            {
                int totalCount;
                var categoryId = categoryItem.ObjectId;

                var searchCriteria = AbstractTypeFactory<CatalogListEntrySearchCriteria>.TryCreateInstance();
                searchCriteria.CategoryId = categoryId;
                searchCriteria.Take = batchSize;
                searchCriteria.HideDirectLinkedCategories = true;
                searchCriteria.SearchInChildren = true;
                searchCriteria.WithHidden = false;
                searchCriteria.SearchInVariations = true;

                do
                {
                    var searchResult = await _listEntrySearchService.SearchAsync(searchCriteria);
                    var listEntries = searchResult.Results;
                    totalCount = searchResult.TotalCount;
                    searchCriteria.Skip += batchSize;

                    // Load products only if we need images
                    var products = new List<CatalogProduct>();
                    if (includeImages)
                    {
                        var productIds = listEntries.Where(x => x is ProductListEntry).Select(x => x.Id).ToArray();
                        products = await GetActiveProducts(productIds, _withImages);
                    }

                    foreach (var listEntry in listEntries)
                    {
                        var records = GetSitemapItemRecords(store, categoryOptions, sitemap.UrlTemplate, baseUrl, listEntry, categoryId).ToList();

                        if (includeImages && listEntry is ProductListEntry)
                        {
                            var product = products.FirstOrDefault(x => x.Id == listEntry.Id);
                            if (product != null)
                            {
                                AddImages(product, records);
                            }
                        }

                        foreach (var record in records)
                        {
                            record.ObjectType = listEntry is ProductListEntry ? "product" : "category";
                        }

                        categoryItem.ItemsRecords.AddRange(records);
                    }

                    progressInfo.Description = $"Catalog: Have been generated  {Math.Min(searchCriteria.Skip, totalCount)} of {totalCount} records for category {categoryItem.Title} item";
                    progressCallback?.Invoke(progressInfo);
                }
                while (searchCriteria.Skip < totalCount);
            }
        }

        protected virtual async Task LoadProductsSitemapItemRecordsAsync(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var productItems = GetSitemapItems(sitemap, SitemapItemTypes.Product);
            if (productItems.Count == 0)
            {
                return;
            }

            var batchSize = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.SearchBunchSize);
            var includeImages = store.Settings.GetValue<bool>(ModuleConstants.Settings.ProductLinks.IncludeImages);

            var responseGroup = includeImages ? _withImages : _noImages;

            var progressInfo = new ExportImportProgressInfo();
            var productOptions = GetProductOptions(store);
            var processedCount = 0;

            foreach (var items in productItems.Paginate(batchSize))
            {
                var productIds = items.Select(x => x.ObjectId).ToArray();
                var products = await GetActiveProducts(productIds, responseGroup);

                foreach (var product in products)
                {
                    var item = items.FirstOrDefault(x => x.ObjectId.EqualsIgnoreCase(product.Id));
                    if (item != null)
                    {
                        var records = GetSitemapItemRecords(store, productOptions, sitemap.UrlTemplate, baseUrl, product);
                        item.ItemsRecords.AddRange(records);

                        if (includeImages)
                        {
                            AddImages(product, records);
                        }
                    }
                }

                processedCount += items.Count;
                progressInfo.Description = $"Catalog: Have been generated  {processedCount} of {productItems.Count} records for products items";
                progressCallback?.Invoke(progressInfo);
            }
        }

        private static List<SitemapItem> GetSitemapItems(Sitemap sitemap, string itemType)
        {
            return sitemap.Items
                .Where(x => x.ObjectType.EqualsIgnoreCase(itemType))
                .GroupBy(x => x.ObjectId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        private static SitemapItemOptions GetProductOptions(Store store)
        {
            return new SitemapItemOptions
            {
                Priority = store.Settings.GetValue<decimal>(ModuleConstants.Settings.ProductLinks.ProductPagePriority),
                UpdateFrequency = store.Settings.GetValue<string>(ModuleConstants.Settings.ProductLinks.ProductPageUpdateFrequency),
            };
        }

        private static SitemapItemOptions GetCategoryOptions(Store store)
        {
            return new SitemapItemOptions
            {
                Priority = store.Settings.GetValue<decimal>(ModuleConstants.Settings.CategoryLinks.CategoryPagePriority),
                UpdateFrequency = store.Settings.GetValue<string>(ModuleConstants.Settings.CategoryLinks.CategoryPageUpdateFrequency),
            };
        }

        private async Task<List<CatalogProduct>> GetActiveProducts(IList<string> ids, ItemResponseGroup responseGroup)
        {
            var products = await _itemService.GetAsync(ids, responseGroup.ToString());

            return products
                .Where(x => x.IsActive is null || x.IsActive.Value)
                .ToList();
        }

        private static void AddImages(CatalogProduct product, IList<SitemapItemRecord> records)
        {
            foreach (var record in records)
            {
                var images = product.Images.Where(x => !string.IsNullOrWhiteSpace(x.Url)).ToList();
                if (images.Count > 0)
                {
                    record.Images.AddRange(images.Select(x => new SitemapItemImageRecord { Loc = x.Url }));
                }
            }
        }
    }
}
