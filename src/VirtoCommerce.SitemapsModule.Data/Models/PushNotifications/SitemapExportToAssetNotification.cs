using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.SitemapsModule.Data.Model.PushNotifications
{
    /// <summary>
    /// Represents a notification about the sitemap export to asset.
    /// </summary>
    public class SitemapExportToAssetNotification : PushNotification
    {
        public SitemapExportToAssetNotification(string creator)
            : base(creator)
        {
            NotifyType = "SitemapExportToAsset";
            Errors = [];
        }

        [JsonProperty("finished")]
        public DateTime? Finished { get; set; }

        [JsonProperty("totalCount")]
        public long TotalCount { get; set; }

        [JsonProperty("processedCount")]
        public long ProcessedCount { get; set; }

        [JsonProperty("errorCount")]
        public long ErrorCount => Errors?.Count ?? 0;

        [JsonProperty("errors")]
        public ICollection<string> Errors { get; set; }

        [JsonProperty("sitemapXmlUrl")]
        public string SitemapXmlUrl { get; set; }
    }
}
