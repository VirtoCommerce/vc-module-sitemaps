using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace VirtoCommerce.SitemapsModule.Data.Models.Xml
{
    [Serializable]
    [XmlRoot("urlset", Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9")]
    public class SitemapXmlRecord
    {
        public SitemapXmlRecord()
        {
            Items = new List<SitemapItemXmlRecord>();
        }

        /// <summary>
        /// Property that is used to dynamically set xml namespaces for objects like Image
        /// </summary>
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns;

        [XmlElement("url")]
        public List<SitemapItemXmlRecord> Items { get; set; }
    }
}
