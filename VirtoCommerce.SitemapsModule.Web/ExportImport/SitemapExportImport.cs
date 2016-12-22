using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Web.ExportImport
{
    public sealed class BackupObject
    {
        public BackupObject()
        {
            Sitemaps = new List<Sitemap>();
        }

        public ICollection<Sitemap> Sitemaps { get; set; }
        public ICollection<SitemapItem> SitemapItems { get; set; }
    }

    public sealed class SitemapExportImport
    {
        public SitemapExportImport(ISitemapService sitemapService, ISitemapItemService sitemapItemService)
        {
            _sitemapService = sitemapService;
            _sitemapItemService = sitemapItemService;
        }

        private readonly ISitemapService _sitemapService;
        private readonly ISitemapItemService _sitemapItemService;

        public void DoExport(Stream backupStream, Action<ExportImportProgressInfo> progressCallback)
        {
            var backupObject = GetBackupObject(progressCallback);
            backupObject.SerializeJson(backupStream);
        }

        public void DoImport(Stream backupStream, Action<ExportImportProgressInfo> progressCallback)
        {
            var backupObject = backupStream.DeserializeJson<BackupObject>();
            var progressInfo = new ExportImportProgressInfo();

            progressInfo.Description = string.Format("Sitemaps importing...");
            progressCallback(progressInfo);
            _sitemapService.SaveChanges(backupObject.Sitemaps.ToArray());

            progressInfo.Description = string.Format("Sitemaps items importing...");
            progressCallback(progressInfo);
            _sitemapItemService.SaveChanges(backupObject.SitemapItems.ToArray());

        }

        private BackupObject GetBackupObject(Action<ExportImportProgressInfo> progressCallback)
        {
            var backupObject = new BackupObject();
            var progressInfo = new ExportImportProgressInfo();
         

            progressInfo.Description = string.Format("Sitemaps loading...");
            progressCallback(progressInfo);
            //Load sitemaps
            var sitemapSearchCriteria = new SitemapSearchCriteria {
                Skip = 0,
                Take = int.MaxValue
            };
            var sitemapSearchResult = _sitemapService.Search(sitemapSearchCriteria);
            backupObject.Sitemaps = sitemapSearchResult.Results;

            progressInfo.Description = string.Format("Sitemaps items loading...");
            progressCallback(progressInfo);
            var sitemapItemsSearchCriteria = new SitemapItemSearchCriteria
            {
                Skip = 0,
                Take = int.MaxValue
            };
            var sitemapItemsSearchResult = _sitemapItemService.Search(sitemapItemsSearchCriteria);
            backupObject.SitemapItems = sitemapItemsSearchResult.Results;

            return backupObject;
        } 
      
    }
}