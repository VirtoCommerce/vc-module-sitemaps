using System.Collections.Generic;
using VirtoCommerce.SitemapsModule.Data.Model;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public interface ISitemapService
    {
        SitemapEntity GetSitemapById(string storeId, string sitemapId);

        ICollection<SitemapEntity> GetSitemaps(string storeId);

        void SaveChanges(SitemapEntity[] sitemaps);

        void DeleteSitemaps(string storeId, string[] sitemapIds);
    }
}