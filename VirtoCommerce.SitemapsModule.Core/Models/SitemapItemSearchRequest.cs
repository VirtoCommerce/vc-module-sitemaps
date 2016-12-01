namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapItemSearchRequest
    {
        public SitemapItemSearchRequest()
        {
            Skip = 0;
            Take = 20;
        }

        public string SitemapId { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }
    }
}