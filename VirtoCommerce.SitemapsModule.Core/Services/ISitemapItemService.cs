using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapItemService
    {
        SearchResponse<SitemapItem> Search(SitemapItemSearchRequest request);

        void Add(string sitemapId, SitemapItem[] items);

        void Remove(string sitemapId, string[] itemIds);
    }
}