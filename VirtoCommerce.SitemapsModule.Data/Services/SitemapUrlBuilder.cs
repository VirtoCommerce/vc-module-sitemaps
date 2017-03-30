using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.Tools;
using VirtoCommerce.Tools.Models;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapUrlBuilder : ISitemapUrlBuilder
    {
        public SitemapUrlBuilder(IUrlBuilder urlBuilder)
        {
            UrlBuilder = urlBuilder;
        }

        protected IUrlBuilder UrlBuilder { get; private set; }

        public virtual string CreateAbsoluteUrl(Domain.Store.Model.Store store, string urlTemplate, string baseUrl, string language = null, string semanticUrl = null)
        {
            if (urlTemplate.IsAbsoluteUrl())
            {
                return urlTemplate;
            }

            var url = urlTemplate;
            if (!string.IsNullOrEmpty(baseUrl))
            {
                url = string.Format("{0}/{1}", baseUrl.TrimEnd('/'), urlTemplate);
            }

            url = url.Replace(UrlTemplatePatterns.Language, language);
            url = url.Replace(UrlTemplatePatterns.Slug, semanticUrl);

            var urlBuilderStore = new Store
            {
                Catalog = store.Catalog,
                DefaultLanguage = store.DefaultLanguage,
                Id = store.Id,
                SecureUrl = store.SecureUrl,
                SeoLinksType = GetSeoLinksType(store)
            };

            var urlBuilderContext = new UrlBuilderContext
            {
                AllStores = new List<Store> { urlBuilderStore },
                CurrentLanguage = language,
                CurrentStore = urlBuilderStore,
                CurrentUrl = url
            };
            var result = UrlBuilder.BuildStoreUrl(urlBuilderContext, url);

            return result;
        }

        private SeoLinksType GetSeoLinksType(Domain.Store.Model.Store store)
        {
            var seoLinksType = SeoLinksType.Long;

            if (store.Settings != null)
            {
                var seoLinksTypeSetting = store.Settings.FirstOrDefault(s => s.Name == "Stores.SeoLinksType");
                if (seoLinksTypeSetting != null)
                {
                    seoLinksType = EnumUtility.SafeParse(seoLinksTypeSetting.Value, SeoLinksType.Collapsed);
                }
            }

            return seoLinksType;
        }
    }
}