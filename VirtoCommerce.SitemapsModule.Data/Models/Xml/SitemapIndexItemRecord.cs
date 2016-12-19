using System;
using System.Xml.Serialization;

namespace VirtoCommerce.SitemapsModule.Data.Models.Xml
{
    [Serializable]
    public class SitemapIndexItemRecord
    {
        [XmlElement("loc")]
        public string Url { get; set; }

        [XmlElement("lastmod")]
        public DateTime? ModifiedDate { get; set; }

        [XmlIgnore]
        public string SitemapId { get; set; }

        [XmlIgnore]
        public string Filename { get; set; }
    }
}