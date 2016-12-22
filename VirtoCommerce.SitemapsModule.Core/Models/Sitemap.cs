using System.Collections.Generic;
using VirtoCommerce.Domain.Store.Model;
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

        public Store Store { get; set; }

        public ICollection<SitemapItem> Items { get; set; }

        public string UrlTemplate { get; set; }

        public string BaseUrl { get; set; }

        public int TotalItemsCount { get; set; }
    }
}