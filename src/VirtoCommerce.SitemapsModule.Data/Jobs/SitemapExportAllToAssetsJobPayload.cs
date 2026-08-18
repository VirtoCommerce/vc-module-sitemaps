namespace VirtoCommerce.SitemapsModule.Data.Jobs
{
    /// <summary>
    /// Payload of the recurring job that exports sitemaps for every store that has the export enabled. Carries no
    /// data: the schedule fires with no arguments and the job discovers the stores itself. It exists because the job
    /// API is payload-addressed, and it stays a class so a partner module can extend it via AbstractTypeFactory.
    /// </summary>
    public class SitemapExportAllToAssetsJobPayload
    {
    }
}
