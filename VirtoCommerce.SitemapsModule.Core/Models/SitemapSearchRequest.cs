namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapSearchRequest
    {
        public SitemapSearchRequest()
        {
            Skip = 0;
            Take = 20;
        }

        public string StoreId { get; set; }

        public string Filename { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }
    }
}