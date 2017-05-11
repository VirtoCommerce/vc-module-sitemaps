using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapUrlBuilder
    {
        string BuildStoreUrl(Store store, string language, string urlTemplate, string baseUrl, IEntity entity = null);
    }
}