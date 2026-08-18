using VirtoCommerce.SitemapsModule.Data.Model.PushNotifications;

namespace VirtoCommerce.SitemapsModule.Data.Jobs
{
    /// <summary>
    /// Payload of the background job that packs a store's sitemap files into a downloadable archive.
    /// </summary>
    public class SitemapDownloadJobPayload
    {
        public string StoreId { get; set; }

        public string BaseUrl { get; set; }

        public string LocalTmpFolder { get; set; }

        public string[] SitemapIds { get; set; }

        /// <summary>
        /// The notification created by the request that started the job. Carried in the payload, as the Hangfire job
        /// this replaces did, because the job reports its progress and final download URL against that same id.
        /// </summary>
        public SitemapDownloadNotification Notification { get; set; }
    }
}
