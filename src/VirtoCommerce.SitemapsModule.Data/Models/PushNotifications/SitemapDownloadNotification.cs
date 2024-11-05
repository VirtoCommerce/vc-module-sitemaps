using Newtonsoft.Json;

namespace VirtoCommerce.SitemapsModule.Data.Model.PushNotifications
{
    /// <summary>
    /// Represents a notification about sitemap download.
    /// </summary>
    public class SitemapDownloadNotification : SitemapNotification
    {
        public SitemapDownloadNotification(string creator)
            : base(creator)
        {
            NotifyType = "SitemapDownload";
        }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }
    }
}
