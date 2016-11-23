using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.SitemapsModule.Core.Model;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Web.Controllers.Api
{
    [RoutePrefix("api/sitemaps")]
    public class SitemapsModuleApiController : ApiController
    {
        private readonly ISitemapService _sitemapService;
        private readonly ISitemapItemService _sitemapItemService;

        public SitemapsModuleApiController(ISitemapService sitemapService, ISitemapItemService sitemapItemService)
        {
            _sitemapService = sitemapService;
            _sitemapItemService = sitemapItemService;
        }

        [HttpPost]
        [Route("search")]
        [ResponseType(typeof(SearchResponse<Sitemap>))]
        public IHttpActionResult SearchSitemap(SitemapSearchRequest request)
        {
            if (request == null)
            {
                return BadRequest("request is null");
            }

            return Ok(_sitemapService.Search(request));
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

            _sitemapService.SaveChanges(new[] { sitemap });

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

            _sitemapService.SaveChanges(new[] { sitemap });

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

        [HttpPost]
        [Route("items/search")]
        [ResponseType(typeof(SearchResponse<SitemapItem>))]
        public IHttpActionResult SearchItems(SitemapItemSearchRequest request)
        {
            if (request == null)
            {
                return BadRequest("request is null");
            }

            return Ok(_sitemapItemService.Search(request));
        }

        [HttpPost]
        [Route("{sitemapId}/items")]
        [ResponseType(typeof(void))]
        public IHttpActionResult AddItems(string sitemapId, [FromBody]SitemapItem[] items)
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
        public IHttpActionResult RemoveItems(string sitemapId, [FromUri]string[] itemIds)
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

            return Ok();
        }
    }
}