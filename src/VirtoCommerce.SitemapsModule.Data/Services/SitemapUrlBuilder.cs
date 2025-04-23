using System;
using System.Linq;
using System.Text;
using VirtoCommerce.CatalogModule.Core.Extensions;
using VirtoCommerce.CatalogModule.Core.Model.ListEntry;
using VirtoCommerce.CoreModule.Core.Outlines;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Extensions;
using VirtoCommerce.StoreModule.Core.Model;
using static VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.SEO;

namespace VirtoCommerce.SitemapsModule.Data.Services;

public class SitemapUrlBuilder : ISitemapUrlBuilder
{
    public virtual string BuildStoreUrl(Store store, string language, string urlTemplate, string baseUrl, IEntity entity = null, Outline outline = null)
    {
        var url = ResolveTemplate(urlTemplate, entity, store, language, outline);

        if (IsAbsoluteUri(url))
        {
            return url;
        }

        var builder = new StringBuilder("~");

        if (store != null)
        {
            // Add store URL
            if (TryGetBaseUrl(baseUrl, store, out var newBaseUrl))
            {
                builder.Clear();
                builder.Append(newBaseUrl.TrimEnd('/'));
            }

            // Add language if store has multiple languages
            if (store.Languages?.Count > 1)
            {
                var actualLanguage = store.Languages.FirstOrDefault(x => x.EqualsIgnoreCase(language)) ?? store.DefaultLanguage;

                if (!url.Contains($"/{actualLanguage}/", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Append('/');
                    builder.Append(actualLanguage);
                }
            }
        }

        builder.Append('/');
        builder.Append(url.TrimStart('~', '/'));

        return builder.ToString();
    }


    private static bool TryGetBaseUrl(string baseUrl, Store store, out string newBaseUrl)
    {
        if (!string.IsNullOrEmpty(baseUrl))
        {
            newBaseUrl = baseUrl;
            return true;
        }

        if (!string.IsNullOrEmpty(store.Url))
        {
            newBaseUrl = store.Url;
            return true;
        }

        if (!string.IsNullOrEmpty(store.SecureUrl))
        {
            newBaseUrl = store.SecureUrl;
            return true;
        }

        newBaseUrl = null;
        return false;
    }

    private static string ResolveTemplate(string urlTemplate, IEntity entity, Store store, string language, Outline outline)
    {
        // Override SEO links type if explicitly set in urlTemplate
        var seoLinksType = urlTemplate switch
        {
            UrlTemplatePatterns.SlugLong => SeoLong,
            UrlTemplatePatterns.SlugShort => SeoShort,
            UrlTemplatePatterns.SlugCollapsed => SeoCollapsed,
            _ => store?.GetSeoLinksType(),
        };

        var seoPath = entity is ISeoSupport seoSupport
            ? GetSeoPath(seoSupport, seoLinksType, store, language, outline)
            : string.Empty;

        var builder = new StringBuilder(urlTemplate);

        // Remove unused {language} template
        builder.Replace(UrlTemplatePatterns.Language, string.Empty);

        // Replace {slug} templates with actual SEO path
        builder.Replace(UrlTemplatePatterns.SlugLong, seoPath);
        builder.Replace(UrlTemplatePatterns.SlugShort, seoPath);
        builder.Replace(UrlTemplatePatterns.SlugCollapsed, seoPath);
        builder.Replace(UrlTemplatePatterns.Slug, seoPath);

        return builder.ToString();
    }

    private static string GetSeoPath(ISeoSupport entity, string seoLinksType, Store store, string language, Outline outline)
    {
        string seoPath;

        if (seoLinksType == SeoNone)
        {
            seoPath = entity is ListEntryBase listEntry ? $"{listEntry.Type}/{entity.Id}" : entity.Id;
        }
        else
        {
            seoPath = entity.GetBestMatchingSeoInfo(store, language)?.SemanticUrl ?? string.Empty;
        }

        if (outline != null)
        {
            seoPath = outline.Items.GetSeoPath(store, language, defaultValue: seoPath, seoLinksType);
        }
        else if (entity is IHasOutlines hasOutlines)
        {
            seoPath = hasOutlines.GetSeoPath(store, language, defaultValue: seoPath, seoLinksType);
        }

        return seoPath;
    }

    private static bool IsAbsoluteUri(string uriString)
    {
        return Uri.TryCreate(uriString, UriKind.Absolute, out var uri) && uri.Scheme != "file";
    }
}
