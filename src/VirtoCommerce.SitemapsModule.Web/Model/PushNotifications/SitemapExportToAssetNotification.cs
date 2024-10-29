using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.SitemapsModule.Web.Model.PushNotifications
{
    /// <summary>
    /// 
    /// </summary>
    public class SitemapExportToAssetNotification : PushNotification
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="creator"></param>
        public SitemapExportToAssetNotification(string creator)
            : base(creator)
        {
            NotifyType = "SitemapExportToAsset";
            Errors = [];
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("finished")]
        public DateTime? Finished { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("totalCount")]
        public long TotalCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("processedCount")]
        public long ProcessedCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("errorCount")]
        public long ErrorCount => Errors?.Count ?? 0;

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("errors")]
        public ICollection<string> Errors { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("sitemapXmlUrl")]
        public string SitemapXmlUrl { get; set; }
    }
}
