using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Domain.Commerce.Model.Search;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Web.Security;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Models.Xml;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.SitemapsModule.Web.Model;
using VirtoCommerce.SitemapsModule.Web.Security;

namespace VirtoCommerce.SitemapsModule.Web.Controllers.Api
{
    [RoutePrefix("api/sitemaps")]
    [CheckPermission(Permission = SitemapsPredefinedPermissions.Read)]
    public class SitemapsModuleApiController : ApiController
    {
        private readonly ISitemapService _sitemapService;
        private readonly ISitemapItemService _sitemapItemService;
        private readonly ISitemapXmlGenerator _sitemapXmlGenerator;
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly IBlobUrlResolver _blobUrlResolver;

        public SitemapsModuleApiController(
            ISitemapService sitemapService,
            ISitemapItemService sitemapItemService,
            ISitemapXmlGenerator sitemapXmlGenerator,
            IBlobStorageProvider blobStorageProvider,
            IBlobUrlResolver blobUrlResolver)
        {
            _sitemapService = sitemapService;
            _sitemapItemService = sitemapItemService;
            _sitemapXmlGenerator = sitemapXmlGenerator;
            _blobStorageProvider = blobStorageProvider;
            _blobUrlResolver = blobUrlResolver;
        }

        [HttpPost]
        [Route("search")]
        [ResponseType(typeof(GenericSearchResult<Sitemap>))]
        public IHttpActionResult SearchSitemaps(SitemapSearchCriteria request)
        {
            if (request == null)
            {
                return BadRequest("request is null");
            }

            var sitemapSearchResponse = _sitemapService.Search(request);

            return Ok(sitemapSearchResponse);
        }

        [HttpGet]
        [Route("{id}")]
        [ResponseType(typeof(Sitemap))]
        public IHttpActionResult GetSitemapById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("id is null");
            }

            var sitemap = _sitemapService.GetById(id);

            if (sitemap == null)
            {
                return NotFound();
            }

            return Ok(sitemap);
        }

        [HttpPost]
        [Route("")]
        [ResponseType(typeof(void))]
        [CheckPermission(Permission = SitemapsPredefinedPermissions.Create)]
        public IHttpActionResult AddSitemap(Sitemap sitemap)
        {
            if (sitemap == null)
            {
                return BadRequest("sitemap is null");
            }

            _sitemapService.SaveChanges(new[] { sitemap });

            return Ok(sitemap);
        }

        [HttpPut]
        [Route("")]
        [ResponseType(typeof(void))]
        [CheckPermission(Permission = SitemapsPredefinedPermissions.Update)]
        public IHttpActionResult UpdateSitemap(Sitemap sitemap)
        {
            if (sitemap == null)
            {
                return BadRequest("sitemap is null");
            }

            _sitemapService.SaveChanges(new[] { sitemap });

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(void))]
        [CheckPermission(Permission = SitemapsPredefinedPermissions.Delete)]
        public IHttpActionResult DeleteSitemap([FromUri]string[] ids)
        {
            if (ids == null)
            {
                return BadRequest("ids is null");
            }

            _sitemapService.Remove(ids);

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        [Route("items/search")]
        [ResponseType(typeof(GenericSearchResult<SitemapItem>))]
        public IHttpActionResult SearchSitemapItems(SitemapItemSearchCriteria request)
        {
            if (request == null)
            {
                return BadRequest("request is null");
            }

            var searchSitemapItemResponse = _sitemapItemService.Search(request);

            return Ok(searchSitemapItemResponse);
        }

        [HttpPost]
        [Route("{sitemapId}/items")]
        [ResponseType(typeof(void))]
        public IHttpActionResult AddSitemapItems(string sitemapId, [FromBody]SitemapItem[] items)
        {
            if (string.IsNullOrEmpty(sitemapId))
            {
                return BadRequest("sitemapId is null");
            }
            if (items == null)
            {
                return BadRequest("items is null");
            }

            _sitemapItemService.Add(sitemapId, items);

            return Ok();
        }

        [HttpDelete]
        [Route("items")]
        [ResponseType(typeof(void))]
        public IHttpActionResult RemoveSitemapItems([FromUri]string[] itemIds)
        {
            if (itemIds == null)
            {
                return BadRequest("itemIds is null");
            }

            _sitemapItemService.Remove(itemIds);

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpGet]
        [Route("schema")]
        [ResponseType(typeof(string[]))]
        public IHttpActionResult GetSitemapsSchema(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                return BadRequest("storeId is empty");
            }

            var sitemapUrls = _sitemapXmlGenerator.GetSitemapUrls(storeId);

            return Ok(sitemapUrls);
        }

        [HttpGet]
        [Route("generate")]
        [ResponseType(typeof(Stream))]
        public IHttpActionResult GenerateSitemap(string storeId, string sitemapUrl)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                return BadRequest("storeId is empty");
            }
            if (string.IsNullOrEmpty(sitemapUrl))
            {
                return BadRequest("sitemapUrl is empty");
            }

            var stream = _sitemapXmlGenerator.GenerateSitemapXml(storeId, sitemapUrl);

            return Ok(stream);
        }

        [HttpGet]
        [Route("download")]
        [ResponseType(typeof(SitemapPackage))]
        public IHttpActionResult DownloadSitemaps(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                return BadRequest("storeId is empty");
            }

            var zipPackageRelativeUrl = "tmp/sitemap.zip";

            using (var targetStream = _blobStorageProvider.OpenWrite(zipPackageRelativeUrl))
            {
                using (var zipPackage = ZipPackage.Open(targetStream, FileMode.Create))
                {
                    CreateSitemapPart(zipPackage, storeId, "sitemap.xml");
                    var sitemapUrls = _sitemapXmlGenerator.GetSitemapUrls(storeId);
                    foreach (var sitemapUrl in sitemapUrls)
                    {
                        var filename = sitemapUrl.Split('/').LastOrDefault();
                        if (!string.IsNullOrEmpty(filename))
                        {
                            CreateSitemapPart(zipPackage, storeId, filename);
                        }
                    }
                }
            }

            return Ok(new SitemapPackage { Url = _blobUrlResolver.GetAbsoluteUrl(zipPackageRelativeUrl) });
        }

        private void CreateSitemapPart(System.IO.Packaging.Package package, string storeId, string sitemapFilename)
        {
            var uri = PackUriHelper.CreatePartUri(new Uri(sitemapFilename, UriKind.Relative));
            var sitemapPart = package.CreatePart(uri, System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Normal);
            var stream = _sitemapXmlGenerator.GenerateSitemapXml(storeId, sitemapFilename);
            var sitemapPartStream = sitemapPart.GetStream();
            stream.CopyTo(sitemapPartStream);
        }
    }
}