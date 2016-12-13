using System;
using System.Data.Entity;
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
    public class SitemapService : ServiceBase, ISitemapService
    {
        public SitemapService(
            Func<ISitemapRepository> repositoryFactory,
            ISitemapItemService sitemapItemService)
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
                        var sitemapItemsSearchResponse = SitemapItemService.Search(new SitemapItemSearchCriteria
                        {
                            SitemapId = sitemap.Id
                        });

                        sitemap = sitemapEntity.ToModel(sitemap);
                        sitemap.Items = sitemapItemsSearchResponse.Results;
                        sitemap.TotalItemsCount = sitemapItemsSearchResponse.TotalCount;
                    }
                }

                return sitemap;
            }
        }

        public virtual GenericSearchResult<Sitemap> Search(SitemapSearchCriteria request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            using (var repository = RepositoryFactory())
            {
                var searchResponse = new GenericSearchResult<Sitemap>();

                var sitemapEntities = repository.Sitemaps;

                if (!string.IsNullOrEmpty(request.StoreId))
                {
                    sitemapEntities = sitemapEntities.Where(s => s.StoreId == request.StoreId);
                }
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
                        var sitemapItemsSearchResponse = SitemapItemService.Search(new SitemapItemSearchCriteria
                        {
                            SitemapId = sitemapEntity.Id
                        });

                        sitemap = sitemapEntity.ToModel(sitemap);
                        sitemap.TotalItemsCount = sitemapItemsSearchResponse.TotalCount;
                        searchResponse.Results.Add(sitemap);
                    }
                }

                return searchResponse;
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

                var sitemapIds = sitemaps.Where(s => !s.IsTransient()).Select(s => s.Id);
                var sitemapExistEntities = repository.Sitemaps.Where(s => sitemapIds.Contains(s.Id));
                foreach (var sitemap in sitemaps)
                {
                    var sitemapSourceEntity = AbstractTypeFactory<SitemapEntity>.TryCreateInstance();
                    if (sitemapSourceEntity != null)
                    {
                        sitemapSourceEntity.FromModel(sitemap, pkMap);
                        var sitemapTargetEntity = sitemapExistEntities.FirstOrDefault(s => s.Id == sitemap.Id);
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