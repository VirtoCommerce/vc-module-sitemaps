using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace VirtoCommerce.SitemapsModule.Data.Models.Xml
{
    [Serializable]
    [XmlRoot("sitemapindex", Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9")]
    public class SitemapIndexRecord
    {
        public SitemapIndexRecord()
        {
            Sitemaps = new List<SitemapIndexItemRecord>();
        }

        [XmlElement("sitemap")]
        public List<SitemapIndexItemRecord> Sitemaps { get; set; }
    }
}