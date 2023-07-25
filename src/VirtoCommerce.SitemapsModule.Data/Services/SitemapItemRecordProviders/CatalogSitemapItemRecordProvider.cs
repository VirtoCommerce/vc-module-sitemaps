using System;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model;
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
            var progressInfo = new ExportImportProgressInfo();
            var categoryOptions = await GetCategoryOptions(store);
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
                        foreach (var listEntry in result.Results)
                        {
                            categorySiteMapItem.ItemsRecords.AddRange(GetSitemapItemRecords(store, categoryOptions, sitemap.UrlTemplate, baseUrl, listEntry));
                        }
                        progressInfo.Description = $"Catalog: Have been generated  {Math.Min(listEntrySearchCriteria.Skip, totalCount)} of {totalCount} records for category {categorySiteMapItem.Title} item";
                        progressCallback?.Invoke(progressInfo);

                    }
                    while (listEntrySearchCriteria.Skip < totalCount);
                }
            }
        }

        protected virtual async Task LoadProductsSitemapItemRecordsAsync(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var progressInfo = new ExportImportProgressInfo();
            var productOptions = await GetProductOptions(store);
            var batchSize = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.SearchBunchSize);

            var skip = 0;
            var productSitemapItems = sitemap.Items.Where(x => x.ObjectType.EqualsInvariant(SitemapItemTypes.Product)).ToList();
            if (productSitemapItems.Count > 0)
            {
                progressInfo.Description = $"Catalog: Starting records generation  for {productSitemapItems.Count} products items";
                progressCallback?.Invoke(progressInfo);

                do
                {
                    var productIds = productSitemapItems.Select(x => x.ObjectId).Skip(skip).Take(batchSize).ToArray();
                    var products = (await _itemService.GetAsync(productIds, (ItemResponseGroup.Seo | ItemResponseGroup.Outlines).ToString())).Where(p => !p.IsActive.HasValue || p.IsActive.Value);
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
        }

        private async Task<SitemapItemOptions> GetProductOptions(Store store)
        {
            var storeOptionProductPriority = store.Settings.GetSettingValue(ModuleConstants.Settings.ProductLinks.ProductPagePriority.Name, decimal.MinusOne);
            var storeOptionProductUpdateFrequency = store.Settings.GetSettingValue(ModuleConstants.Settings.ProductLinks.ProductPageUpdateFrequency.Name, "");

            return new SitemapItemOptions
            {
                Priority = storeOptionProductPriority > -1
                    ? storeOptionProductPriority
                    : await _settingsManager.GetValueAsync<decimal>(ModuleConstants.Settings.ProductLinks.ProductPagePriority),
                UpdateFrequency = !string.IsNullOrEmpty(storeOptionProductUpdateFrequency)
                    ? storeOptionProductUpdateFrequency
                    : await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.ProductLinks.ProductPageUpdateFrequency),
            };
        }

        private async Task<SitemapItemOptions> GetCategoryOptions(Store store)
        {
            var storeOptionCategoryPriority = store.Settings.GetSettingValue(ModuleConstants.Settings.CategoryLinks.CategoryPagePriority.Name, decimal.MinusOne);
            var storeOptionCategoryUpdateFrequency = store.Settings.GetSettingValue(ModuleConstants.Settings.CategoryLinks.CategoryPageUpdateFrequency.Name, "");

            return new SitemapItemOptions
            {
                Priority = storeOptionCategoryPriority > -1
                    ? storeOptionCategoryPriority
                    : await _settingsManager.GetValueAsync<decimal>(ModuleConstants.Settings.CategoryLinks.CategoryPagePriority),
                UpdateFrequency = !string.IsNullOrEmpty(storeOptionCategoryUpdateFrequency)
                    ? storeOptionCategoryUpdateFrequency
                    : await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.CategoryLinks.CategoryPageUpdateFrequency),
            };
        }
    }
}
