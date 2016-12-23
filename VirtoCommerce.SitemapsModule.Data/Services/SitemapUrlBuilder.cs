using System;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapUrlBuilder : ISitemapUrlBuilder
    {
        public virtual string CreateAbsoluteUrl(string urlTemplate, string baseUrl, string language = null, string semanticUrl = null)
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

            Uri uri;
            Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri);

            return uri != null ? uri.ToString() : null;
        }
    }
}