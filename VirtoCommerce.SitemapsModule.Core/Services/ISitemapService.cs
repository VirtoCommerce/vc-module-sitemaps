using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapService
    {
        Sitemap GetById(string id);

        SearchResponse<Sitemap> Search(SitemapSearchRequest searchRequest);

        void SaveChanges(Sitemap sitemap);

        void Remove(string[] sitemapIds);
    }
}