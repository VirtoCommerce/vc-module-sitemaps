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

            var pageSize = 20;
            var sitemaps = backupObject.Sitemaps.Skip(0).Take(pageSize);
            var partsCount = backupObject.Sitemaps.Count / pageSize + 1;

            for (var i = 1; i <= partsCount; i++)
            {
                _sitemapService.SaveChanges(sitemaps.ToArray());
                foreach (var sitemap in sitemaps)
                {
                    _sitemapItemService.Add(sitemap.Id, sitemap.Items.ToArray());
                }

                progressInfo.Description = string.Format("{0} of {1} sitemaps are exported", Math.Min((i - 1) * pageSize + pageSize, backupObject.Sitemaps.Count), backupObject.Sitemaps.Count);
                progressCallback(progressInfo);

                if (partsCount > 1)
                {
                    sitemaps = backupObject.Sitemaps.Skip(pageSize * i).Take(pageSize);
                }
            }
        }

        private BackupObject GetBackupObject(Action<ExportImportProgressInfo> progressCallback)
        {
            var backupObject = new BackupObject();
            var progressInfo = new ExportImportProgressInfo();
            var pageSize = 20;

            var sitemapSearchCriteria = new SitemapSearchCriteria {
                Skip = 0,
                Take = pageSize
            };
            var sitemapSearchResult = _sitemapService.Search(sitemapSearchCriteria);
            var partsCount = sitemapSearchResult.TotalCount / pageSize + 1;
            for (var i = 1; i <= partsCount; i++)
            {
                progressInfo.Description = string.Format("{0} of {1} sitemaps loading", Math.Min(pageSize * i + pageSize, sitemapSearchResult.TotalCount), sitemapSearchResult.TotalCount);
                progressCallback(progressInfo);

                foreach (var sitemap in sitemapSearchResult.Results)
                {
                    sitemap.Items = GetSitemapItems(sitemap.Id, pageSize);
                    backupObject.Sitemaps.Add(sitemap);
                }

                if (partsCount > 1)
                {
                    sitemapSearchCriteria.Skip = pageSize * i;
                    sitemapSearchResult = _sitemapService.Search(sitemapSearchCriteria);
                }
            }

            return backupObject;
        }

        private ICollection<SitemapItem> GetSitemapItems(string sitemapId, int pageSize)
        {
            var sitemapItems = new List<SitemapItem>();

            var sitemapItemSearchCriteria = new SitemapItemSearchCriteria
            {
                SitemapId = sitemapId,
                Skip = 0,
                Take = pageSize
            };
            var sitemapItemSearchResult = _sitemapItemService.Search(sitemapItemSearchCriteria);
            var partsCount = sitemapItemSearchResult.TotalCount / pageSize + 1;
            for (var i = 1; i <= partsCount; i++)
            {
                sitemapItems.AddRange(sitemapItemSearchResult.Results);
                if (partsCount > 1)
                {
                    sitemapItemSearchCriteria.Skip = pageSize * i;
                    sitemapItemSearchResult = _sitemapItemService.Search(sitemapItemSearchCriteria);
                }
            }

            return sitemapItems;
        }
    }
}