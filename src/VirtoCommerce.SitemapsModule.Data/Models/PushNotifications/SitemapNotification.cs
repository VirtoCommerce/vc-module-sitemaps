using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.SitemapsModule.Data.Model.PushNotifications
{
    /// <summary>
    /// Represents a base class for sitemap notifications.
    /// </summary>
    public class SitemapNotification : PushNotification
    {
        public SitemapNotification(string creator)
            : base(creator)
        {
            NotifyType = "SitemapNotification";
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
    }
}
