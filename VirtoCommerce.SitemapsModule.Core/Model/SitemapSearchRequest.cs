namespace VirtoCommerce.SitemapsModule.Core.Model
{
    public class SitemapSearchRequest
    {
        public string StoreId { get; set; }

        public string[] SitemapIds { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }
    }
}