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
        private readonly ICategoryService _сategoryService;
        private readonly IItemService _itemService;
        private readonly IListEntrySearchService _listEntrySearchService;

        public CatalogSitemapItemRecordProvider(ISettingsManager settingsManager, ISitemapUrlBuilder urlBuilider)
            : base(settingsManager, urlBuilider)
        {
        }

        public CatalogSitemapItemRecordProvider(
            ICategoryService categoryService,
            IItemService itemService,
            IListEntrySearchService listEntrySearchService,
            ISitemapUrlBuilder urlBuilder,
            ISettingsManager settingsManager)
            : base(settingsManager, urlBuilder)
        {
            _сategoryService = categoryService;
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

            var productOptions = GetProductOptions(store);
            var categoryOptions = GetCategoryOptions(store);
            var batchSize = SettingsManager.GetValue(ModuleConstants.Settings.General.SearchBunchSize.Name, (int)ModuleConstants.Settings.General.SearchBunchSize.DefaultValue);

            var categorySitemapItems = sitemap.Items.Where(x => x.ObjectType.EqualsInvariant(SitemapItemTypes.Category))
                                                    .ToList();
            if (categorySitemapItems.Count > 0)
            {
                progressInfo.Description = $"Catalog: Starting records generation for {categorySitemapItems.Count} category items";
                progressCallback?.Invoke(progressInfo);

                foreach (var categorySiteMapItem in categorySitemapItems)
                {
                    var totalCount = 0;
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
                        progressInfo.Description = $"Catalog: Have been generated  { Math.Min(listEntrySearchCriteria.Skip, totalCount) } of {totalCount} records for category { categorySiteMapItem.Title } item";
                        progressCallback?.Invoke(progressInfo);

                    }
                    while (listEntrySearchCriteria.Skip < totalCount);
                }
            }
        }

        protected virtual async Task LoadProductsSitemapItemRecordsAsync(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var progressInfo = new ExportImportProgressInfo();
            var productOptions = GetProductOptions(store);
            var batchSize = SettingsManager.GetValue(ModuleConstants.Settings.General.SearchBunchSize.Name, (int)ModuleConstants.Settings.General.SearchBunchSize.DefaultValue);

            var skip = 0;
            var productSitemapItems = sitemap.Items.Where(x => x.ObjectType.EqualsInvariant(SitemapItemTypes.Product)).ToList();
            if (productSitemapItems.Count > 0)
            {
                progressInfo.Description = $"Catalog: Starting records generation  for {productSitemapItems.Count} products items";
                progressCallback?.Invoke(progressInfo);

                do
                {
                    var productIds = productSitemapItems.Select(x => x.ObjectId).Skip(skip).Take(batchSize).ToArray();
                    var products = (await _itemService.GetByIdsAsync(productIds, (ItemResponseGroup.Seo | ItemResponseGroup.Outlines).ToString())).Where(p => !p.IsActive.HasValue || p.IsActive.Value);
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
                    progressInfo.Description = $"Catalog: Have been generated  { Math.Min(skip, productSitemapItems.Count) } of {productSitemapItems.Count} records for products items";
                    progressCallback?.Invoke(progressInfo);
                }
                while (skip < productSitemapItems.Count);
            }
        }

        private static SitemapItemOptions GetStoreOptions(Store store)
        {
            return new SitemapItemOptions
            {
                Priority = store.Settings.GetSettingValue(ModuleConstants.Settings.BlogLinks.BlogPagePriority.Name, decimal.MinusOne),
                UpdateFrequency = store.Settings.GetSettingValue(ModuleConstants.Settings.BlogLinks.BlogPageUpdateFrequency.Name, "")
            };
        }

        private SitemapItemOptions GetProductOptions(Store store)
        {
            var storeOptions = GetStoreOptions(store);
            return new SitemapItemOptions
            {
                Priority = storeOptions.Priority > -1 ?
                    storeOptions.Priority : SettingsManager.GetValue(ModuleConstants.Settings.ProductLinks.ProductPagePriority.Name, 1.0M),
                UpdateFrequency = !string.IsNullOrEmpty(storeOptions.UpdateFrequency) ?
                    storeOptions.UpdateFrequency : SettingsManager.GetValue(ModuleConstants.Settings.ProductLinks.ProductPageUpdateFrequency.Name, UpdateFrequency.Daily)
            };
        }

        private SitemapItemOptions GetCategoryOptions(Store store)
        {
            var storeOptions = GetStoreOptions(store);
            return new SitemapItemOptions
            {
                Priority = storeOptions.Priority > -1 ?
                    storeOptions.Priority : SettingsManager.GetValue(ModuleConstants.Settings.CategoryLinks.CategoryPagePriority.Name, .7M),
                UpdateFrequency = !string.IsNullOrEmpty(storeOptions.UpdateFrequency) ?
                    storeOptions.UpdateFrequency : SettingsManager.GetValue(ModuleConstants.Settings.CategoryLinks.CategoryPageUpdateFrequency.Name, UpdateFrequency.Weekly)
            };
        }

    }
}
