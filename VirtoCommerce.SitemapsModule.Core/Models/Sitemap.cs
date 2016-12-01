using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class Sitemap : AuditableEntity
    {
        public Sitemap()
        {
            Items = new List<SitemapItem>();
        }

        public string Filename { get; set; }

        public string StoreId { get; set; }

        public int ItemsTotalCount { get; set; }

        public ICollection<SitemapItem> Items { get; set; }
    }
}