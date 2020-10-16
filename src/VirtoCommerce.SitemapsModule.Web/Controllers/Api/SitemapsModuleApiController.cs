using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.SitemapsModule.Core;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Models.Search;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.SitemapsModule.Web.Extensions;
using VirtoCommerce.SitemapsModule.Web.Model.PushNotifications;
using SystemFile = System.IO.File;

namespace VirtoCommerce.SitemapsModule.Web.Controllers.Api
{
    /// <summary>
    ///
    /// </summary>
    [Route("api/sitemaps")]
    [Authorize(ModuleConstants.Security.Permissions.Read)]
    public class SitemapsModuleApiController : Controller
    {
        private readonly ISitemapService _sitemapService;
        private readonly ISitemapItemService _sitemapItemService;
        private readonly ISitemapSearchService _sitemapSearchService;
        private readonly ISitemapItemSearchService _sitemapItemSearchService;
        private readonly ISitemapXmlGenerator _sitemapXmlGenerator;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IPushNotificationManager _notifier;
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly IBlobUrlResolver _blobUrlResolver;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly PlatformOptions _platformOptions;

        /// <summary>
        ///
        /// </summary>
        /// <param name="sitemapService"></param>
        /// <param name="sitemapItemService"></param>
        /// <param name="sitemapSearchService"></param>
        /// <param name="sitemapItemSearchService"></param>
        /// <param name="sitemapXmlGenerator"></param>
        /// <param name="userNameResolver"></param>
        /// <param name="notifier"></param>
        /// <param name="blobStorageProvider"></param>
        /// <param name="blobUrlResolver"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="platformOptions"></param>
        public SitemapsModuleApiController(
            ISitemapService sitemapService,
            ISitemapItemService sitemapItemService,
            ISitemapSearchService sitemapSearchService,
            ISitemapItemSearchService sitemapItemSearchService,
            ISitemapXmlGenerator sitemapXmlGenerator,
            IUserNameResolver userNameResolver,
            IPushNotificationManager notifier,
            IBlobStorageProvider blobStorageProvider,
            IBlobUrlResolver blobUrlResolver,
            IWebHostEnvironment hostingEnvironment, 
            IOptions<PlatformOptions> platformOptions)
        {
            _sitemapService = sitemapService;
            _sitemapItemService = sitemapItemService;
            _sitemapSearchService = sitemapSearchService;
            _sitemapItemSearchService = sitemapItemSearchService;
            _sitemapXmlGenerator = sitemapXmlGenerator;
            _userNameResolver = userNameResolver;
            _notifier = notifier;
            _blobStorageProvider = blobStorageProvider;
            _blobUrlResolver = blobUrlResolver;
            _hostingEnvironment = hostingEnvironment;
            _platformOptions = platformOptions.Value;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("search")]
        public async Task<ActionResult<SitemapSearchResult>> SearchSitemaps([FromBody] SitemapSearchCriteria request)
        {
            if (request == null)
            {
                return BadRequest("request is null");
            }

            var sitemapSearchResponse = await _sitemapSearchService.SearchAsync(request);

            return Ok(sitemapSearchResponse);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<Sitemap>> GetSitemapById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("id is null");
            }

            var sitemap = await _sitemapService.GetByIdAsync(id);

            if (sitemap == null)
            {
                return NotFound();
            }

            return Ok(sitemap);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sitemap"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Create)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> AddSitemap([FromBody] Sitemap sitemap)
        {
            if (sitemap == null)
            {
                return BadRequest("sitemap is null");
            }

            await _sitemapService.SaveChangesAsync(new[] { sitemap });

            return NoContent();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sitemap"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> UpdateSitemap([FromBody] Sitemap sitemap)
        {
            if (sitemap == null)
            {
                return BadRequest("sitemap is null");
            }

            await _sitemapService.SaveChangesAsync(new[] { sitemap });

            return NoContent();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Delete)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteSitemap(string[] ids)
        {
            if (ids == null)
            {
                return BadRequest("ids is null");
            }

            await _sitemapService.RemoveAsync(ids);

            return NoContent();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("items/search")]
        public async Task<ActionResult<SitemapItemsSearchResult>> SearchSitemapItems([FromBody] SitemapItemSearchCriteria request)
        {
            if (request == null)
            {
                return BadRequest("request is null");
            }

            var result = await _sitemapItemSearchService.SearchAsync(request);

            return Ok(result);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sitemapId"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("{sitemapId}/items")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> AddSitemapItems(string sitemapId, [FromBody] SitemapItem[] items)
        {
            if (string.IsNullOrEmpty(sitemapId))
            {
                return BadRequest("sitemapId is null");
            }
            if (items == null)
            {
                return BadRequest("items is null");
            }

            foreach (var item in items)
            {
                item.SitemapId = sitemapId;
            }
            await _sitemapItemService.SaveChangesAsync(items);

            return NoContent();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="itemIds"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("items")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> RemoveSitemapItems(string[] itemIds)
        {
            if (itemIds == null)
            {
                return BadRequest("itemIds is null");
            }

            await _sitemapItemService.RemoveAsync(itemIds);

            return NoContent();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="storeId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("schema")]
        public async Task<ActionResult<string[]>> GetSitemapsSchema(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                return BadRequest("storeId is empty");
            }

            var sitemapUrls = await _sitemapXmlGenerator.GetSitemapUrlsAsync(storeId);
            return Ok(sitemapUrls);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="storeId"></param>
        /// <param name="baseUrl"></param>
        /// <param name="sitemapUrl"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("generate")]
        [SwaggerFileResponse]
        public async Task<ActionResult> GenerateSitemap(string storeId, string baseUrl, string sitemapUrl)
        {
            var stream = await _sitemapXmlGenerator.GenerateSitemapXmlAsync(storeId, baseUrl, sitemapUrl);
            return new FileStreamResult(stream, "text/xml");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="storeId"></param>
        /// <param name="baseUrl"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("download")]
        public async Task<ActionResult<SitemapDownloadNotification>> DownloadSitemap(string storeId, string baseUrl)
        {
            var notification = new SitemapDownloadNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Download sitemaps",
                Description = "Processing download sitemaps..."
            };

            await _notifier.SendAsync(notification);

            BackgroundJob.Enqueue(() => BackgroundDownload(storeId, baseUrl, notification));

            return Ok(notification);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="storeId"></param>
        /// <param name="baseUrl"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        public Task BackgroundDownload(string storeId, string baseUrl, SitemapDownloadNotification notification)
        {
            // We cannot use storeId.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 to validate path because default
            // sanitizer for Sonar Cube do not trust it, so we use Regex here with same logic. Check this out
            // https://community.sonarsource.com/t/help-sonarcloud-with-understanding-the-usage-of-untrusted-and-tainted-input/9873/7
            // Btw, we cannot move this to extansion or any method from here because sonar ignore any outer checks :(
            if (!Regex.IsMatch(storeId, "^[a-zA-Z0-9-]+$"))
            {
                throw new ArgumentException($"Incorrect name of store {storeId}");
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var correctUri))
            {
                throw new ArgumentException($"Incorrect base URL {baseUrl}");
            }

            return InnerBackgroundDownload(storeId, baseUrl, notification);
        }

        private async Task InnerBackgroundDownload(string storeId, string baseUrl, SitemapDownloadNotification notification)
        {
            void SendNotificationWithProgressInfo(ExportImportProgressInfo c)
            {
                notification.Description = c.Description;
                notification.ProcessedCount = c.ProcessedCount;
                notification.TotalCount = c.TotalCount;
                notification.Errors = c.Errors?.ToList() ?? new List<string>();

                _notifier.Send(notification);
            }

            try
            {
                var relativeUrl = $"tmp/sitemap-{storeId}.zip";
                var localTmpFolder = _hostingEnvironment.MapPath(Path.Combine("~/", _platformOptions.LocalUploadFolderPath, "tmp"));
                var localTmpPath = Path.Combine(localTmpFolder, $"sitemap-{storeId}.zip");

                // Create directory if not exist
                if (!Directory.Exists(localTmpFolder))
                    Directory.CreateDirectory(localTmpFolder);

                // Remove old file if exist
                if (SystemFile.Exists(localTmpPath))
                    SystemFile.Delete(localTmpPath);

                //Import first to local tmp folder because Azure blob storage doesn't support some special file access mode
                using (var stream = SystemFile.Open(localTmpPath, FileMode.CreateNew))
                using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    // Create default sitemap.xml
                    await CreateSitemapPartAsync(zipArchive, storeId, baseUrl, "sitemap.xml", SendNotificationWithProgressInfo);

                    var sitemapUrls = await _sitemapXmlGenerator.GetSitemapUrlsAsync(storeId);
                    foreach (var sitemapUrl in sitemapUrls.Where(url => !string.IsNullOrEmpty(url)))
                    {
                        await CreateSitemapPartAsync(zipArchive, storeId, baseUrl, sitemapUrl, SendNotificationWithProgressInfo);
                    }
                }

                //Copy export data to blob provider for get public download url
                using (var localStream = SystemFile.Open(localTmpPath, FileMode.Open, FileAccess.Read))
                using (var blobStream = _blobStorageProvider.OpenWrite(relativeUrl))
                {
                    localStream.CopyTo(blobStream);
                }

                // Add unique key for every link to prevent browser caching
                notification.DownloadUrl = $"{_blobUrlResolver.GetAbsoluteUrl(relativeUrl)}?v={DateTime.Now.Ticks}";
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

        private async Task CreateSitemapPartAsync(ZipArchive zipArchive, string storeId, string baseUrl, string sitemapUrl, Action<ExportImportProgressInfo> progressCallback)
        {
            var sitemapPart = zipArchive.CreateEntry(sitemapUrl, CompressionLevel.Optimal);
            using (var sitemapPartStream = sitemapPart.Open())
            {
                var stream = await _sitemapXmlGenerator.GenerateSitemapXmlAsync(storeId, baseUrl, sitemapUrl, progressCallback);
                stream.CopyTo(sitemapPartStream);
            }
        }
    }
}
