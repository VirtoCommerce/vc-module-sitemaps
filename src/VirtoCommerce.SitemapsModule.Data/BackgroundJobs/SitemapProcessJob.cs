using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core;
using VirtoCommerce.SitemapsModule.Data.Model.PushNotifications;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model.Search;
using VirtoCommerce.StoreModule.Core.Services;
using SystemFile = System.IO.File;

namespace VirtoCommerce.SitemapsModule.Data.BackgroundJobs;

public class SitemapExportToAssetsJob
{
    private readonly ISitemapXmlGenerator _sitemapXmlGenerator;
    private readonly IStoreSearchService _storeSearchService;
    private readonly IBlobStorageProvider _blobStorageProvider;
    private readonly ILogger<SitemapExportToAssetsJob> _logger;
    private readonly IPushNotificationManager _notifier;
    private readonly IBlobUrlResolver _blobUrlResolver;

    public SitemapExportToAssetsJob(ISitemapXmlGenerator sitemapXmlGenerator,
        IStoreSearchService storeSearchService,
        IBlobStorageProvider blobStorageProvider,
        ILogger<SitemapExportToAssetsJob> logger,
        IPushNotificationManager notifier,
        IBlobUrlResolver blobUrlResolver)
    {
        _sitemapXmlGenerator = sitemapXmlGenerator;
        _storeSearchService = storeSearchService;
        _blobStorageProvider = blobStorageProvider;
        _logger = logger;
        _notifier = notifier;
        _blobUrlResolver = blobUrlResolver;
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

    public Task BackgroundDownload(string storeId, string baseUrl, string localTmpFolder, SitemapDownloadNotification notification)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(storeId);

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Incorrect base URL {baseUrl}");
        }

        return InnerBackgroundDownload(storeId, baseUrl, localTmpFolder, notification);
    }

    public Task BackgroundExportToAssets(string storeId, string baseUrl, SitemapExportToAssetNotification notification)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(storeId);

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Incorrect base URL {baseUrl}");
        }

        return InnerBackgroundExportToAssets(storeId, baseUrl, notification);
    }

    private async Task InnerBackgroundExportToAssets(string storeId, string baseUrl, SitemapExportToAssetNotification notification)
    {
        var outputAssetFolder = string.Format(Core.ModuleConstants.StoreAssetsOutputFolderTemplate, storeId);

        try
        {
            var sitemapXmlBlobInfo = await ExportSitemapPartAsync(storeId, baseUrl, outputAssetFolder, ModuleConstants.SitemapFileName, progress => SendProgressNotification(notification, progress));

            foreach (var sitemapUrl in await _sitemapXmlGenerator.GetSitemapUrlsAsync(storeId, baseUrl))
            {
                await ExportSitemapPartAsync(storeId, baseUrl, outputAssetFolder, sitemapUrl, progress => SendProgressNotification(notification, progress));
            }

            notification.Description = "Sitemap export to store assets finished";
            notification.SitemapXmlUrl = sitemapXmlBlobInfo.Url;
        }
        catch (Exception exception)
        {
            notification.Description = "Sitemap export to store assets failed";
            notification.Errors.Add(exception.ExpandExceptionMessage());
        }
        finally
        {
            notification.Finished = DateTime.UtcNow;
            await _notifier.SendAsync(notification);
        }
    }

    private async Task InnerBackgroundDownload(string storeId, string baseUrl, string localTmpFolder, SitemapDownloadNotification notification)
    {
        var uniqueFileName = $"sitemap-{DateTime.UtcNow:yyyy-MM-dd}-{Guid.NewGuid()}.zip";

        try
        {
            var relativeUrl = $"tmp/{uniqueFileName}";
            var localTmpPath = Path.Combine(localTmpFolder, uniqueFileName);

            // Create directory if not exist
            if (!Directory.Exists(localTmpFolder))
            {
                Directory.CreateDirectory(localTmpFolder);
            }

            // Remove old file if exist
            if (SystemFile.Exists(localTmpPath))
            {
                SystemFile.Delete(localTmpPath);
            }

            //Import first to local tmp folder because Azure blob storage doesn't support some special file access mode
            using (var stream = SystemFile.Open(localTmpPath, FileMode.CreateNew))
            using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                // Create default sitemap.xml
                await CreateSitemapPartAsync(zipArchive, storeId, baseUrl, ModuleConstants.SitemapFileName, progress => SendProgressNotification(notification, progress));

                var sitemapUrls = await _sitemapXmlGenerator.GetSitemapUrlsAsync(storeId, baseUrl);
                foreach (var sitemapUrl in sitemapUrls.Where(url => !string.IsNullOrEmpty(url)))
                {
                    await CreateSitemapPartAsync(zipArchive, storeId, baseUrl, sitemapUrl, progress => SendProgressNotification(notification, progress));
                }
            }

            //Copy export data to blob provider for get public download url
            using (var localStream = SystemFile.Open(localTmpPath, FileMode.Open, FileAccess.Read))
            using (var blobStream = _blobStorageProvider.OpenWrite(relativeUrl))
            {
                await localStream.CopyToAsync(blobStream);
            }

            // Add unique key for every link to prevent browser caching
            notification.DownloadUrl = $"{_blobUrlResolver.GetAbsoluteUrl(relativeUrl)}?v={DateTime.UtcNow.Ticks}";
            notification.Description = "Sitemap download finished";
        }
        catch (Exception exception)
        {
            notification.Description = "Sitemap download failed";
            notification.Errors.Add(exception.ExpandExceptionMessage());
        }
        finally
        {
            notification.Finished = DateTime.UtcNow;
            await _notifier.SendAsync(notification);
        }
    }

    private async Task ProcessStore(Store store)
    {
        var outputAssetFolder = string.Format(Core.ModuleConstants.StoreAssetsOutputFolderTemplate, store.Id);

        _logger.LogInformation("Starting export {sitemapUrl} for store {storeId} to {outputAssetFolder}.", Core.ModuleConstants.SitemapFileName, store.Id, outputAssetFolder); // Log success

        await ExportSitemapPartAsync(store.Id, store.Url, outputAssetFolder, Core.ModuleConstants.SitemapFileName, null);

        foreach (var sitemapUrl in await _sitemapXmlGenerator.GetSitemapUrlsAsync(store.Id, store.Url))
        {
            _logger.LogInformation("Starting export {sitemapUrl} for store {storeId} to {outputAssetFolder}.", sitemapUrl, store.Id, outputAssetFolder); // Log success

            await ExportSitemapPartAsync(store.Id, store.Url, outputAssetFolder, sitemapUrl, null);
        }
    }

    private async Task CreateSitemapPartAsync(ZipArchive zipArchive, string storeId, string baseUrl, string sitemapUrl, Action<ExportImportProgressInfo> progressCallback)
    {
        var sitemapPart = zipArchive.CreateEntry(sitemapUrl, CompressionLevel.Optimal);

        using var sitemapPartStream = sitemapPart.Open();
        using var stream = await _sitemapXmlGenerator.GenerateSitemapXmlAsync(storeId, baseUrl, sitemapUrl, progressCallback);
        await stream.CopyToAsync(sitemapPartStream);
    }

    private async Task<BlobInfo> ExportSitemapPartAsync(string storeId, string storeUrl, string outputAssetFolder, string sitemapUrl, Action<ExportImportProgressInfo> progressCallback)
    {
        var relativeUrl = $"{outputAssetFolder.Trim('/')}/{sitemapUrl.Trim('/')}";

        using var stream = await _sitemapXmlGenerator.GenerateSitemapXmlAsync(storeId, storeUrl, sitemapUrl, progressCallback);
        using var blobStream = _blobStorageProvider.OpenWrite(relativeUrl);
        await stream.CopyToAsync(blobStream);

        return await _blobStorageProvider.GetBlobInfoAsync(relativeUrl);
    }

    private void SendProgressNotification(SitemapNotification notification, ExportImportProgressInfo progressInfo)
    {
        notification.Description = progressInfo.Description;
        notification.ProcessedCount = progressInfo.ProcessedCount;
        notification.TotalCount = progressInfo.TotalCount;
        notification.Errors = progressInfo.Errors?.ToList() ?? [];

        _notifier.Send(notification);
    }
}
