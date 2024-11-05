using System.IO;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.SitemapsModule.Core;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Models.Search;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.BackgroundJobs;
using VirtoCommerce.SitemapsModule.Data.Model.PushNotifications;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.SitemapsModule.Web.Extensions;

namespace VirtoCommerce.SitemapsModule.Web.Controllers.Api
{
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
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly PlatformOptions _platformOptions;

        public SitemapsModuleApiController(
            ISitemapService sitemapService,
            ISitemapItemService sitemapItemService,
            ISitemapSearchService sitemapSearchService,
            ISitemapItemSearchService sitemapItemSearchService,
            ISitemapXmlGenerator sitemapXmlGenerator,
            IUserNameResolver userNameResolver,
            IPushNotificationManager notifier,
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
            _hostingEnvironment = hostingEnvironment;
            _platformOptions = platformOptions.Value;
        }

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

        [HttpGet]
        [Route("schema")]
        public async Task<ActionResult<string[]>> GetSitemapsSchema(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                return BadRequest("storeId is empty");
            }

            var sitemapUrls = await _sitemapXmlGenerator.GetSitemapUrlsAsync(storeId, string.Empty);
            return Ok(sitemapUrls);
        }

        [HttpGet]
        [Route("generate")]
        [SwaggerFileResponse]
        public async Task<ActionResult> GenerateSitemap(string storeId, string baseUrl, string sitemapUrl)
        {
            var stream = await _sitemapXmlGenerator.GenerateSitemapXmlAsync(storeId, baseUrl, sitemapUrl);
            return new FileStreamResult(stream, "text/xml");
        }

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

            var localTmpFolder = _hostingEnvironment.MapPath(Path.Combine("~/", _platformOptions.LocalUploadFolderPath, "tmp"));

            BackgroundJob.Enqueue<SitemapExportToAssetsJob>(job => job.BackgroundDownload(storeId, baseUrl, localTmpFolder, notification));

            return Ok(notification);
        }

        [HttpGet]
        [Route("exportToStoreAssets")]
        [Authorize(ModuleConstants.Security.Permissions.ExportToStoreAssets)]
        public async Task<ActionResult<SitemapDownloadNotification>> ExportToStoreAssets(string storeId, string baseUrl)
        {
            var notification = new SitemapExportToAssetNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Exporting sitemaps to store assets",
                Description = "Processing exporting sitemaps to store assets..."
            };

            await _notifier.SendAsync(notification);

            BackgroundJob.Enqueue<SitemapExportToAssetsJob>(job => job.BackgroundExportToAssets(storeId, baseUrl, notification));

            return Ok(notification);
        }
    }
}
