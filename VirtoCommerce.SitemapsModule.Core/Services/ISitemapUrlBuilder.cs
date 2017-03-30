using VirtoCommerce.Domain.Store.Model;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapUrlBuilder
    {
        string CreateAbsoluteUrl(Store store, string urlTemplate, string baseUrl, string language = null, string semanticUrl = null);
    }
}