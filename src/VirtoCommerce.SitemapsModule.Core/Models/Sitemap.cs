using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class Sitemap : AuditableEntity, ICloneable
    {
        public string Location { get; set; }

        public string StoreId { get; set; }

        public ICollection<SitemapItem> Items { get; set; } = [];

        public string UrlTemplate { get; set; }

        public int TotalItemsCount { get; set; }

        public SitemapContentMode SitemapMode { get; set; }

        public ICollection<string> PagedLocations { get; } = [];

        #region ICloneable members

        public virtual object Clone()
        {
            var result = (Sitemap)MemberwiseClone();

            result.Items = Items?.Select(x => x.CloneTyped()).ToList();

            return result;
        }

        #endregion
    }
}
