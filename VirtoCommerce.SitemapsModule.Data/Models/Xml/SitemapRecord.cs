using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace VirtoCommerce.SitemapsModule.Data.Models.Xml
{
    [Serializable]
    [XmlRoot("urlset", Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9")]
    public class SitemapRecord
    {
        public SitemapRecord()
        {
            Items = new List<SitemapItemRecord>();
        }

        [XmlElement("url")]
        public List<SitemapItemRecord> Items { get; set; }
    }
}