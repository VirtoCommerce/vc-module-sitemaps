namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapOptions
    {
        public int RecordsLimitPerFile { get; set; }

        public string FilenameSeparator { get; set; }

        public int CatalogSearchBunchSize { get; set; }

        public SitemapItemOptions CategoryOptions { get; set; }

        public SitemapItemOptions ProductOptions { get; set; }
    }
}