using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapItemRecord : SitemapRecord, ICloneable
    {
        public DateTime ModifiedDate { get; set; }

        public string UpdateFrequency { get; set; }

        public decimal Priority { get; set; }

        public string ObjectType { get; set; }

        public ICollection<SitemapItemAlternateLinkRecord> Alternates { get; set; } = new List<SitemapItemAlternateLinkRecord>();

        public ICollection<SitemapItemImageRecord> Images { get; set; } = new List<SitemapItemImageRecord>();

        #region ICloneable members

        public virtual object Clone()
        {
            var result = (SitemapItemRecord)MemberwiseClone();

            result.Alternates = Alternates?.Select(x => x.CloneTyped()).ToList();
            result.Images = Images?.Select(x => x.CloneTyped()).ToList();

            return result;
        }

        #endregion
    }
}
