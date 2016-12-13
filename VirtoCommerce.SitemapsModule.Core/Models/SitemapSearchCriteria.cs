using VirtoCommerce.Domain.Commerce.Model.Search;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapSearchCriteria : SearchCriteriaBase
    {
        public string StoreId { get; set; }

        public string Filename { get; set; }
    }
}