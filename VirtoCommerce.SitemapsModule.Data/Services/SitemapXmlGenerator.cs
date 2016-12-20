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

        public Stream GenerateSitemapXml(string storeId, SitemapIndexXmlRecord sitemapSchema, string sitemapFilename)
        {
            var stream = new MemoryStream();

            XmlSerializer xmlSerializer = null;
            var xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add("", "http://www.sitemaps.org/schemas/sitemap/0.9");

            var store = StoreService.GetById(storeId);
            if (store != null)
            {
                if (sitemapFilename.EqualsInvariant("sitemap.xml"))
                {
                    xmlSerializer = new XmlSerializer(sitemapSchema.GetType());
                    xmlSerializer.Serialize(stream, sitemapSchema, xmlNamespaces);
                }
                else
                {
                    var recordsLimitPerFile = SettingsManager.GetValue("Sitemap.RecordsLimitPerFile", 10000);
                    var filenameSeparator = SettingsManager.GetValue("Sitemap.FilenameSeparator", "--");

                    int partNumber = 0;
                    var sitemapPart = sitemapFilename.Replace(".xml", "").Split(new[] { filenameSeparator }, StringSplitOptions.None).LastOrDefault();
                    int.TryParse(sitemapPart, out partNumber);
                    var sitemapIndexItemRecord = sitemapSchema.Sitemaps.FirstOrDefault(s => s.Filename == sitemapFilename);
                    if (sitemapIndexItemRecord != null)
                    {
                        var sitemap = SitemapService.GetById(sitemapIndexItemRecord.SitemapId);
                        if (sitemap != null)
                        {
                            sitemap.Items = GetFormalSitemapItems(sitemapIndexItemRecord.SitemapId, recordsLimitPerFile);
                            var sitemapItemRecords = GetSitemapItemRecords(store, sitemap);
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
                }
            }

            stream.Position = 0;

            return stream;
        }

        public SitemapIndexXmlRecord GetSitemapSchema(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                throw new ArgumentException("storeId");
            }

            var sitemapSchema = new SitemapIndexXmlRecord();

            var store = StoreService.GetById(storeId);
            if (store != null)
            {
                var recordsLimitPerFile = SettingsManager.GetValue("Sitemap.RecordsLimitPerFile", 10000);
                var filenameSeparator = SettingsManager.GetValue("Sitemap.FilenameSeparator", "--");
                var sitemaps = GetSitemaps(store, recordsLimitPerFile);
                foreach (var sitemap in sitemaps)
                {
                    sitemap.Items = GetFormalSitemapItems(sitemap.Id, recordsLimitPerFile);

                    if (sitemap.Items.Count > 0)
                    {
                        var sitemapIndexItemRecords = GetSitemapIndexItemRecords(store, sitemap, recordsLimitPerFile, filenameSeparator);
                        sitemapSchema.Sitemaps.AddRange(sitemapIndexItemRecords);
                    }
                }
            }

            return sitemapSchema;
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

        private IEnumerable<SitemapItemRecord> GetSitemapItemRecords(Store store, Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var cataloSitemapItemProvider = SitemapItemRecordProviders.OfType<CatalogSitemapItemRecordProvider>().FirstOrDefault();
            if (cataloSitemapItemProvider != null)
            {
                var catalogSitemapItemRecords = cataloSitemapItemProvider.GetSitemapItemRecords(store, sitemap);
                sitemapItemRecords.AddRange(catalogSitemapItemRecords);
            }

            var vendorSitemapItemRecordProvider = SitemapItemRecordProviders.OfType<VendorSitemapItemRecordProvider>().FirstOrDefault();
            if (vendorSitemapItemRecordProvider != null)
            {
                var vendorSitemapItemRecords = vendorSitemapItemRecordProvider.GetSitemapItemRecords(store, sitemap);
                sitemapItemRecords.AddRange(vendorSitemapItemRecords);
            }

            var customSitemapItemRecordProvider = SitemapItemRecordProviders.OfType<CustomSitemapItemRecordProvider>().FirstOrDefault();
            if (customSitemapItemRecordProvider != null)
            {
                var customSitemapItemRecords = customSitemapItemRecordProvider.GetSitemapItemRecords(store, sitemap);
                sitemapItemRecords.AddRange(customSitemapItemRecords);
            }

            return sitemapItemRecords.GroupBy(m => m.Url).Select(i => i.First());
        }

        private ICollection<SitemapIndexItemXmlRecord> GetSitemapIndexItemRecords(Store store, Sitemap sitemap, int recordsLimitPerFile, string filenameSeparator)
        {
            var sitemapRecords = new List<SitemapIndexItemXmlRecord>();

            var sitemapItemRecords = GetSitemapItemRecords(store, sitemap);
            var partsCount = sitemapItemRecords.Count() / recordsLimitPerFile + 1;
            for (var i = 1; i <= partsCount; i++)
            {
                var filename = partsCount > 1 ?
                    string.Format("{0}{1}{2}.xml", sitemap.Filename.Replace(".xml", ""), filenameSeparator, i) :
                    sitemap.Filename;
                sitemapRecords.Add(new SitemapIndexItemXmlRecord
                {
                    Filename = filename,
                    ModifiedDate = DateTime.UtcNow,
                    SitemapId = sitemap.Id,
                    Url = SitemapUrlBuilder.CreateAbsoluteUrl(store, sitemap.UrlTemplate, null, filename)
                });
            }

            return sitemapRecords;
        }
    }
}