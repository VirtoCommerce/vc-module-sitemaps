using Newtonsoft.Json;

namespace VirtoCommerce.SitemapsModule.Data.Model.PushNotifications
{
    /// <summary>
    /// Represents a notification about the sitemap export to asset.
    /// </summary>
    public class SitemapExportToAssetNotification : SitemapNotification
    {
        public SitemapExportToAssetNotification(string creator)
            : base(creator)
        {
            NotifyType = "SitemapExportToAsset";
        }

        [JsonProperty("sitemapXmlUrl")]
        public string SitemapXmlUrl { get; set; }
    }
}
