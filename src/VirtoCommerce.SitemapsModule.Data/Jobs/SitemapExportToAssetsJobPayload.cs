using VirtoCommerce.SitemapsModule.Data.Model.PushNotifications;

namespace VirtoCommerce.SitemapsModule.Data.Jobs
{
    /// <summary>
    /// Payload of the background job that writes a store's sitemap files to its asset folder on demand.
    /// </summary>
    public class SitemapExportToAssetsJobPayload
    {
        public string StoreId { get; set; }

        public string BaseUrl { get; set; }

        public string[] SitemapIds { get; set; }

        /// <summary>
        /// The notification created by the request that started the job. Carried in the payload, as the Hangfire job
        /// this replaces did, because the job reports its progress and resulting asset URL against that same id.
        /// </summary>
        public SitemapExportToAssetNotification Notification { get; set; }
    }
}
