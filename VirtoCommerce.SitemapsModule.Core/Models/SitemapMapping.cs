using System.Collections.Generic;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapMapping
    {
        public SitemapMapping()
        {
            Items = new List<SitemapItemMapping>();
        }

        public string SitemapId { get; set; }

        public string Filename { get; set; }

        public string Url { get; set; }

        public IEnumerable<SitemapItemMapping> Items { get; set; }
    }
}