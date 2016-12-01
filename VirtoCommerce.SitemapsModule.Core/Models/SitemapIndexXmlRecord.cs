using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    [Serializable]
    [XmlRoot("sitemapindex", Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9")]
    public class SitemapIndexXmlRecord
    {
        [XmlElement("sitemap")]
        public List<SitemapIndexItemXmlRecord> Sitemaps { get; set; }
    }
}