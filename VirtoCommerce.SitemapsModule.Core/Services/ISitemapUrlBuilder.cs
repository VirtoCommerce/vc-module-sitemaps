using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapUrlBuilder
    {
        string CreateAbsoluteUrl(Store store, string urlTemplate, string language = null, string semanticUrl = null);
    }
}