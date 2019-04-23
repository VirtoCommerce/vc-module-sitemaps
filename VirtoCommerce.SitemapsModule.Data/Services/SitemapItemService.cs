using System;
using System.Linq;
using VirtoCommerce.Domain.Commerce.Model.Search;
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
        public SitemapItemService(Func<ISitemapRepository> repositoryFactory)
        {
            RepositoryFactory = repositoryFactory;
        }

        protected Func<ISitemapRepository> RepositoryFactory { get; private set; }

        public virtual GenericSearchResult<SitemapItem> Search(SitemapItemSearchCriteria criteria)
        {
            if (criteria == null)
            {
                throw new ArgumentNullException("request");
            }

            using (var repository = RepositoryFactory())
            {
                var searchResponse = new GenericSearchResult<SitemapItem>();
                var query = repository.SitemapItems;
                if (!string.IsNullOrEmpty(criteria.SitemapId))
                {
                    query = query.Where(x => x.SitemapId == criteria.SitemapId);
                }
                if (criteria.ObjectTypes != null)
                {
                    query = query.Where(i => criteria.ObjectTypes.Contains(i.ObjectType, StringComparer.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrEmpty(criteria.ObjectType))
                {
                    query = query.Where(i => i.ObjectType.EqualsInvariant(criteria.ObjectType));
                }

                var sortInfos = criteria.SortInfos;
                if (sortInfos.IsNullOrEmpty())
                {
                    sortInfos = new[] { new SortInfo { SortColumn = ReflectionUtility.GetPropertyName<SitemapItemEntity>(x => x.CreatedDate), SortDirection = SortDirection.Descending } };
                }
                query = query.OrderBySortInfos(sortInfos).ThenBy(x => x.Id);
                searchResponse.TotalCount = query.Count();

                foreach (var sitemapItemEntity in query.Skip(criteria.Skip).Take(criteria.Take))
                {
                    var sitemapItem = AbstractTypeFactory<SitemapItem>.TryCreateInstance();
                    if (sitemapItem != null)
                    {
                        searchResponse.Results.Add(sitemapItemEntity.ToModel(sitemapItem));
                    }
                }
                return searchResponse;
            }
        }

        public virtual void SaveChanges(SitemapItem[] sitemapItems)
        {
            if (sitemapItems == null)
            {
                throw new ArgumentNullException("sitemapItems");
            }

            using (var repository = RepositoryFactory())
            using (var changeTracker = GetChangeTracker(repository))
            {
                var pkMap = new PrimaryKeyResolvingMap();
                var itemsIds = sitemapItems.Select(x => x.Id).Where(x => x != null).Distinct().ToArray();
                var existEntities = repository.SitemapItems.Where(s => itemsIds.Contains(s.Id));
                foreach (var sitemapItem in sitemapItems)
                {
                    var changedEntity = AbstractTypeFactory<SitemapItemEntity>.TryCreateInstance().FromModel(sitemapItem, pkMap);
                    var existEntity = existEntities.FirstOrDefault(x => x.Id == sitemapItem.Id);
                    if (existEntity != null)
                    {
                        changeTracker.Attach(existEntity);
                        changedEntity.Patch(existEntity);
                    }
                    else
                    {
                        repository.Add(changedEntity);
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
    }
}
