using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapItemRecordProvider
    {
        void LoadSitemapItemRecords(Store store, Sitemap sitemap, string baseUrl);
    }
}