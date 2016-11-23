namespace VirtoCommerce.SitemapsModule.Core.Model
{
    public class SitemapItemSearchRequest
    {
        public string SitemapId { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }
    }
}