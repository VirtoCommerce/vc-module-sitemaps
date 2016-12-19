using System;
using System.Xml.Serialization;

namespace VirtoCommerce.SitemapsModule.Data.Models.Xml
{
    [Serializable]
    public class SitemapItemRecord
    {
        [XmlElement("loc")]
        public string Url { get; set; }

        [XmlElement("lastmod")]
        public DateTime ModifiedDate { get; set; }

        [XmlElement("changefreq")]
        public string UpdateFrequency { get; set; }

        [XmlElement("priority")]
        public decimal Priority { get; set; }
    }
}