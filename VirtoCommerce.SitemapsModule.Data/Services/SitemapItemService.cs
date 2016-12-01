using System;
using System.Linq;
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

        public virtual SearchResponse<SitemapItem> Search(SitemapItemSearchRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (string.IsNullOrEmpty(request.SitemapId))
            {
                throw new ArgumentException("request.sitemapId");
            }

            using (var repository = RepositoryFactory())
            {
                var searchResponse = new SearchResponse<SitemapItem>();

                var sitemapItemEntities = repository.SitemapItems.Where(i => i.SitemapId == request.SitemapId).OrderByDescending(i => i.CreatedDate);
                searchResponse.TotalCount = sitemapItemEntities.Count();

                if (sitemapItemEntities.Any())
                {
                    foreach (var sitemapItemEntity in sitemapItemEntities.Skip(request.Skip).Take(request.Take))
                    {
                        var sitemapItem = AbstractTypeFactory<SitemapItem>.TryCreateInstance();
                        if (sitemapItem != null)
                        {
                            searchResponse.Items.Add(sitemapItemEntity.ToModel(sitemapItem));
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

        public virtual void Remove(string sitemapId, string[] itemIds)
        {
            if (string.IsNullOrEmpty(sitemapId))
            {
                throw new ArgumentException("sitemapId");
            }
            if (itemIds == null)
            {
                throw new ArgumentNullException("itemIds");
            }

            using (var repository = RepositoryFactory())
            {
                var sitemapEntity = repository.Sitemaps.FirstOrDefault(s => s.Id == sitemapId);
                if (sitemapEntity != null)
                {
                    var sitemapItemEntities = repository.SitemapItems.Where(i => itemIds.Contains(i.Id));
                    if (sitemapItemEntities.Any())
                    {
                        foreach (var sitemapItemEntity in sitemapItemEntities)
                        {
                            repository.Remove(sitemapItemEntity);
                            sitemapEntity.Items.Remove(sitemapItemEntity);
                        }

                        CommitChanges(repository);
                    }
                }
            }
        }
    }
}