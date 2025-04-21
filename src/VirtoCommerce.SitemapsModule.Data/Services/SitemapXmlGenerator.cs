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
            ArgumentException.ThrowIfNullOrEmpty(storeId);

            var store = await _storeService.GetByIdAsync(storeId, nameof(StoreResponseGroup.StoreInfo));
            var sitemaps = await LoadStoreSitemaps(store, baseUrl);

            return sitemaps
                .SelectMany(x => x.PagedLocations)
                .ToList();
        }

        public virtual async Task<Stream> GenerateSitemapXmlAsync(string storeId, string baseUrl, string sitemapUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            object xmlRecord = null;

            var filenameSeparator = await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.FilenameSeparator);
            var recordsLimitPerFile = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.RecordsLimitPerFile);

            var sitemapLocation = SitemapLocation.Parse(sitemapUrl, filenameSeparator);
            var store = await _storeService.GetByIdAsync(storeId, nameof(StoreResponseGroup.StoreInfo));

            if (sitemapLocation.Location.EqualsIgnoreCase(ModuleConstants.SitemapFileName))
            {
                progressCallback?.Invoke(new ExportImportProgressInfo
                {
                    Description = $"Creating {ModuleConstants.SitemapFileName}...",
                });

                var sitemaps = await LoadStoreSitemaps(store, baseUrl);

                xmlRecord = new SitemapIndexXmlRecord
                {
                    Sitemaps = sitemaps
                        .SelectMany(sitemap => sitemap.PagedLocations
                            .Select(location =>
                                new SitemapIndexItemXmlRecord
                                {
                                    Url = _sitemapUrlBuilder.BuildStoreUrl(store, store.DefaultLanguage, location, baseUrl),
                                }))
                        .ToList(),
                };
            }
            else
            {
                var sitemapSearchCriteria = new SitemapSearchCriteria
                {
                    StoreId = storeId,
                    Location = sitemapLocation.Location,
                    Take = 1,
                };

                var sitemap = (await _sitemapSearchService.SearchAsync(sitemapSearchCriteria)).Results.FirstOrDefault();

                if (sitemap != null)
                {
                    await LoadSitemapRecords(sitemap, store, baseUrl, progressCallback);

                    var sitemapRecord = new SitemapXmlRecord
                    {
                        Items = sitemap.AllRecords
                            .Skip((sitemapLocation.PageNumber - 1) * recordsLimitPerFile)
                            .Take(recordsLimitPerFile)
                            .Select(x => new SitemapItemXmlRecord().ToXmlModel(x))
                            .ToList(),
                    };

                    if (sitemapRecord.Items.Count > 0)
                    {
                        xmlRecord = sitemapRecord;
                    }
                }
            }

            var stream = new MemoryStream();

            if (xmlRecord != null)
            {
                SaveXml(xmlRecord, stream);
            }

            return stream;
        }


        private static void SaveXml(object xmlRecord, MemoryStream stream)
        {
            var xmlSerializer = new XmlSerializer(xmlRecord.GetType());

            var xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add("xhtml", "https://www.w3.org/1999/xhtml");
            xmlNamespaces.Add("image", "http://www.google.com/schemas/sitemap-image/1.1");

            xmlSerializer.Serialize(stream, xmlRecord, xmlNamespaces);
            stream.Seek(0, SeekOrigin.Begin);
        }

        private async Task<IList<Sitemap>> LoadStoreSitemaps(Store store, string baseUrl)
        {
            var sitemapSearchCriteria = new SitemapSearchCriteria
            {
                StoreId = store.Id,
                Take = int.MaxValue,
            };

            var sitemaps = (await _sitemapSearchService.SearchAsync(sitemapSearchCriteria)).Results;

            foreach (var sitemap in sitemaps)
            {
                await LoadSitemapRecords(sitemap, store, baseUrl);
            }

            return sitemaps;
        }

        private async Task LoadSitemapRecords(Sitemap sitemap, Store store, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var recordsLimitPerFile = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.RecordsLimitPerFile);
            var filenameSeparator = await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.FilenameSeparator);

            var sitemapItemSearchCriteria = new SitemapItemSearchCriteria
            {
                SitemapId = sitemap.Id,
                Take = int.MaxValue,
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
                    _logger.LogError(ex, "Failed to load sitemap item records for store #{StoreId}, sitemap #{SitemapId} and baseURL '{BaseUrl}'", store.Id, sitemap.Id, baseUrl);
                }
            }

            sitemap.AllRecords = GetAllRecords(sitemap);

            var totalRecordsCount = sitemap.AllRecords.Count;
            var pagesCount = totalRecordsCount > 0 ? (int)Math.Ceiling(totalRecordsCount / (double)recordsLimitPerFile) : 0;

            sitemap.PagedLocations.Clear();

            for (var pageNumber = 1; pageNumber <= pagesCount; pageNumber++)
            {
                sitemap.PagedLocations.Add(new SitemapLocation(sitemap.Location, pageNumber, filenameSeparator).ToString(pagesCount > 1));
            }
        }

        private static List<SitemapItemRecord> GetAllRecords(Sitemap sitemap)
        {
            var records = sitemap.Items.SelectMany(x => x.ItemsRecords);

            switch (sitemap.SitemapMode)
            {
                case SitemapContentMode.OnlyCategories:
                    records = records.Where(x => x.ObjectType == "category");
                    break;
                case SitemapContentMode.OnlyProducts:
                    records = records.Where(x => x.ObjectType == "product");
                    break;
                case SitemapContentMode.Full:
                default:
                    break;
            }

            return records
                .GroupBy(x => x.Url)
                .Select(g => g.First())
                .ToList();
        }
    }
}
