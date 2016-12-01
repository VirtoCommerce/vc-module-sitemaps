using System;
using System.Linq;
using System.Text;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapUrlBuilder : ISitemapUrlBuilder
    {
        public virtual string ToAbsoluteUrl(Store store, SeoInfo seoInfo)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            if (seoInfo == null)
            {
                throw new ArgumentNullException("seoInfo");
            }

            Uri url = null;

            var storeUrl = string.IsNullOrEmpty(store.Url) ? store.SecureUrl : store.Url;
            if (!string.IsNullOrEmpty(storeUrl) && seoInfo.IsActive)
            {
                var stringBuilder = new StringBuilder(storeUrl);
                var storeLanguage = store.Languages.FirstOrDefault(l => l.Equals(seoInfo.LanguageCode, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(storeLanguage))
                {
                    stringBuilder.AppendFormat("/{0}", storeLanguage);
                    stringBuilder.AppendFormat("/{0}", seoInfo.SemanticUrl);

                    Uri.TryCreate(stringBuilder.ToString().TrimEnd('/'), UriKind.Absolute, out url);
                }
            }

            return url != null ? url.AbsoluteUri : null;
        }

        public virtual string ToAbsoluteUrl(Store store, string relativeUrl)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            if (string.IsNullOrEmpty(relativeUrl))
            {
                throw new ArgumentException("relativeUrl");
            }

            Uri url = null;

            var storeUrl = string.IsNullOrEmpty(store.Url) ? store.SecureUrl : store.Url;
            if (!string.IsNullOrEmpty(storeUrl))
            {
                var stringBuilder = new StringBuilder(storeUrl);
                stringBuilder.AppendFormat("/{0}", relativeUrl);

                Uri.TryCreate(stringBuilder.ToString().TrimEnd('/'), UriKind.Absolute, out url);
            }

            return url != null ? url.AbsoluteUri : null;
        }
    }
}