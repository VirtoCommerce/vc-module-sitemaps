using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapItemRecord : ICloneable
    {
        public string Url { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string UpdateFrequency { get; set; }

        public decimal Priority { get; set; }

        public string ObjectType { get; set; }

        public ICollection<SitemapItemAlternateLinkRecord> Alternates { get; set; } = new List<SitemapItemAlternateLinkRecord>();

        public ICollection<SitemapItemImageRecord> Images { get; set; } = new List<SitemapItemImageRecord>();

        #region ICloneable members

        public virtual object Clone()
        {
            var result = MemberwiseClone() as SitemapItemRecord;

            result.Alternates = Alternates?.Select(x => x.Clone()).OfType<SitemapItemAlternateLinkRecord>().ToList();
            result.Images = Images?.Select(x => x.Clone()).OfType<SitemapItemImageRecord>().ToList();

            return result;
        }

        #endregion
    }
}
