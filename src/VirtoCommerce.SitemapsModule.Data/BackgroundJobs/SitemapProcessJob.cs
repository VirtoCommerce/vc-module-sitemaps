using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Data.Extensions;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model.Search;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.BackgroundJobs;

public class SitemapExportToAssetsJob
{
    private readonly ISitemapXmlGenerator _sitemapXmlGenerator;
    private readonly IStoreSearchService _storeSearchService;
    private readonly IBlobStorageProvider _blobStorageProvider;
    private readonly ILogger<SitemapExportToAssetsJob> _logger;

    public SitemapExportToAssetsJob(ISitemapXmlGenerator sitemapXmlGenerator,
        IStoreSearchService storeSearchService,
        IBlobStorageProvider blobStorageProvider,
        ILogger<SitemapExportToAssetsJob> logger)
    {
        _sitemapXmlGenerator = sitemapXmlGenerator;
        _storeSearchService = storeSearchService;
        _blobStorageProvider = blobStorageProvider;
        _logger = logger;
    }

    public async Task ProcessAll(IJobCancellationToken cancellationToken)
    {
        var searchCriteria = AbstractTypeFactory<StoreSearchCriteria>.TryCreateInstance();

        await foreach (var searchResult in _storeSearchService.SearchBatchesAsync(searchCriteria))
        {
            foreach (var store in searchResult.Results.Where(x => x.Settings.GetValue<bool>(Core.ModuleConstants.Settings.General.EnableExportToAssetsJob)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ProcessStore(store);
                    _logger.LogInformation("Sitemap for store {storeId} exported successfully.", store.Id); // Log success
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error exporting sitemap for store {storeId}.", store.Id); // Log error
                }
            }
        }
    }

    protected virtual async Task ProcessStore(Store store)
    {
        var outputAssetFolder = string.Format(Core.ModuleConstants.StoreAssetsOutputFolderTemplate, store.Id);

        _logger.LogInformation("Starting export {sitemapUrl} for store {storeId} to {outputAssetFolder}.", Core.ModuleConstants.SitemapFileName, store.Id, outputAssetFolder); // Log success

        await ExportSitemapPartAsync(store, outputAssetFolder, Core.ModuleConstants.SitemapFileName);

        foreach (var sitemapUrl in await _sitemapXmlGenerator.GetSitemapUrlsAsync(store.Id, store.Url))
        {
            _logger.LogInformation("Starting export {sitemapUrl} for store {storeId} to {outputAssetFolder}.", sitemapUrl, store.Id, outputAssetFolder); // Log success

            await ExportSitemapPartAsync(store, outputAssetFolder, sitemapUrl);
        }
    }


    protected virtual async Task ExportSitemapPartAsync(Store store, string outputAssetFolder, string sitemapUrl)
    {
        var relativeUrl = RelativePathUtils.Combine(outputAssetFolder, sitemapUrl);

        using var stream = await _sitemapXmlGenerator.GenerateSitemapXmlAsync(store.Id, store.Url, sitemapUrl, null);
        using var blobStream = _blobStorageProvider.OpenWrite(relativeUrl);
        await stream.CopyToAsync(blobStream);
    }
}
