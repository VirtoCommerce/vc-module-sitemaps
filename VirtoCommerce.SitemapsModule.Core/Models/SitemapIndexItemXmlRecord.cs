using System;
using System.Xml.Serialization;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    [Serializable]
    public class SitemapIndexItemXmlRecord
    {
        [XmlElement("loc")]
        public string Url { get; set; }

        [XmlElement("lastmod")]
        public DateTime? ModifiedDate { get; set; }
    }
}