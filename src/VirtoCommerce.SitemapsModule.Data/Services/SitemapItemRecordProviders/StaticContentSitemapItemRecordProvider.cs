using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.ContentModule.Core.Model;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Extensions;
using VirtoCommerce.StoreModule.Core.Model;
using YamlDotNet.RepresentationModel;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class StaticContentSitemapItemRecordProvider : SitemapItemRecordProviderBase, ISitemapItemRecordProvider
    {
        private const string PagesContentType = "pages";
        private const string BlogsContentType = "blogs";
        private static readonly Regex _headerRegExp = new Regex(@"(?s:^---(.*?)---)");

        private readonly ISettingsManager _settingsManager;
        private readonly IContentFileService _contentFileService;
        private readonly IContentService _contentService;

        public StaticContentSitemapItemRecordProvider(
            ISitemapUrlBuilder urlBuilder,
            ISettingsManager settingsManager,
            IContentService contentService,
            IContentFileService contentFileService)
            : base(urlBuilder)
        {
            _settingsManager = settingsManager;
            _contentService = contentService;
            _contentFileService = contentFileService;
        }

        public virtual async Task LoadSitemapItemRecordsAsync(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var progressInfo = new ExportImportProgressInfo();

            var staticContentSitemapItems = GetStaticContentSitemapItems(sitemap);

            var totalCount = staticContentSitemapItems.Count;
            if (totalCount == 0)
            {
                return;
            }

            var processedCount = 0;

            var allowedExtensions = await GetAllowedExtensions();
            var blogOptions = await GetBlogOptions(store);

            progressInfo.Description = $"Content: Starting records generation  for {totalCount} pages";
            progressCallback?.Invoke(progressInfo);

            foreach (var sitemapItem in staticContentSitemapItems)
            {
                var validSitemapItems = new List<string>();

                if (sitemapItem.ObjectType.EqualsInvariant(SitemapItemTypes.Folder))
                {
                    await LoadPagesRecursively(sitemap.StoreId, sitemapItem.UrlTemplate, allowedExtensions, validSitemapItems);
                }
                else if (sitemapItem.ObjectType.EqualsInvariant(SitemapItemTypes.ContentItem) &&
                    IsExtensionAllowed(allowedExtensions, sitemapItem.UrlTemplate) &&
                    await _contentService.ItemExistsAsync(PagesContentType, sitemap.StoreId, sitemapItem.UrlTemplate))
                {
                    validSitemapItems.Add(sitemapItem.UrlTemplate);
                }

                totalCount = validSitemapItems.Count;

                foreach (var url in validSitemapItems)
                {
                    ContentFile contentFile = null;
                    var contentType = PagesContentType;
                    if (await _contentService.ItemExistsAsync(PagesContentType, sitemap.StoreId, url))
                    {
                        contentFile = await _contentService.GetFileAsync(PagesContentType, sitemap.StoreId, url);
                    }
                    else if (await _contentService.ItemExistsAsync(BlogsContentType, sitemap.StoreId, url))
                    {
                        contentType = BlogsContentType;
                        contentFile = await _contentService.GetFileAsync(BlogsContentType, sitemap.StoreId, url);
                    }

                    if (contentFile != null)
                    {
                        using (var stream = await _contentService.GetItemStreamAsync(contentType, sitemap.StoreId, url))
                        {
                            var content = stream.ReadToString();

                            var frontMatterPermalink = GetPermalink(content, url, contentFile.Url);
                            var urlTemplate = frontMatterPermalink.ToUrl().TrimStart('/');

                            var records = GetSitemapItemRecords(store, blogOptions, urlTemplate, baseUrl);
                            sitemapItem.ItemsRecords.AddRange(records);
                        }

                        processedCount++;
                        progressInfo.Description = $"Content: Have been generated records for {processedCount} of {totalCount} pages";
                        progressCallback?.Invoke(progressInfo);
                    }
                }
            }
        }

        private async Task LoadPagesRecursively(string storeId, string folderUrl, List<string> allowedExtensions, List<string> validSitemapItems)
        {
            var criteria = AbstractTypeFactory<FilterItemsCriteria>.TryCreateInstance();
            criteria.ContentType = PagesContentType;
            criteria.StoreId = storeId;
            criteria.FolderUrl = folderUrl;

            var searchResult = await _contentFileService.FilterItemsAsync(criteria);
            //In case if we not find any content in the pages try to search in the blogs
            if (!searchResult.Any())
            {
                criteria.ContentType = BlogsContentType;
                searchResult = await _contentFileService.FilterItemsAsync(criteria);
            }

            foreach (var file in searchResult.Where(file => file.Type == "blob" && IsExtensionAllowed(allowedExtensions, file.RelativeUrl)))
            {
                validSitemapItems.Add(file.RelativeUrl);
            }

            // Load Pages from SubFolders
            foreach (var folder in searchResult.Where(file => file.Type == "folder"))
            {
                await LoadPagesRecursively(storeId, folder.RelativeUrl, allowedExtensions, validSitemapItems);
            }
        }

        private static List<SitemapItem> GetStaticContentSitemapItems(Sitemap sitemap)
        {
            return sitemap.Items
                            .Where(si => !string.IsNullOrEmpty(si.ObjectType))
                            .Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.ContentItem) || si.ObjectType.EqualsInvariant(SitemapItemTypes.Folder))
                            .ToList();
        }

        private async Task<List<string>> GetAllowedExtensions()
        {
            return (await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.AcceptedFilenameExtensions))
                            .Split(',')
                            .Select(i => i.Trim())
                            .Where(i => !string.IsNullOrEmpty(i))
                            .ToList();
        }

        private async Task<SitemapItemOptions> GetBlogOptions(Store store)
        {
            var storeOptionPriority = store.Settings.GetValue<decimal>(ModuleConstants.Settings.BlogLinks.BlogPagePriority);
            var storeOptionUpdateFrequency = store.Settings.GetValue<string>(ModuleConstants.Settings.BlogLinks.BlogPageUpdateFrequency);

            return new SitemapItemOptions
            {
                Priority = storeOptionPriority > -1
                    ? storeOptionPriority
                    : await _settingsManager.GetValueAsync<decimal>(ModuleConstants.Settings.BlogLinks.BlogPagePriority),
                UpdateFrequency = !string.IsNullOrEmpty(storeOptionUpdateFrequency)
                    ? storeOptionUpdateFrequency
                    : await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.BlogLinks.BlogPageUpdateFrequency),
            };
        }

        private static bool IsExtensionAllowed(IList<string> acceptedFilenameExtensions, string itemUrl)
        {
            if (!acceptedFilenameExtensions.Any())
            {
                return true;
            }

            var itemExtension = Path.GetExtension(itemUrl);

            return string.IsNullOrEmpty(itemExtension) || acceptedFilenameExtensions.Contains(itemExtension, StringComparer.OrdinalIgnoreCase);
        }

        private static FrontMatterPermalink GetPermalink(string content, string url, string filePath)
        {
            if (content.TryParseJson(out var token) && token.HasValues)
            {
                if (token is JArray array && array.First?["permalink"] != null)
                {
                    return new FrontMatterPermalink(array.First["permalink"].ToString());
                }
                if (token["settings"] != null && token["settings"]?["permalink"] != null)
                {
                    return new FrontMatterPermalink(token["settings"]["permalink"].ToString());
                }
            }

            var yamlHeader = ReadYamlHeader(content);
            yamlHeader.TryGetValue("permalink", out var permalinks);
            if (permalinks != null)
            {
                return new FrontMatterPermalink(permalinks.FirstOrDefault()) { FilePath = filePath };
            }

            return new FrontMatterPermalink(url.Replace(".md", "")) { FilePath = filePath };
        }

        private static async Task<ICollection<string>> GetItemUrls(IBlobContentStorageProvider storageProvider, GenericSearchResult<BlobEntry> searchResult)
        {
            var urls = new List<string>();

            foreach (var item in searchResult.Results.OfType<BlobInfo>())
            {
                urls.Add(item.RelativeUrl);
            }
            foreach (var folder in searchResult.Results.OfType<BlobFolder>())
            {
                var folderSearchResult = await storageProvider.SearchAsync(folder.RelativeUrl, null);
                urls.AddRange(await GetItemUrls(storageProvider, folderSearchResult));
            }

            return urls;
        }

        private static IDictionary<string, IEnumerable<string>> ReadYamlHeader(string text)
        {
            var retVal = new Dictionary<string, IEnumerable<string>>();
            var headerMatches = _headerRegExp.Matches(text);
            if (headerMatches.Count == 0)
            {
                return retVal;
            }

            var input = new StringReader(headerMatches[0].Groups[1].Value);
            var yaml = new YamlStream();

            yaml.Load(input);

            if (yaml.Documents.Count > 0)
            {
                var root = yaml.Documents[0].RootNode;
                if (root is YamlMappingNode collection)
                {
                    foreach (var entry in collection.Children)
                    {
                        if (entry.Key is YamlScalarNode node)
                        {
                            retVal.Add(node.Value, GetYamlNodeValues(entry.Value));
                        }
                    }
                }
            }
            return retVal;
        }

        private static IEnumerable<string> GetYamlNodeValues(YamlNode value)
        {
            var retVal = new List<string>();

            if (value is YamlSequenceNode list)
            {
                retVal.AddRange(list.Children.OfType<YamlScalarNode>().Select(node => node.Value));
            }
            else
            {
                retVal.Add(value.ToString());
            }

            return retVal;
        }
    }
}
