using System;
using System.Linq;
using System.Text;
using VirtoCommerce.CatalogModule.Core.Extensions;
using VirtoCommerce.CatalogModule.Core.Model.ListEntry;
using VirtoCommerce.CoreModule.Core.Outlines;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;
using StoreSettings = VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.SEO;

namespace VirtoCommerce.SitemapsModule.Data.Services;

public class SitemapUrlBuilder : ISitemapUrlBuilder
{
    public virtual string BuildStoreUrl(Store store, string language, string urlTemplate, string baseUrl, IEntity entity = null)
    {
        // Remove unused {language} template
        urlTemplate = urlTemplate.Replace(UrlTemplatePatterns.Language, string.Empty);

        // Replace {slug} template in the provided URL template
        var slug = GetSlug(store, language, entity);
        urlTemplate = urlTemplate.Replace(UrlTemplatePatterns.Slug, slug);

        // Don't process absolute URL
        if (Uri.TryCreate(urlTemplate, UriKind.Absolute, out var absoluteUri) && absoluteUri.Scheme != "file")
        {
            return urlTemplate;
        }

        var builder = new StringBuilder("~");

        if (store != null)
        {
            // If store has public or secure URL, use them
            if (!string.IsNullOrEmpty(baseUrl) || !string.IsNullOrEmpty(store.Url) || !string.IsNullOrEmpty(store.SecureUrl))
            {
                baseUrl = baseUrl.EmptyToNull() ?? store.Url.EmptyToNull() ?? store.SecureUrl.EmptyToNull() ?? string.Empty;

                builder.Clear();
                builder.Append(baseUrl.TrimEnd('/'));
            }

            // Do not add language to URL if store has only one language
            if (store.Languages?.Count > 1)
            {
                var actualLanguage = store.Languages.FirstOrDefault(x => x.EqualsIgnoreCase(language)) ?? store.DefaultLanguage;

                if (!urlTemplate.Contains($"/{actualLanguage}/", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Append('/');
                    builder.Append(actualLanguage);
                }
            }
        }

        builder.Append('/');
        builder.Append(urlTemplate.TrimStart('~', '/'));

        return builder.ToString();
    }

    private static string GetSlug(Store store, string language, IEntity entity)
    {
        if (entity is not ISeoSupport seoSupport)
        {
            return string.Empty;
        }

        string slug;

        var seoLinksType = store?.Settings?.GetValue<string>(StoreSettings.SeoLinksType);
        if (seoLinksType == "None")
        {
            slug = entity is ListEntryBase listEntry ? $"{listEntry.Type}/{entity.Id}" : entity.Id;
        }
        else
        {
            slug = seoSupport.GetBestMatchingSeoInfo(store, language)?.SemanticUrl ?? string.Empty;
        }

        if (entity is IHasOutlines hasOutlines)
        {
            slug = hasOutlines.GetSeoPath(store, language) ?? slug;
        }

        return slug;
    }
}
