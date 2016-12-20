using System;
using System.Xml.Serialization;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Data.Models.Xml
{
    [Serializable]
    public class SitemapItemXmlRecord
    {
        [XmlElement("loc")]
        public string Url { get; set; }

        [XmlElement("lastmod")]
        public DateTime ModifiedDate { get; set; }

        [XmlElement("changefreq")]
        public string UpdateFrequency { get; set; }

        [XmlElement("priority")]
        public decimal Priority { get; set; }

        public virtual SitemapItemXmlRecord ToXmlModel(SitemapItemRecord coreModel)
        {
            if (coreModel == null)
            {
                throw new ArgumentNullException("coreModel");
            }

            ModifiedDate = coreModel.ModifiedDate;
            Priority = coreModel.Priority;
            UpdateFrequency = coreModel.UpdateFrequency;
            Url = coreModel.Url;

            return this;
        }
    }
}