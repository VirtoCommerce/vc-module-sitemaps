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
    public class SitemapService : ServiceBase, ISitemapService
    {
        public SitemapService(Func<ISitemapRepository> repositoryFactory, ISitemapItemService sitemapItemService)
        {
            RepositoryFactory = repositoryFactory;
            SitemapItemService = sitemapItemService;
        }

        protected Func<ISitemapRepository> RepositoryFactory { get; private set; }
        protected ISitemapItemService SitemapItemService { get; private set; }

        public virtual Sitemap GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id");
            }

            using (var repository = RepositoryFactory())
            {
                Sitemap sitemap = null;

                var sitemapEntity = repository.Sitemaps.FirstOrDefault(s => s.Id == id);
                if (sitemapEntity != null)
                {
                    sitemap = AbstractTypeFactory<Sitemap>.TryCreateInstance();
                    if (sitemap != null)
                    {
                        var sitemapItemSearchResponse = SitemapItemService.Search(new SitemapItemSearchRequest
                        {
                            SitemapId = id
                        });

                        sitemap = sitemapEntity.ToModel(sitemap);
                        sitemap.Items = sitemapItemSearchResponse.Items;
                        sitemap.ItemsTotalCount = sitemapItemSearchResponse.TotalCount;
                    }
                }

                return sitemap;
            }
        }

        public virtual SearchResponse<Sitemap> Search(SitemapSearchRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (string.IsNullOrEmpty(request.StoreId))
            {
                throw new ArgumentException("request.storeId");
            }

            using (var repository = RepositoryFactory())
            {
                var searchResponse = new SearchResponse<Sitemap>();

                var sitemapEntities = repository.Sitemaps.Where(s => s.StoreId == request.StoreId);

                if (!string.IsNullOrEmpty(request.Filename))
                {
                    sitemapEntities = sitemapEntities.Where(s => s.Filename == request.Filename);
                }

                searchResponse.TotalCount = sitemapEntities.Count();

                foreach (var sitemapEntity in sitemapEntities.OrderByDescending(s => s.CreatedDate).Skip(request.Skip).Take(request.Take))
                {
                    var sitemap = AbstractTypeFactory<Sitemap>.TryCreateInstance();
                    if (sitemap != null)
                    {
                        sitemap = sitemapEntity.ToModel(sitemap);
                        sitemap.ItemsTotalCount = repository.SitemapItems.Where(i => i.SitemapId == sitemap.Id).Count();
                        searchResponse.Items.Add(sitemap);
                    }
                }

                return searchResponse;
            }
        }

        public virtual void SaveChanges(Sitemap sitemap)
        {
            if (sitemap == null)
            {
                throw new ArgumentNullException("sitemap");
            }

            using (var repository = RepositoryFactory())
            {
                var pkMap = new PrimaryKeyResolvingMap();
                var changeTracker = GetChangeTracker(repository);

                var sitemapSourceEntity = AbstractTypeFactory<SitemapEntity>.TryCreateInstance();
                if (sitemapSourceEntity != null)
                {
                    sitemapSourceEntity.FromModel(sitemap, pkMap);

                    var sitemapTargetEntity = repository.Sitemaps.FirstOrDefault(s => s.Id == sitemap.Id);
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

                CommitChanges(repository);
                pkMap.ResolvePrimaryKeys();
            }
        }

        public virtual void Remove(string[] ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("ids");
            }

            using (var repository = RepositoryFactory())
            {
                var sitemapEntities = repository.Sitemaps.Where(s => ids.Contains(s.Id));
                foreach (var sitemapEntity in sitemapEntities)
                {
                    repository.Remove(sitemapEntity);
                }

                CommitChanges(repository);
            }
        }
    }
}