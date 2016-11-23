using VirtoCommerce.SitemapsModule.Core.Model;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapItemService
    {
        SearchResponse<SitemapItem> Search(SitemapItemSearchRequest request);

        void Add(string sitemapId, SitemapItem[] sitemapItems);

        void Remove(string sitemapId, string[] sitemapItemIds);
    }
}