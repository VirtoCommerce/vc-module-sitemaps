using System;
using System.Collections.Generic;
using System.IO;
using VirtoCommerce.Platform.Core.ExportImport;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public interface ISitemapXmlGenerator
    {
        ICollection<string> GetSitemapUrls(string storeId);

        Stream GenerateSitemapXml(string storeId, string baseUrl, string sitemapUrl, Action<ExportImportProgressInfo> progressCallback = null);
    }
}