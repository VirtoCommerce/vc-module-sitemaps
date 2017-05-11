using System;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapItemRecordProvider
    {
        void LoadSitemapItemRecords(Store store, Sitemap sitemap, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null);
    }
}