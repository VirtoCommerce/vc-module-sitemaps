using System.Linq;
using VirtoCommerce.CatalogModule.Core.Model.ListEntry;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Outlines;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Converters;
using VirtoCommerce.SitemapsModule.Data.Extensions;
using VirtoCommerce.Tools;
using VirtoCommerce.Tools.Models;
using Store = VirtoCommerce.StoreModule.Core.Model.Store;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapUrlBuilder : ISitemapUrlBuilder
    {
        private readonly ICategoryService _categoryService;

        public SitemapUrlBuilder(IUrlBuilder urlBuilder, ICategoryService categoryService)
        {
            UrlBuilder = urlBuilder;
            _categoryService = categoryService;
        }

        protected IUrlBuilder UrlBuilder { get; private set; }

        #region ISitemapUrlBuilder members
        public virtual string BuildStoreUrl(Store store, string language, string urlTemplate, string baseUrl, IEntity entity = null)
        {
            var toolsStore = store.ToToolsStore(baseUrl);
            if (!string.IsNullOrEmpty(baseUrl))
            {
                toolsStore.Url = baseUrl;
            }

            // Override SEO link type, if explicitly set in urlTemplate
            switch (urlTemplate)
            {
                case UrlTemplatePatterns.SlugLong:
                    toolsStore.SeoLinksType = SeoLinksType.Long;
                    break;
                case UrlTemplatePatterns.SlugShort:
                    toolsStore.SeoLinksType = SeoLinksType.Short;
                    break;
                case UrlTemplatePatterns.SlugCollapsed:
                    toolsStore.SeoLinksType = SeoLinksType.Collapsed;
                    break;
            }

            var seoSupport = entity as ISeoSupport;

            //remove unused {language} template
            urlTemplate = urlTemplate.Replace(UrlTemplatePatterns.Language, string.Empty);

            var slug = string.Empty;
            if (seoSupport != null)
            {
                var hasOutlines = entity as IHasOutlines;
                var seoInfos = seoSupport.SeoInfos?.Select(x => x.JsonConvert<Tools.Models.SeoInfo>());
                seoInfos = seoInfos?.GetBestMatchingSeoInfos(toolsStore.Id, toolsStore.DefaultLanguage, language, null);
                if (toolsStore.SeoLinksType == SeoLinksType.None)
                {
                    slug = entity is ListEntryBase @base ? $"{@base.Type}/{entity.Id}" : entity.Id;
                }
                else if (!seoInfos.IsNullOrEmpty())
                {
                    slug = seoInfos.Select(x => x.SemanticUrl).FirstOrDefault();
                }

                if (hasOutlines != null && !hasOutlines.Outlines.IsNullOrEmpty())
                {
                    var outlines = hasOutlines.Outlines.Select(x => x.JsonConvert<Tools.Models.Outline>());
                    slug = outlines.GetSeoPath(toolsStore, language, slug);
                }

                var categoryNeedsOutlines = new[] { SeoLinksType.Long, SeoLinksType.Collapsed }.Contains(toolsStore.SeoLinksType);
                if (categoryNeedsOutlines && entity is CategoryListEntry categoryListEntry)
                {
                    // CategoryListEntry does not have IHasOutlines interface, but Categoy does
                    // And we need outlines to build long and collapsed SEO path, so retrieve Category by id to have outlines
                    var category = _categoryService.GetByIdAsync(categoryListEntry.Id).GetAwaiter().GetResult();
                    var outlines = category.Outlines.Select(x => x.JsonConvert<Tools.Models.Outline>());
                    slug = outlines.GetSeoPath(toolsStore, language, slug);
                }
            }
            var toolsContext = new Tools.Models.UrlBuilderContext
            {
                AllStores = new[] { toolsStore },
                CurrentLanguage = language,
                CurrentStore = toolsStore
            };
            //Replace {slug} template in passed url template
            urlTemplate = urlTemplate.Replace(UrlTemplatePatterns.SlugLong, slug);
            urlTemplate = urlTemplate.Replace(UrlTemplatePatterns.SlugShort, slug);
            urlTemplate = urlTemplate.Replace(UrlTemplatePatterns.SlugCollapsed, slug);
            urlTemplate = urlTemplate.Replace(UrlTemplatePatterns.Slug, slug);
            var result = UrlBuilder.BuildStoreUrl(toolsContext, urlTemplate);
            return result;
        }
        #endregion
    }
}
