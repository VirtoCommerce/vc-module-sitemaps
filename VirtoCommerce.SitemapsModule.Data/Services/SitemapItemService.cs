using System;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.SitemapsModule.Core.Model;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Model;
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

            using (var repository = RepositoryFactory())
            {
                var response = new SearchResponse<SitemapItem>();

                var sitemapItemEntities = repository.SitemapItems.Where(i => i.SitemapId == request.SitemapId);
                response.TotalCount = sitemapItemEntities.Count();
                foreach (var sitemapItemEntity in sitemapItemEntities.OrderByDescending(i => i.CreatedDate).Skip(request.Skip).Take(request.Take))
                {
                    var sitemapItem = AbstractTypeFactory<SitemapItem>.TryCreateInstance();
                    if (sitemapItem != null)
                    {
                        response.Items.Add(sitemapItemEntity.ToModel(sitemapItem));
                    }
                }

                return response;
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

        public virtual void Remove(string sitemapId, string[] sitemapItemIds)
        {
            if (string.IsNullOrEmpty(sitemapId))
            {
                throw new ArgumentException("sitemapId");
            }
            if (sitemapItemIds == null)
            {
                throw new ArgumentNullException("sitemapItemIds");
            }

            using (var repository = RepositoryFactory())
            {
                var sitemapEntities = repository.GetSitemapsByIds(new[] { sitemapId });
                if (sitemapEntities != null && sitemapEntities.Any())
                {
                    var sitemapEntity = sitemapEntities.First();
                    foreach (var sitemapItemId in sitemapItemIds)
                    {
                        var sitemapItemEntity = repository.SitemapItems.FirstOrDefault(i => i.SitemapId == sitemapEntity.Id && i.Id == sitemapItemId);
                        repository.Remove(sitemapItemEntity);
                        sitemapEntity.Items.Remove(sitemapItemEntity);
                    }
                }

                CommitChanges(repository);
            }
        }
    }
}