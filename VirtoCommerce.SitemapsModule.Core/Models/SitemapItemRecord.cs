using System;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapItemRecord
    {
        public string Url { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string UpdateFrequency { get; set; }

        public decimal Priority { get; set; }
    }
}