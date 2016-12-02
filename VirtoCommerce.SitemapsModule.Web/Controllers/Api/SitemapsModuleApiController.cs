using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Description;
using System.Xml.Serialization;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Core.Web.Security;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
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
        private readonly ISettingsManager _settingManager;

        public SitemapsModuleApiController(ISitemapService sitemapService, ISitemapItemService sitemapItemService, ISitemapXmlGenerator sitemapXmlGenerator, ISettingsManager settingManager)
        {
            _sitemapService = sitemapService;
            _sitemapItemService = sitemapItemService;
            _sitemapXmlGenerator = sitemapXmlGenerator;
            _settingManager = settingManager;
        }

        [HttpPost]
        [Route("search")]
        [ResponseType(typeof(SearchResponse<Sitemap>))]
        public IHttpActionResult SearchSitemaps(SitemapSearchRequest request)
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

            _sitemapService.SaveChanges(sitemap);

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

            _sitemapService.SaveChanges(sitemap);

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
        [ResponseType(typeof(SearchResponse<SitemapItem>))]
        public IHttpActionResult SearchSitemapItems(SitemapItemSearchRequest request)
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
        [Route("{sitemapId}/items")]
        [ResponseType(typeof(void))]
        public IHttpActionResult RemoveSitemapItems(string sitemapId, [FromUri]string[] itemIds)
        {
            if (string.IsNullOrWhiteSpace(sitemapId))
            {
                return BadRequest("sitemapId is null");
            }
            if (itemIds == null)
            {
                return BadRequest("itemIds is null");
            }

            _sitemapItemService.Remove(sitemapId, itemIds);

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpGet]
        [Route("xml")]
        public HttpResponseMessage GenerateSitemapXml(string storeId, string sitemapFilename)
        {
            var apiResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

            if (string.IsNullOrEmpty(storeId) || string.IsNullOrEmpty(sitemapFilename))
            {
                apiResponse.StatusCode = HttpStatusCode.BadRequest;
            }

            var sitemapOptions = GetSetemapOptions();

            using (var stream = new MemoryStream())
            {
                XmlSerializer xmlSerializer = null;

                var xns = new XmlSerializerNamespaces();
                xns.Add("", "http://www.sitemaps.org/schemas/sitemap/0.9");

                if (sitemapFilename.Equals("sitemap.xml", StringComparison.OrdinalIgnoreCase))
                {
                    var sitemapIndex = _sitemapXmlGenerator.GenerateSitemapIndex(storeId, sitemapOptions);
                    xmlSerializer = new XmlSerializer(sitemapIndex.GetType());
                    xmlSerializer.Serialize(stream, sitemapIndex, xns);
                }
                else
                {
                    var sitemap = _sitemapXmlGenerator.GenerateSitemap(storeId, sitemapFilename, sitemapOptions);
                    if (sitemap != null)
                    {
                        xmlSerializer = new XmlSerializer(sitemap.GetType());
                        xmlSerializer.Serialize(stream, sitemap, xns);
                    }
                }

                stream.Position = 0;

                var streamReader = new StreamReader(stream);

                apiResponse.StatusCode = HttpStatusCode.OK;
                apiResponse.Content = new StringContent(streamReader.ReadToEnd(), Encoding.UTF8, "application/xml");
            }

            return apiResponse;
        }

        private SitemapOptions GetSetemapOptions()
        {
            return new SitemapOptions
            {
                CategoryPagePriority = _settingManager.GetValue("Sitemap.CategoryPagePriority", 0.7M),
                CategoryPageUpdateFrequency = _settingManager.GetValue("Sitemap.CategoryPageUpdateFrequency", PageUpdateFrequency.Weekly),
                ProductPagePriority = _settingManager.GetValue("Sitemap.ProductPagePriority", 1.0M),
                ProductPageUpdateFrequency = _settingManager.GetValue("Sitemap.ProductPageUpdateFrequency", PageUpdateFrequency.Daily),
                RecordsLimitPerFile = _settingManager.GetValue("Sitemap.RecordsLimitPerFile", 10000)
            };
        }
    }
}