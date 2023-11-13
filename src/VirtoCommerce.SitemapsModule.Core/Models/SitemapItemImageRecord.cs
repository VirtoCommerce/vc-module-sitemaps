using System;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapItemImageRecord : ICloneable
    {
        public string Loc { get; set; }

        #region ICloneable members

        public virtual object Clone()
        {
            return MemberwiseClone() as SitemapItemImageRecord;
        }

        #endregion
    }
}
