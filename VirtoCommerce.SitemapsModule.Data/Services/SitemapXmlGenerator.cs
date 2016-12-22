using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Models.Xml;
using VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public class SitemapXmlGenerator : ISitemapXmlGenerator
    {
        public SitemapXmlGenerator(
            ISitemapService sitemapService,
            ISitemapItemService sitemapItemService,
            ISitemapUrlBuilder sitemapUrlBuilder,
            ISitemapItemRecordProvider[] sitemapItemRecordProviders,
            IStoreService storeService,
            ISettingsManager settingsManager)
        {
            SitemapService = sitemapService;
            SitemapItemService = sitemapItemService;
            SitemapUrlBuilder = sitemapUrlBuilder;
            SitemapItemRecordProviders = sitemapItemRecordProviders;
            StoreService = storeService;
            SettingsManager = settingsManager;
        }

        protected ISitemapService SitemapService { get; private set; }
        protected ISitemapItemService SitemapItemService { get; private set; }
        protected ISitemapUrlBuilder SitemapUrlBuilder { get; private set; }
        protected ISitemapItemRecordProvider[] SitemapItemRecordProviders { get; private set; }
        protected IStoreService StoreService { get; private set; }
        protected ISettingsManager SettingsManager { get; private set; }

        public virtual ICollection<string> GetSitemapUrls(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                throw new ArgumentException("storeId");
            }

            var sitemapUrls = new List<string>();

            var store = StoreService.GetById(storeId);
            if (store != null)
            {
                var recordsLimitPerFile = SettingsManager.GetValue("Sitemap.RecordsLimitPerFile", 10000);
                var filenameSeparator = SettingsManager.GetValue("Sitemap.FilenameSeparator", "--");

                var sitemaps = GetSitemaps(store, recordsLimitPerFile);
                foreach (var sitemap in sitemaps)
                {
                    sitemap.Store = store;
                    sitemap.Items = GetFormalSitemapItems(sitemap.Id, recordsLimitPerFile);
                    var sitemapItemRecords = GetSitemapItemRecords(sitemap);
                    var sitemapPartialUrls = GetSitemapPartialUrls(sitemap, sitemapItemRecords.Count(), recordsLimitPerFile, filenameSeparator);
                    sitemapUrls.AddRange(sitemapPartialUrls);
                }
            }

            return sitemapUrls;
        }

        public virtual Stream GenerateSitemapXml(string storeId, string baseUrl, string sitemapUrl)
        {
            var stream = new MemoryStream();

            var recordsLimitPerFile = SettingsManager.GetValue("Sitemap.RecordsLimitPerFile", 10000);
            var filenameSeparator = SettingsManager.GetValue("Sitemap.FilenameSeparator", "--");

            XmlSerializer xmlSerializer = null;
            var xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add("", "http://www.sitemaps.org/schemas/sitemap/0.9");

            var sitemapFilename = GetSitemapFilenameFromUrl(sitemapUrl, filenameSeparator);
            if (sitemapFilename.EqualsInvariant("sitemap.xml"))
            {
                var sitemapUrls = GetSitemapUrls(storeId);
                var sitemapIndexXmlRecord = new SitemapIndexXmlRecord
                {
                    Sitemaps = sitemapUrls.Select(u => new SitemapIndexItemXmlRecord
                    {
                        ModifiedDate = DateTime.UtcNow,
                        Url = SitemapUrlBuilder.CreateAbsoluteUrl(u, baseUrl)
                    }).ToList()
                };

                xmlSerializer = new XmlSerializer(sitemapIndexXmlRecord.GetType());
                xmlSerializer.Serialize(stream, sitemapIndexXmlRecord, xmlNamespaces);
            }
            else
            {
                int partNumber = 0;
                var sitemapPart = sitemapFilename.Replace(".xml", "").Split(new[] { filenameSeparator }, StringSplitOptions.None).LastOrDefault();
                int.TryParse(sitemapPart, out partNumber);
                var sitemapSearchResult = SitemapService.Search(new SitemapSearchCriteria { Filename = sitemapFilename, StoreId = storeId, Skip = 0, Take = 1 });
                var sitemap = sitemapSearchResult.Results.FirstOrDefault();
                if (sitemap != null)
                {
                    sitemap.Store = StoreService.GetById(storeId);
                    sitemap.Items = GetFormalSitemapItems(sitemap.Id, recordsLimitPerFile);
                    sitemap.BaseUrl = baseUrl;
                    var sitemapItemRecords = GetSitemapItemRecords(sitemap);
                    if (partNumber > 0)
                    {
                        sitemapItemRecords = sitemapItemRecords.Skip((partNumber - 1) * recordsLimitPerFile).Take(recordsLimitPerFile);
                    }

                    var sitemapRecord = new SitemapXmlRecord
                    {
                        Items = sitemapItemRecords.Select(i => AbstractTypeFactory<SitemapItemXmlRecord>.TryCreateInstance().ToXmlModel(i)).ToList()
                    };

                    if (sitemapRecord.Items.Count > 0)
                    {
                        xmlSerializer = new XmlSerializer(sitemapRecord.GetType());
                        xmlSerializer.Serialize(stream, sitemapRecord, xmlNamespaces);
                    }
                }
            }

            stream.Position = 0;

            return stream;
        }

        private string GetSitemapFilenameFromUrl(string url, string filenameSeparator)
        {
            string filename = null;

            var urlParts = url.Split('/').ToList();
            var urlLastPart = urlParts.LastOrDefault();
            urlParts.Remove(urlLastPart);
            if (!string.IsNullOrEmpty(urlLastPart))
            {
                var filenameParts = urlLastPart.Split(new[] { filenameSeparator }, StringSplitOptions.None);
                filename = filenameParts.FirstOrDefault();
                if (filenameParts.Length > 0)
                {
                    filename = string.Format("{0}.xml", filenameParts.First().Replace(".xml", ""));
                }
            }
            urlParts.Add(filename);

            return string.Join("/", urlParts);
        }

        private ICollection<string> GetSitemapPartialUrls(Sitemap sitemap, int actualSitemapItemsCount, int recordsLimitPerFile, string filenameSeparator)
        {
            var sitemapUrls = new List<string>();

            var partsCount = actualSitemapItemsCount / recordsLimitPerFile + 1;
            for (var i = 1; i <= partsCount; i++)
            {
                var url = partsCount > 1 ? string.Format("{0}{1}{2}.xml", sitemap.Filename.Replace(".xml", ""), filenameSeparator, i) : sitemap.Filename;
                sitemapUrls.Add(url);
            }

            return sitemapUrls;
        }

        private ICollection<Sitemap> GetSitemaps(Store store, int recordsLimitPerFile)
        {
            var sitemaps = new List<Sitemap>();

            var sitemapSearchCriteria = new SitemapSearchCriteria
            {
                StoreId = store.Id,
                Skip = 0,
                Take = recordsLimitPerFile
            };
            var sitemapSearchResult = SitemapService.Search(sitemapSearchCriteria);
            var partsCount = sitemapSearchResult.TotalCount / recordsLimitPerFile + 1;
            for (var i = 1; i <= partsCount; i++)
            {
                sitemaps.AddRange(sitemapSearchResult.Results);
                if (partsCount > 1)
                {
                    sitemapSearchCriteria.Skip = recordsLimitPerFile * i;
                    sitemapSearchResult = SitemapService.Search(sitemapSearchCriteria);
                }
            }

            return sitemaps;
        }

        private ICollection<SitemapItem> GetFormalSitemapItems(string sitemapId, int recordsLimitPerFile)
        {
            var formalSitemapItems = new List<SitemapItem>();

            var sitemapItemSearchCriteria = new SitemapItemSearchCriteria
            {
                SitemapId = sitemapId,
                Skip = 0,
                Take = recordsLimitPerFile
            };
            var sitemapItemSearchResult = SitemapItemService.Search(sitemapItemSearchCriteria);
            var partsCount = sitemapItemSearchResult.TotalCount / recordsLimitPerFile + 1;
            for (var i = 1; i <= partsCount; i++)
            {
                formalSitemapItems.AddRange(sitemapItemSearchResult.Results);
                if (partsCount > 1)
                {
                    sitemapItemSearchCriteria.Skip = recordsLimitPerFile * i;
                    sitemapItemSearchResult = SitemapItemService.Search(sitemapItemSearchCriteria);
                }
            }

            return formalSitemapItems;
        }

        private IEnumerable<SitemapItemRecord> GetSitemapItemRecords(Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var cataloSitemapItemProvider = SitemapItemRecordProviders.OfType<CatalogSitemapItemRecordProvider>().FirstOrDefault();
            if (cataloSitemapItemProvider != null)
            {
                var catalogSitemapItemRecords = cataloSitemapItemProvider.GetSitemapItemRecords(sitemap);
                sitemapItemRecords.AddRange(catalogSitemapItemRecords);
            }

            var vendorSitemapItemRecordProvider = SitemapItemRecordProviders.OfType<VendorSitemapItemRecordProvider>().FirstOrDefault();
            if (vendorSitemapItemRecordProvider != null)
            {
                var vendorSitemapItemRecords = vendorSitemapItemRecordProvider.GetSitemapItemRecords(sitemap);
                sitemapItemRecords.AddRange(vendorSitemapItemRecords);
            }

            var customSitemapItemRecordProvider = SitemapItemRecordProviders.OfType<CustomSitemapItemRecordProvider>().FirstOrDefault();
            if (customSitemapItemRecordProvider != null)
            {
                var customSitemapItemRecords = customSitemapItemRecordProvider.GetSitemapItemRecords(sitemap);
                sitemapItemRecords.AddRange(customSitemapItemRecords);
            }

            return sitemapItemRecords.GroupBy(m => m.Url).Select(i => i.First());
        }
    }
}