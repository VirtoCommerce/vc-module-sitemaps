using System;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapUrlBuilder : ISitemapUrlBuilder
    {
        public virtual string CreateAbsoluteUrl(Store store, string urlTemplate, string language = null, string semanticUrl = null)
        {
            string relativeUrl = urlTemplate;
            if (urlTemplate.IsAbsoluteUrl())
            {
                return urlTemplate;
            }

            if (!string.IsNullOrEmpty(store.Url))
            {
                relativeUrl = relativeUrl.Replace(UrlTemplatePatterns.StoreUrl, store.Url);
            }

            if (!string.IsNullOrEmpty(store.SecureUrl))
            {
                relativeUrl = relativeUrl.Replace(UrlTemplatePatterns.StoreSecureUrl, store.SecureUrl);
            }

            if (!string.IsNullOrEmpty(language))
            {
                relativeUrl = relativeUrl.Replace(UrlTemplatePatterns.Language, language);
            }

            if (!string.IsNullOrEmpty(semanticUrl))
            {
                relativeUrl = relativeUrl.Replace(UrlTemplatePatterns.Slug, semanticUrl);
            }

            Uri uri = null;
            if (relativeUrl != urlTemplate)
            {
                Uri.TryCreate(relativeUrl, UriKind.Absolute, out uri);
            }

            return uri != null ? Uri.UnescapeDataString(uri.AbsoluteUri) : null;
        }
    }
}