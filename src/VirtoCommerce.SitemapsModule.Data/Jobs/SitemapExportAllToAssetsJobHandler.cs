using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SitemapsModule.Data.BackgroundJobs;

namespace VirtoCommerce.SitemapsModule.Data.Jobs
{
    /// <summary>
    /// Exports sitemaps to store assets for every store that has the export enabled. Target of the recurring schedule.
    /// </summary>
    public class SitemapExportAllToAssetsJobHandler(SitemapExportToAssetsJob job) : IBackgroundJobHandler<SitemapExportAllToAssetsJobPayload>
    {
        public virtual Task Execute(SitemapExportAllToAssetsJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return job.ProcessAll(cancellationToken);
        }
    }
}
