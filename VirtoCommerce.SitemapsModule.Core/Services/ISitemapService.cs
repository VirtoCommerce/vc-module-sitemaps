using VirtoCommerce.SitemapsModule.Core.Model;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapService
    {
        SearchResponse<Sitemap> Search(SitemapSearchRequest request);

        void SaveChanges(Sitemap[] sitemaps);

        void DeleteSitemaps(string storeId, string[] sitemapIds);
    }
}