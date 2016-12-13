using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Commerce.Model.Search;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Models;
using VirtoCommerce.SitemapsModule.Data.Repositories;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapItemService : ServiceBase, ISitemapItemService
    {
        public SitemapItemService(
            Func<ISitemapRepository> repositoryFactory,
            IStoreService storeService,
            ICatalogSearchService catalogSearchService,
            ICategoryService categoryService,
            IItemService itemService)
        {
            RepositoryFactory = repositoryFactory;
            StoreService = storeService;
            CatalogSearchService = catalogSearchService;
            CategoryService = categoryService;
            ItemService = itemService;
        }

        protected Func<ISitemapRepository> RepositoryFactory { get; private set; }
        protected IStoreService StoreService { get; private set; }
        protected ICatalogSearchService CatalogSearchService { get; private set; }
        protected ICategoryService CategoryService { get; private set; }
        protected IItemService ItemService { get; private set; }

        public virtual GenericSearchResult<SitemapItem> Search(SitemapItemSearchCriteria criteria)
        {
            if (criteria == null)
            {
                throw new ArgumentNullException("request");
            }

            using (var repository = RepositoryFactory())
            {
                var searchResponse = new GenericSearchResult<SitemapItem>();

                var sitemapItemEntities = repository.SitemapItems.Where(i => i.SitemapId == criteria.SitemapId);
                searchResponse.TotalCount = sitemapItemEntities.Count();

                if (criteria.ObjectTypes != null)
                {
                    sitemapItemEntities = sitemapItemEntities.Where(i => criteria.ObjectTypes.Contains(i.ObjectType, StringComparer.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrEmpty(criteria.ObjectType))
                {
                    sitemapItemEntities = sitemapItemEntities.Where(i => i.ObjectType.EqualsInvariant(criteria.ObjectType));
                }

                if (sitemapItemEntities.Any())
                {
                    foreach (var sitemapItemEntity in sitemapItemEntities.OrderByDescending(i => i.CreatedDate).Skip(criteria.Skip).Take(criteria.Take))
                    {
                        var sitemapItem = AbstractTypeFactory<SitemapItem>.TryCreateInstance();
                        if (sitemapItem != null)
                        {
                            searchResponse.Results.Add(sitemapItemEntity.ToModel(sitemapItem));
                        }
                    }
                }

                return searchResponse;
            }
        }

        public virtual void Add(string sitemapId, SitemapItem[] sitemapItems)
        {
            if (string.IsNullOrEmpty(sitemapId))
            {
                throw new ArgumentException("sitemapId");
            }
            if (sitemapItems == null)
            {
                throw new ArgumentNullException("sitemapItems");
            }

            using (var repository = RepositoryFactory())
            {
                var pkMap = new PrimaryKeyResolvingMap();

                var sitemapEntity = repository.Sitemaps.FirstOrDefault(s => s.Id == sitemapId);
                if (sitemapEntity != null)
                {
                    foreach (var sitemapItem in sitemapItems)
                    {
                        var sitemapItemEntity = AbstractTypeFactory<SitemapItemEntity>.TryCreateInstance();
                        sitemapEntity.Items.Add(sitemapItemEntity.FromModel(sitemapItem, pkMap));
                    }
                }

                CommitChanges(repository);
                pkMap.ResolvePrimaryKeys();
            }
        }

        public virtual void Remove(string[] itemIds)
        {
            if (itemIds == null)
            {
                throw new ArgumentNullException("itemIds");
            }

            using (var repository = RepositoryFactory())
            {
                var sitemapItemEntities = repository.SitemapItems.Where(i => itemIds.Contains(i.Id));
                if (sitemapItemEntities.Any())
                {
                    foreach (var sitemapItemEntity in sitemapItemEntities)
                    {
                        repository.Remove(sitemapItemEntity);
                    }

                    CommitChanges(repository);
                }
            }
        }

        public virtual ICollection<ISeoSupport> GetIncludedSitemapItems(string sitemapId, int limit)
        {
            using (var repository = RepositoryFactory())
            {
                var includedItems = new List<ISeoSupport>();

                var sitemap = repository.Sitemaps.FirstOrDefault(s => s.Id == sitemapId);
                if (sitemap != null)
                {
                    var sitemapItems = repository.SitemapItems.Where(i => i.SitemapId == sitemap.Id);

                    var store = StoreService.GetById(sitemap.StoreId);
                    if (store != null)
                    {
                        var catalogItems = GetCatalogSitemapItems(store, sitemapItems, limit);
                        includedItems.AddRange(catalogItems);
                    }
                }

                return includedItems;
            }
        }

        private ICollection<ISeoSupport> GetCatalogSitemapItems(Store store, IQueryable<SitemapItemEntity> sitemapItems, int limit)
        {
            var catalogItems = new List<ISeoSupport>();

            var categorySitemapItems = sitemapItems.Where(i => i.ObjectType.Equals("category", StringComparison.OrdinalIgnoreCase));
            var productSitemapItems = sitemapItems.Where(i => i.ObjectType.Equals("product", StringComparison.OrdinalIgnoreCase));

            var catalogItemsSearchResult = CatalogSearchService.Search(new Domain.Catalog.Model.SearchCriteria
            {
                CatalogId = store.Catalog,
                CategoryIds = categorySitemapItems.Select(i => i.ObjectId).ToArray(),
                ResponseGroup = Domain.Catalog.Model.SearchResponseGroup.WithCategories | Domain.Catalog.Model.SearchResponseGroup.WithProducts,
                StoreId = store.Id,
                Take = limit
            });

            catalogItems.AddRange(catalogItemsSearchResult.Categories);
            catalogItems.AddRange(catalogItemsSearchResult.Products);

            var catalogItemIds = catalogItems.Select(ci => ci.Id);

            var unincludedCategoryItemIds = categorySitemapItems.Where(si => !catalogItemIds.Contains(si.ObjectId)).Select(si => si.ObjectId).ToArray();
            var uninludedCategories = CategoryService.GetByIds(unincludedCategoryItemIds, Domain.Catalog.Model.CategoryResponseGroup.WithSeo);
            catalogItems.AddRange(uninludedCategories);

            var unincludedProductItemIds = productSitemapItems.Where(si => !catalogItemIds.Contains(si.ObjectId)).Select(si => si.ObjectId).ToArray();
            var unincludedProducts = ItemService.GetByIds(unincludedProductItemIds, Domain.Catalog.Model.ItemResponseGroup.Seo);
            catalogItems.AddRange(unincludedProducts);

            return catalogItems;
        }
    }
}