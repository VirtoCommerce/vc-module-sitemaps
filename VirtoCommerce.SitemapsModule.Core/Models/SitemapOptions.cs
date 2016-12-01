namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapOptions
    {
        public int RecordsLimitPerFile { get; set; }

        public string CategoryPageUpdateFrequency { get; set; }

        public decimal CategoryPagePriority { get; set; }

        public string ProductPageUpdateFrequency { get; set; }

        public decimal ProductPagePriority { get; set; }
    }
}