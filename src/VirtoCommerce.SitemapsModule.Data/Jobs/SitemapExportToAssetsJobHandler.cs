using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SitemapsModule.Data.BackgroundJobs;

namespace VirtoCommerce.SitemapsModule.Data.Jobs
{
    /// <summary>
    /// Writes a store's sitemap files to its asset folder on demand, off the request thread.
    /// </summary>
    public class SitemapExportToAssetsJobHandler(SitemapExportToAssetsJob job) : IBackgroundJobHandler<SitemapExportToAssetsJobPayload>
    {
        public virtual Task Execute(SitemapExportToAssetsJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return job.BackgroundExportToAssets(payload.StoreId, payload.BaseUrl, payload.SitemapIds, payload.Notification);
        }
    }
}
