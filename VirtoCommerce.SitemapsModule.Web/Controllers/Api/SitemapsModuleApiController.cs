using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.SitemapsModule.Web.Model;

namespace VirtoCommerce.SitemapsModule.Web.Controllers.Api
{
    [RoutePrefix("api/sitemaps")]
    public class SitemapsModuleApiController : ApiController
    {
        private readonly ISitemapService _sitemapService;

        public SitemapsModuleApiController(ISitemapService sitemapService)
        {
            _sitemapService = sitemapService;
        }

        [HttpGet]
        [Route("")]
        [ResponseType(typeof(Sitemap[]))]
        public IHttpActionResult GetAll(string storeId)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                return BadRequest("storeId is null");
            }

            var dataSitemaps = _sitemapService.GetSitemaps(storeId);

            var webSitemaps = new List<Sitemap>(dataSitemaps.Select(i => AbstractTypeFactory<Sitemap>.TryCreateInstance().FromDataModel(i)));

            return Ok(webSitemaps);
        }

        [HttpGet]
        [Route("{sitemapId}")]
        [ResponseType(typeof(Sitemap))]
        public IHttpActionResult GetById(string storeId, string sitemapId)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                return BadRequest("storeId is null");
            }
            if (string.IsNullOrWhiteSpace(sitemapId))
            {
                return BadRequest("sitemapId is null");
            }

            var dataSitemap = _sitemapService.GetSitemapById(storeId, sitemapId);
            if (dataSitemap == null)
            {
                return NotFound();
            }

            var webSitemap = AbstractTypeFactory<Sitemap>.TryCreateInstance().FromDataModel(dataSitemap);

            return Ok(webSitemap);
        }

        [HttpPost]
        [Route("")]
        [ResponseType(typeof(void))]
        public IHttpActionResult Create(Sitemap sitemap)
        {
            if (sitemap == null)
            {
                return BadRequest("sitemap is null");
            }

            _sitemapService.SaveChanges(new[] { sitemap.ToDataModel() });

            return Ok();
        }

        [HttpPut]
        [Route("")]
        [ResponseType(typeof(void))]
        public IHttpActionResult Update(Sitemap sitemap)
        {
            if (sitemap == null)
            {
                return BadRequest("sitemap is null");
            }

            _sitemapService.SaveChanges(new[] { sitemap.ToDataModel() });

            return Ok();
        }

        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(void))]
        public IHttpActionResult Delete(string storeId, [FromUri]string[] sitemapIds)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                return BadRequest("storeId is null");
            }
            if (sitemapIds == null)
            {
                return BadRequest("sitemapIds is null");
            }

            _sitemapService.DeleteSitemaps(storeId, sitemapIds);

            return Ok();
        }
    }
}