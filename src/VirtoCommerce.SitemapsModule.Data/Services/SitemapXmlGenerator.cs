using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Models.Search;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Models.Xml;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapXmlGenerator : ISitemapXmlGenerator
    {
        private readonly ISitemapSearchService _sitemapSearchService;
        private readonly ISitemapItemSearchService _sitemapItemSearchService;
        private readonly ISitemapUrlBuilder _sitemapUrlBuilder;
        private readonly IEnumerable<ISitemapItemRecordProvider> _sitemapItemRecordProviders;
        private readonly ISettingsManager _settingsManager;
        private readonly ILogger _logger;
        private readonly IStoreService _storeService;

        public SitemapXmlGenerator(
            ISitemapSearchService sitemapSearchService,
            ISitemapItemSearchService sitemapItemSearchService,
            ISitemapUrlBuilder sitemapUrlBuilder,
            IEnumerable<ISitemapItemRecordProvider> sitemapItemRecordProviders,
            ISettingsManager settingsManager,
            ILogger<SitemapXmlGenerator> logger,
            IStoreService storeService)
        {
            _sitemapSearchService = sitemapSearchService;
            _sitemapItemSearchService = sitemapItemSearchService;
            _sitemapUrlBuilder = sitemapUrlBuilder;
            _sitemapItemRecordProviders = sitemapItemRecordProviders;
            _settingsManager = settingsManager;
            _logger = logger;
            _storeService = storeService;
        }

        public virtual async Task<ICollection<string>> GetSitemapUrlsAsync(string storeId, string baseUrl)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                throw new ArgumentException(nameof(storeId));
            }

            var sitemapUrls = new List<string>();
            var store = await _storeService.GetByIdAsync(storeId, StoreResponseGroup.StoreInfo.ToString());

            var sitemaps = await LoadStoreSitemaps(store, baseUrl);
            foreach (var sitemap in sitemaps)
            {
                sitemapUrls.AddRange(sitemap.PagedLocations);
            }

            return sitemapUrls;
        }

        public virtual async Task<Stream> GenerateSitemapXmlAsync(string storeId, string baseUrl, string sitemapUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var stream = new MemoryStream();

            var filenameSeparator = await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.FilenameSeparator);
            var recordsLimitPerFile = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.RecordsLimitPerFile);

            var xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add("xhtml", "https://www.w3.org/1999/xhtml");
            xmlNamespaces.Add("image", "http://www.google.com/schemas/sitemap-image/1.1");

            var sitemapLocation = SitemapLocation.Parse(sitemapUrl, filenameSeparator);
            var store = await _storeService.GetByIdAsync(storeId, StoreResponseGroup.StoreInfo.ToString());
            if (sitemapLocation.Location.EqualsInvariant(ModuleConstants.SitemapFileName))
            {
                progressCallback?.Invoke(new ExportImportProgressInfo
                {
                    Description = $"Creating {ModuleConstants.SitemapFileName}..."
                });

                var storeSitemaps = await LoadStoreSitemaps(store, baseUrl);

                var sitemapIndexXmlRecord = new SitemapIndexXmlRecord();

                foreach (var sitemap in storeSitemaps)
                {
                    var xmlSiteMapRecords = sitemap.PagedLocations.Select(location => new SitemapIndexItemXmlRecord
                    {
                        Url = _sitemapUrlBuilder.BuildStoreUrl(store, store.DefaultLanguage, location, baseUrl),
                    }).ToList();

                    sitemapIndexXmlRecord.Sitemaps.AddRange(xmlSiteMapRecords);
                }

                var xmlSerializer = new XmlSerializer(sitemapIndexXmlRecord.GetType());
                xmlSerializer.Serialize(stream, sitemapIndexXmlRecord, xmlNamespaces);
            }
            else
            {
                var sitemapSearchResult = await _sitemapSearchService.SearchAsync(new SitemapSearchCriteria { Location = sitemapLocation.Location, StoreId = storeId, Skip = 0, Take = 1 });
                var sitemap = sitemapSearchResult.Results.FirstOrDefault();
                if (sitemap != null)
                {
                    await LoadSitemapRecords(store, sitemap, baseUrl, progressCallback);

                    var requiredItems = new List<SitemapItemRecord>();

                    switch (sitemap.SitemapMode)
                    {
                        case SitemapContentMode.OnlyCategories:
                            requiredItems = sitemap.Items.SelectMany(x => x.ItemsRecords).Where(x => x.ObjectType == "category").ToList();
                            break;
                        case SitemapContentMode.OnlyProducts:
                            requiredItems = sitemap.Items.SelectMany(x => x.ItemsRecords).Where(x => x.ObjectType == "product").ToList();
                            break;
                        case SitemapContentMode.Full:
                        default:
                            requiredItems = sitemap.Items.SelectMany(x => x.ItemsRecords).ToList();
                            break;
                    }

                    var sitemapItemRecords = requiredItems.Skip((sitemapLocation.PageNumber - 1) * recordsLimitPerFile).Take(recordsLimitPerFile).ToArray();

                    var sitemapRecord = new SitemapXmlRecord
                    {
                        xmlns = xmlNamespaces,
                        Items = sitemapItemRecords.Select(i => new SitemapItemXmlRecord().ToXmlModel(i)).ToList()
                    };

                    if (sitemapRecord.Items.Count > 0)
                    {
                        var xmlSerializer = new XmlSerializer(sitemapRecord.GetType());
                        xmlSerializer.Serialize(stream, sitemapRecord, xmlNamespaces);
                    }
                }
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private async Task<ICollection<Sitemap>> LoadStoreSitemaps(Store store, string baseUrl)
        {
            var sitemapSearchCriteria = new SitemapSearchCriteria
            {
                StoreId = store.Id,
                Skip = 0,
                Take = int.MaxValue
            };

            var sitemapSearchResult = await _sitemapSearchService.SearchAsync(sitemapSearchCriteria);

            var sitemaps = new List<Sitemap>();
            foreach (var sitemap in sitemapSearchResult.Results)
            {
                await LoadSitemapRecords(store, sitemap, baseUrl);

                sitemaps.Add(sitemap);
            }

            return sitemaps;
        }

        private async Task LoadSitemapRecords(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var recordsLimitPerFile = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.RecordsLimitPerFile);
            var filenameSeparator = await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.FilenameSeparator);

            var sitemapItemSearchCriteria = new SitemapItemSearchCriteria
            {
                SitemapId = sitemap.Id,
                Skip = 0,
                Take = int.MaxValue
            };
            sitemap.Items = (await _sitemapItemSearchService.SearchAsync(sitemapItemSearchCriteria)).Results;

            foreach (var recordProvider in _sitemapItemRecordProviders)
            {
                //Log exceptions to prevent fail whole sitemap.xml generation
                try
                {
                    await recordProvider.LoadSitemapItemRecordsAsync(store, sitemap, baseUrl, progressCallback);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to load sitemap item records for store #{store.Id}, sitemap #{sitemap.Id} and baseURL '{baseUrl}'");
                }
            }
            sitemap.PagedLocations.Clear();
            var totalRecordsCount = sitemap.Items.SelectMany(x => x.ItemsRecords).GroupBy(x => x.Url).Count();
            var pagesCount = totalRecordsCount > 0 ? (int)Math.Ceiling(totalRecordsCount / (double)recordsLimitPerFile) : 0;
            for (var i = 1; i <= pagesCount; i++)
            {
                sitemap.PagedLocations.Add(new SitemapLocation(sitemap.Location, i, filenameSeparator).ToString(pagesCount > 1));
            }
        }
    }
}
