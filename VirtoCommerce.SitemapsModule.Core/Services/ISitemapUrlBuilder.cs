using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Model;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapUrlBuilder
    {
        string ToAbsoluteUrl(Store store, SeoInfo seoInfo);

        string ToAbsoluteUrl(Store store, string relativeUrl);
    }
}