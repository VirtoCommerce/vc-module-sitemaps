using System;
using System.Xml.Serialization;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Data.Models.Xml
{
    [Serializable]
    [XmlType(Namespace = "http://www.google.com/schemas/sitemap-image/1.1")]
    public class SitemapItemImageXmlRecord
    {
        [XmlElement("loc")]
        public string Loc { get; set; }

        public virtual SitemapItemImageXmlRecord ToXmlModel(SitemapItemImageRecord coreModel)
        {
            if (coreModel == null)
            {
                throw new ArgumentNullException(nameof(coreModel));
            }

            Loc = coreModel.Loc;

            return this;
        }
    }
}
