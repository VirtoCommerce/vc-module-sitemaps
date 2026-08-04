using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SitemapsModule.Data.BackgroundJobs;

namespace VirtoCommerce.SitemapsModule.Data.Jobs
{
    /// <summary>
    /// Packs a store's sitemap files into a downloadable archive, off the request thread.
    /// </summary>
    public class SitemapDownloadJobHandler(SitemapExportToAssetsJob job) : IBackgroundJobHandler<SitemapDownloadJobPayload>
    {
        public virtual Task Execute(SitemapDownloadJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return job.BackgroundDownload(payload.StoreId, payload.BaseUrl, payload.LocalTmpFolder, payload.SitemapIds, payload.Notification);
        }
    }
}
