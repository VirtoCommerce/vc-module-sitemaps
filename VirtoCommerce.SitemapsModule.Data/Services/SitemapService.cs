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
    public class SitemapService : ServiceBase, ISitemapService
    {
        public SitemapService(Func<ISitemapRepository> repositoryFactory)
        {
            RepositoryFactory = repositoryFactory;
        }

        protected Func<ISitemapRepository> RepositoryFactory { get; private set; }

        public virtual SearchResponse<Sitemap> Search(SitemapSearchRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            using (var repository = RepositoryFactory())
            {
                var response = new SearchResponse<Sitemap>();

                var sitemapEntities = repository.Sitemaps.Where(s => s.StoreId == request.StoreId);
                if (request.SitemapIds != null && request.SitemapIds.Any())
                {
                    sitemapEntities = repository.GetSitemapsByIds(request.SitemapIds).Where(s => s.StoreId == request.StoreId).AsQueryable();
                }
                response.TotalCount = sitemapEntities.Count();

                foreach (var sitemapEntity in sitemapEntities.OrderByDescending(s => s.CreatedDate).Skip(request.Skip).Take(request.Take))
                {
                    var sitemap = AbstractTypeFactory<Sitemap>.TryCreateInstance();
                    if (sitemap != null)
                    {
                        response.Items.Add(sitemapEntity.ToModel(sitemap));
                    }
                }

                return response;
            }
        }

        public virtual void SaveChanges(Sitemap[] sitemaps)
        {
            if (sitemaps == null)
            {
                throw new ArgumentNullException("sitemaps");
            }

            using (var repository = RepositoryFactory())
            {
                var pkMap = new PrimaryKeyResolvingMap();
                var changeTracker = GetChangeTracker(repository);

                var existSitemapEntities = repository.GetSitemapsByIds(sitemaps.Where(s => !s.IsTransient()).Select(s => s.Id).ToArray());
                foreach (var sitemap in sitemaps)
                {
                    var sitemapSourceEntity = AbstractTypeFactory<SitemapEntity>.TryCreateInstance();
                    if (sitemapSourceEntity != null)
                    {
                        sitemapSourceEntity.FromModel(sitemap, pkMap);
                        var sitemapTargetEntity = existSitemapEntities.FirstOrDefault(s => s.StoreId == sitemap.StoreId && s.Id == sitemap.Id);
                        if (sitemapTargetEntity != null)
                        {
                            changeTracker.Attach(sitemapTargetEntity);
                            sitemapSourceEntity.Patch(sitemapTargetEntity);
                        }
                        else
                        {
                            repository.Add(sitemapSourceEntity);
                        }
                    }
                }

                CommitChanges(repository);
                pkMap.ResolvePrimaryKeys();
            }
        }

        public virtual void DeleteSitemaps(string storeId, string[] sitemapIds)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                throw new ArgumentException("storeId");
            }
            if (sitemapIds == null)
            {
                throw new ArgumentException("sitemapIds");
            }

            using (var repository = RepositoryFactory())
            {
                var storeSitemaps = repository.Sitemaps.Where(s => s.StoreId == storeId);
                foreach (var sitemapId in sitemapIds)
                {
                    var sitemapEntity = storeSitemaps.FirstOrDefault(s => s.Id == sitemapId);
                    if (sitemapEntity != null)
                    {
                        repository.Remove(sitemapEntity);
                    }
                }

                CommitChanges(repository);
            }
        }
    }
}