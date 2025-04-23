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
    public class SitemapXmlGenerator : ISitemapXmlGenerator, ISitemapGenerator
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

            var files = await GetSitemapFilesAsync(storeId, baseUrl);

            return files[0].Records
                .Select(x => x.Url)
                .ToList();
        }

        public virtual async Task<Stream> GenerateSitemapXmlAsync(string storeId, string baseUrl, string sitemapUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var files = await GetSitemapFilesAsync(storeId, baseUrl, progressCallback);
            var file = files.FirstOrDefault(x => x.Name.EqualsIgnoreCase(sitemapUrl));

            var stream = new MemoryStream();

            if (file != null)
            {
                SaveSitemapFile(file, stream, progressCallback);
                stream.Seek(0, SeekOrigin.Begin);
            }

            return stream;
        }

        public virtual async Task<IList<SitemapFile>> GetSitemapFilesAsync(string storeId, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null)
        {
            var store = await _storeService.GetByIdAsync(storeId, nameof(StoreResponseGroup.StoreInfo));

            return await GetSitemapFilesAsync(store, baseUrl, progressCallback);
        }

        public virtual async Task<IList<SitemapFile>> GetSitemapFilesAsync(Store store, string baseUrl, Action<ExportImportProgressInfo> progressCallback)
        {
            var sitemapSearchCriteria = new SitemapSearchCriteria
            {
                StoreId = store.Id,
                Take = int.MaxValue,
            };

            var sitemaps = (await _sitemapSearchService.SearchAsync(sitemapSearchCriteria)).Results;
            var files = new List<SitemapFile>();

            foreach (var sitemap in sitemaps)
            {
                await LoadSitemapRecords(sitemap, store, baseUrl, progressCallback);
                await AddSitemapFiles(sitemap, files);
            }

            var indexRecords = files
                .Select(x => new SitemapRecord { Url = _sitemapUrlBuilder.BuildStoreUrl(store, store.DefaultLanguage, x.Name, baseUrl) })
                .ToList();

            files.Insert(0, new SitemapFile(ModuleConstants.SitemapFileName, indexRecords));

            return files;
        }

        public virtual void SaveSitemapFile(SitemapFile file, Stream stream, Action<ExportImportProgressInfo> progressCallback = null)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(stream);

            if (file.Records.Count == 0)
            {
                return;
            }

            progressCallback?.Invoke(new ExportImportProgressInfo
            {
                Description = $"Creating {file.Name}...",
            });

            object xmlRecord = file.Name.EqualsIgnoreCase(ModuleConstants.SitemapFileName)
                ? GenerateSitemapIndex(file)
                : GenerateUrlSet(file);

            if (xmlRecord != null)
            {
                SaveXml(xmlRecord, stream);
            }
        }


        private static SitemapIndexXmlRecord GenerateSitemapIndex(SitemapFile file)
        {
            return new SitemapIndexXmlRecord
            {
                Sitemaps = file.Records
                    .Select(x => new SitemapIndexItemXmlRecord { Url = x.Url })
                    .ToList(),
            };
        }

        private static SitemapXmlRecord GenerateUrlSet(SitemapFile file)
        {
            return new SitemapXmlRecord
            {
                Items = file.Records
                    .OfType<SitemapItemRecord>()
                    .Select(x => new SitemapItemXmlRecord().ToXmlModel(x))
                    .ToList(),
            };
        }

        private static void SaveXml(object xmlRecord, Stream stream)
        {
            var xmlSerializer = new XmlSerializer(xmlRecord.GetType());

            var xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add("xhtml", "https://www.w3.org/1999/xhtml");
            xmlNamespaces.Add("image", "http://www.google.com/schemas/sitemap-image/1.1");

            xmlSerializer.Serialize(stream, xmlRecord, xmlNamespaces);
        }

        private async Task LoadSitemapRecords(Sitemap sitemap, Store store, string baseUrl, Action<ExportImportProgressInfo> progressCallback)
        {
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
        }

        private async Task AddSitemapFiles(Sitemap sitemap, List<SitemapFile> files)
        {
            var recordsLimitPerFile = await _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.RecordsLimitPerFile);
            var filenameSeparator = await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.FilenameSeparator);

            var allRecords = GetAllRecords(sitemap);
            var totalRecordsCount = allRecords.Count;
            var filesCount = totalRecordsCount > 0 ? (int)Math.Ceiling(totalRecordsCount / (double)recordsLimitPerFile) : 0;
            var addFileNumber = filesCount > 1;
            var fileNumber = 0;

            foreach (var records in allRecords.Paginate(recordsLimitPerFile))
            {
                fileNumber++;

                var fileName = addFileNumber
                    ? $"{sitemap.Location.Replace(".xml", string.Empty)}{filenameSeparator}{fileNumber}.xml"
                    : sitemap.Location;

                files.Add(new SitemapFile(fileName, records));
            }
        }

        private static List<SitemapRecord> GetAllRecords(Sitemap sitemap)
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
                .ToList<SitemapRecord>();
        }
    }
}
