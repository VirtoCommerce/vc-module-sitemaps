using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.ExportImport;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public interface ISitemapXmlGenerator
    {
        Task<ICollection<string>> GetSitemapUrlsAsync(string storeId, string baseUrl);

        Task<Stream> GenerateSitemapXmlAsync(string storeId, string baseUrl, string sitemapUrl, Action<ExportImportProgressInfo> progressCallback = null);
    }
}
