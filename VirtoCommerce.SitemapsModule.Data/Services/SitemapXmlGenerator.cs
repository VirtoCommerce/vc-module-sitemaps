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
using VirtoCommerce.SitemapsModule.Data.Models;
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
            ISettingsManager settingsManager)
        {
            SitemapService = sitemapService;
            SitemapItemService = sitemapItemService;
            SitemapUrlBuilder = sitemapUrlBuilder;
            SitemapItemRecordProviders = sitemapItemRecordProviders;
            SettingsManager = settingsManager;
        }

        protected ISitemapService SitemapService { get; private set; }
        protected ISitemapItemService SitemapItemService { get; private set; }
        protected ISitemapUrlBuilder SitemapUrlBuilder { get; private set; }
        protected ISitemapItemRecordProvider[] SitemapItemRecordProviders { get; private set; }
        protected ISettingsManager SettingsManager { get; private set; }

        public virtual ICollection<string> GetSitemapUrls(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                throw new ArgumentException("storeId");
            }

            var sitemapUrls = new List<string>();


            var sitemaps = LoadAllStoreSitemaps(storeId, "");
            foreach (var sitemap in sitemaps)
            {
                sitemapUrls.AddRange(sitemap.PagedLocations);
            }

            return sitemapUrls;
        }

        public virtual Stream GenerateSitemapXml(string storeId, string baseUrl, string sitemapUrl)
        {
            var stream = new MemoryStream();

            var filenameSeparator = SettingsManager.GetValue("Sitemap.FilenameSeparator", "--");
            var recordsLimitPerFile = SettingsManager.GetValue("Sitemap.RecordsLimitPerFile", 10000);

            XmlSerializer xmlSerializer = null;
            var xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add("", "http://www.sitemaps.org/schemas/sitemap/0.9");
       
            var sitemapLocation = SitemapLocation.Parse(sitemapUrl, filenameSeparator);

            if (sitemapLocation.Location.EqualsInvariant("sitemap.xml"))
            {
                var allStoreSitemaps = LoadAllStoreSitemaps(storeId, baseUrl);
                var sitemapIndexXmlRecord = new SitemapIndexXmlRecord();
                foreach(var sitemap in allStoreSitemaps)
                {
                    var xmlSiteMapRecords = sitemap.PagedLocations.Select(location => new SitemapIndexItemXmlRecord
                    {
                        ModifiedDate = sitemap.Items.Select(x => x.ModifiedDate).OrderByDescending(x => x).FirstOrDefault(),
                        Url = SitemapUrlBuilder.CreateAbsoluteUrl(location, baseUrl)
                    }).ToList();
                    sitemapIndexXmlRecord.Sitemaps.AddRange(xmlSiteMapRecords);
                }             
                xmlSerializer = new XmlSerializer(sitemapIndexXmlRecord.GetType());
                xmlSerializer.Serialize(stream, sitemapIndexXmlRecord, xmlNamespaces);
            }
            else
            {
                var sitemapSearchResult = SitemapService.Search(new SitemapSearchCriteria { Location = sitemapLocation.Location, StoreId = storeId, Skip = 0, Take = 1 });
                var sitemap = sitemapSearchResult.Results.FirstOrDefault();
                if (sitemap != null)
                {
                    LoadSitemapRecords(sitemap, baseUrl);
                    var distinctRecords = sitemap.Items.SelectMany(x => x.ItemsRecords).GroupBy(x => x.Url).Select(x => x.FirstOrDefault());
                    var sitemapItemRecords = distinctRecords.Skip((sitemapLocation.PageNumber - 1) * recordsLimitPerFile).Take(recordsLimitPerFile).ToArray();
                    var sitemapRecord = new SitemapXmlRecord
                    {
                        Items = sitemapItemRecords.Select(i => new SitemapItemXmlRecord().ToXmlModel(i)).ToList()
                    };
                    if (sitemapRecord.Items.Count > 0)
                    {
                        xmlSerializer = new XmlSerializer(sitemapRecord.GetType());
                        xmlSerializer.Serialize(stream, sitemapRecord, xmlNamespaces);
                    }
                }
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
        
        private ICollection<Sitemap> LoadAllStoreSitemaps(string storeId, string baseUrl)
        {
            var result = new List<Sitemap>();          
            var sitemapSearchCriteria = new SitemapSearchCriteria
            {
                StoreId = storeId,
                Skip = 0,
                Take = int.MaxValue
            };
            var sitemapSearchResult = SitemapService.Search(sitemapSearchCriteria);
            foreach(var sitemap in sitemapSearchResult.Results)
            {              
                LoadSitemapRecords(sitemap, baseUrl);
                result.Add(sitemap);
            }
            return result;
        }

        private void LoadSitemapRecords(Sitemap sitemap, string baseUrl)
        {
            var recordsLimitPerFile = SettingsManager.GetValue("Sitemap.RecordsLimitPerFile", 10000);
            var filenameSeparator = SettingsManager.GetValue("Sitemap.FilenameSeparator", "--");

            var sitemapItemSearchCriteria = new SitemapItemSearchCriteria
            {
                SitemapId = sitemap.Id,
                Skip = 0,
                Take = int.MaxValue
            };
            sitemap.Items = SitemapItemService.Search(sitemapItemSearchCriteria).Results;
            foreach(var recordProvider in SitemapItemRecordProviders)
            {
                recordProvider.LoadSitemapItemRecords(sitemap, baseUrl);
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