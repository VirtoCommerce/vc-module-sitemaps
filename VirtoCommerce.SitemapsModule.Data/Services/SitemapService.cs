using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;
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

        public virtual SitemapEntity GetSitemapById(string storeId, string sitemapId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                throw new ArgumentException("storeId");
            }
            if (string.IsNullOrEmpty(sitemapId))
            {
                throw new ArgumentException("sitemapId");
            }

            using (var repository = RepositoryFactory())
            {
                return repository.Sitemaps.Include(s => s.Items).FirstOrDefault(s => s.StoreId == storeId && s.Id == sitemapId);
            }
        }

        public virtual ICollection<SitemapEntity> GetSitemaps(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                throw new ArgumentException("storeId");
            }

            using (var repository = RepositoryFactory())
            {
                return repository.Sitemaps.Where(s => s.StoreId == storeId).OrderByDescending(s => s.CreatedDate).ToList();
            }
        }

        public virtual void SaveChanges(SitemapEntity[] sitemaps)
        {
            if (sitemaps == null)
            {
                throw new ArgumentNullException("sitemaps");
            }

            using (var repository = RepositoryFactory())
            {
                var pkMap = new PrimaryKeyResolvingMap();
                var changeTracker = GetChangeTracker(repository);

                var sitemapIdsToSave = sitemaps.Where(s => !s.IsTransient()).Select(s => s.Id).ToArray();
                var existingSitemaps = repository.GetSitemapsByIds(sitemapIdsToSave);
                foreach (var sitemap in sitemaps)
                {
                    foreach (var sitemapItem in sitemap.Items)
                    {
                        sitemapItem.AbsoluteUrl = "http://localhost";
                    }

                    var existingSitemap = existingSitemaps.FirstOrDefault(s => s.Id == sitemap.Id);
                    if (existingSitemap != null)
                    {
                        sitemap.Patch(existingSitemap);
                        changeTracker.Attach(existingSitemap);
                    }
                    else
                    {
                        repository.Add(sitemap);
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
                    var sitemap = storeSitemaps.FirstOrDefault(s => s.Id == sitemapId);
                    if (sitemap != null)
                    {
                        repository.Remove(sitemap);
                    }
                }

                CommitChanges(repository);
            }
        }
    }
}