using System;
using System.Linq;
using System.Text;
using VirtoCommerce.CatalogModule.Core.Extensions;
using VirtoCommerce.CatalogModule.Core.Model.ListEntry;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Outlines;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Extensions;
using VirtoCommerce.StoreModule.Core.Model;
using static VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.SEO;

namespace VirtoCommerce.SitemapsModule.Data.Services;

public class SitemapUrlBuilder(ICategoryService categoryService) : ISitemapUrlBuilder
{
    public virtual string BuildStoreUrl(Store store, string language, string urlTemplate, string baseUrl, IEntity entity = null)
    {
        var url = ResolveTemplate(urlTemplate, entity, store, language);

        if (IsAbsoluteUri(url))
        {
            return url;
        }

        var builder = new StringBuilder("~");

        if (store != null)
        {
            // Add store URL
            if (!string.IsNullOrEmpty(baseUrl) || !string.IsNullOrEmpty(store.Url) || !string.IsNullOrEmpty(store.SecureUrl))
            {
                baseUrl = baseUrl.EmptyToNull() ?? store.Url.EmptyToNull() ?? store.SecureUrl.EmptyToNull() ?? string.Empty;

                builder.Clear();
                builder.Append(baseUrl.TrimEnd('/'));
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

    private string ResolveTemplate(string urlTemplate, IEntity entity, Store store, string language)
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
            ? GetSeoPath(seoSupport, seoLinksType, store, language)
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

    private string GetSeoPath(ISeoSupport entity, string seoLinksType, Store store, string language)
    {
        string seoPath;

        if (seoLinksType == SeoNone)
        {
            seoPath = entity is ListEntryBase listEntry ? $"{listEntry.Type}/{entity.Id}" : entity.Id;
        }
        else
        {
            seoPath = entity.GetBestMatchingSeoInfo(store, language)?.SemanticUrl ?? string.Empty;

            // CategoryListEntry does not have IHasOutlines interface, but Category does,
            // and we need outlines to build long or collapsed SEO path, so retrieve Category by id to have outlines
            if (entity is CategoryListEntry categoryListEntry && seoLinksType is SeoLong or SeoCollapsed)
            {
                entity = categoryService.GetByIdAsync(categoryListEntry.Id).GetAwaiter().GetResult();
            }
        }

        if (entity is IHasOutlines hasOutlines)
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
