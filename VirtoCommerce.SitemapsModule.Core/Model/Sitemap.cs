using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SitemapsModule.Core.Model
{
    public class Sitemap : AuditableEntity
    {
        public string StoreId { get; set; }

        public string Filename { get; set; }

        public ICollection<SitemapItem> Items { get; set; }
    }
}