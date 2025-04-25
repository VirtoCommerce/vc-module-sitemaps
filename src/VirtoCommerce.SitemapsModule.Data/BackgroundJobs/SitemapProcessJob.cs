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
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Data.Model.PushNotifications;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model.Search;
using VirtoCommerce.StoreModule.Core.Services;
using SystemFile = System.IO.File;

namespace VirtoCommerce.SitemapsModule.Data.BackgroundJobs;

public class SitemapExportToAssetsJob
{
    private readonly ISitemapGenerator _sitemapGenerator;
    private readonly IStoreSearchService _storeSearchService;
    private readonly IBlobStorageProvider _blobStorageProvider;
    private readonly ILogger<SitemapExportToAssetsJob> _logger;
    private readonly IPushNotificationManager _notifier;
    private readonly IBlobUrlResolver _blobUrlResolver;

    public SitemapExportToAssetsJob(
        ISitemapGenerator sitemapGenerator,
        IStoreSearchService storeSearchService,
        IBlobStorageProvider blobStorageProvider,
        ILogger<SitemapExportToAssetsJob> logger,
        IPushNotificationManager notifier,
        IBlobUrlResolver blobUrlResolver)
    {
        _sitemapGenerator = sitemapGenerator;
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
            foreach (var store in searchResult.Results.Where(x => x.Settings.GetValue<bool>(ModuleConstants.Settings.General.EnableExportToAssetsJob)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await SaveSitemapToBlob(store.Id, store.Url, progressCallback: null);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(storeId);

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Incorrect base URL {baseUrl}");
        }

        return InnerBackgroundDownload(storeId, baseUrl, localTmpFolder, notification);
    }

    public Task BackgroundExportToAssets(string storeId, string baseUrl, SitemapExportToAssetNotification notification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeId);

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Incorrect base URL {baseUrl}");
        }

        return InnerBackgroundExportToAssets(storeId, baseUrl, notification);
    }

    private async Task InnerBackgroundExportToAssets(string storeId, string baseUrl, SitemapExportToAssetNotification notification)
    {
        try
        {
            var sitemapXmlBlobInfo = await SaveSitemapToBlob(storeId, baseUrl, progress => SendProgressNotification(notification, progress));

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

            //Save to local folder because Azure blob storage doesn't support some special file access modes
            await using (var stream = SystemFile.Open(localTmpPath, FileMode.CreateNew))
            using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                await SaveSitemapToZip(storeId, baseUrl, zipArchive, progress => SendProgressNotification(notification, progress));
            }

            //Copy data to blob provider to get public download URL
            await using (var localStream = SystemFile.Open(localTmpPath, FileMode.Open, FileAccess.Read))
            await using (var blobStream = await _blobStorageProvider.OpenWriteAsync(relativeUrl))
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

    private async Task<BlobInfo> SaveSitemapToBlob(string storeId, string baseUrl, Action<ExportImportProgressInfo> progressCallback)
    {
        BlobInfo firstBlobInfo = null;
        var outputAssetFolder = string.Format(ModuleConstants.StoreAssetsOutputFolderTemplate, storeId);

        var files = await _sitemapGenerator.GetSitemapFilesAsync(storeId, baseUrl, progressCallback);

        foreach (var file in files)
        {
            var blobInfo = await SaveFileToBlob(file, outputAssetFolder, progressCallback);
            firstBlobInfo ??= blobInfo;
        }

        return firstBlobInfo;
    }

    private async Task SaveSitemapToZip(string storeId, string baseUrl, ZipArchive zipArchive, Action<ExportImportProgressInfo> progressCallback)
    {
        var files = await _sitemapGenerator.GetSitemapFilesAsync(storeId, baseUrl, progressCallback);

        foreach (var file in files)
        {
            await SaveFileToZip(file, zipArchive, progressCallback);
        }
    }

    private async Task<BlobInfo> SaveFileToBlob(SitemapFile file, string outputAssetFolder, Action<ExportImportProgressInfo> progressCallback)
    {
        _logger.LogInformation("Saving file {fileName} to {outputAssetFolder}.", file.Name, outputAssetFolder);

        var relativeUrl = $"{outputAssetFolder.Trim('/')}/{file.Name.Trim('/')}";
        await using var targetStream = await _blobStorageProvider.OpenWriteAsync(relativeUrl);
        _sitemapGenerator.SaveSitemapFile(file, targetStream, progressCallback);

        return await _blobStorageProvider.GetBlobInfoAsync(relativeUrl);
    }

    private async Task SaveFileToZip(SitemapFile file, ZipArchive zipArchive, Action<ExportImportProgressInfo> progressCallback)
    {
        var entry = zipArchive.CreateEntry(file.Name, CompressionLevel.Optimal);
        await using var targetStream = entry.Open();
        _sitemapGenerator.SaveSitemapFile(file, targetStream, progressCallback);
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
