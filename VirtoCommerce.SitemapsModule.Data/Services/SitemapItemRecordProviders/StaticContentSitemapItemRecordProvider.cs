using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VirtoCommerce.ContentModule.Data.Services;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.Tools;
using YamlDotNet.RepresentationModel;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class StaticContentSitemapItemRecordProvider : SitemapItemRecordProviderBase, ISitemapItemRecordProvider
    {
        public StaticContentSitemapItemRecordProvider(
            IUrlBuilder urlBuilder,
            ISettingsManager settingsManager,
            Func<string, IContentBlobStorageProvider> contentStorageProviderFactory)
            : base(settingsManager, urlBuilder)
        {
            ContentStorageProviderFactory = contentStorageProviderFactory;
        }

        private readonly Func<string, IContentBlobStorageProvider> ContentStorageProviderFactory;
        private static readonly Regex _headerRegExp = new Regex(@"(?s:^---(.*?)---)");

        public virtual void LoadSitemapItemRecords(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var progressInfo = new ExportImportProgressInfo();

            var contentBasePath = string.Format("Pages/{0}", sitemap.StoreId);
            var storageProvider = ContentStorageProviderFactory(contentBasePath);
            var options = new SitemapItemOptions();
            var staticContentSitemapItems = sitemap.Items.Where(si => !string.IsNullOrEmpty(si.ObjectType) &&
                                                                      (si.ObjectType.EqualsInvariant(SitemapItemTypes.ContentItem) ||
                                                                       si.ObjectType.EqualsInvariant(SitemapItemTypes.Folder)));
            var staticContentItemsCount = staticContentSitemapItems.Count();
            var i = 0;
            foreach (var sitemapItem in staticContentSitemapItems)
            {
                var urls = new List<string>();
                if (sitemapItem.ObjectType.EqualsInvariant(SitemapItemTypes.Folder))
                {
                    var searchResult = storageProvider.Search(sitemapItem.UrlTemplate, null);
                    urls.AddRange(GetItemUrls(storageProvider, searchResult));
                }
                else
                {
                    var item = storageProvider.GetBlobInfo(sitemapItem.UrlTemplate);
                    if (item != null)
                    {
                        urls.Add(item.RelativeUrl);
                    }
                }
                foreach (var url in urls)
                {
                    using (var stream = storageProvider.OpenRead(url))
                    {
                        progressInfo.Description = string.Format("Generating sitemap items for static content: {0}...", i);


                        var content = stream.ReadToString();
                        var yamlHeader = ReadYamlHeader(content);
                        IEnumerable<string> permalinks = null;
                        yamlHeader.TryGetValue("permalink", out permalinks);
                        var frontMatterPermalink = new FrontMatterPermalink(url.Replace(".md", ""));
                        if (permalinks != null && permalinks.Any())
                        {
                            frontMatterPermalink = new FrontMatterPermalink(permalinks.FirstOrDefault());
                        }
                        sitemapItem.ItemsRecords.AddRange(GetSitemapItemRecords(store, options, frontMatterPermalink.ToUrl().TrimStart(new[] { '/' }), baseUrl));
                        i++;
                    }
                }
            }
        }

        private ICollection<string> GetItemUrls(IContentBlobStorageProvider storageProvider, BlobSearchResult searchResult)
        {
            var urls = new List<string>();

            foreach (var item in searchResult.Items)
            {
                urls.Add(item.RelativeUrl);
            }
            foreach (var folder in searchResult.Folders)
            {
                var folderSearchResult = storageProvider.Search(folder.RelativeUrl, null);
                urls.AddRange(GetItemUrls(storageProvider, folderSearchResult));
            }

            return urls;
        }

        private static IDictionary<string, IEnumerable<string>> ReadYamlHeader(string text)
        {
            var retVal = new Dictionary<string, IEnumerable<string>>();
            var headerMatches = _headerRegExp.Matches(text);
            if (headerMatches.Count == 0)
                return retVal;

            var input = new StringReader(headerMatches[0].Groups[1].Value);
            var yaml = new YamlStream();

            yaml.Load(input);

            if (yaml.Documents.Count > 0)
            {
                var root = yaml.Documents[0].RootNode;
                var collection = root as YamlMappingNode;
                if (collection != null)
                {
                    foreach (var entry in collection.Children)
                    {
                        var node = entry.Key as YamlScalarNode;
                        if (node != null)
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
            var list = value as YamlSequenceNode;

            if (list != null)
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