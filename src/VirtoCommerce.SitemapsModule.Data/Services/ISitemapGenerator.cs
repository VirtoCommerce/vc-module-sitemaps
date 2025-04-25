using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Data.Services;

public interface ISitemapGenerator
{
    Task<IList<SitemapFile>> GetSitemapFilesAsync(string storeId, string baseUrl, Action<ExportImportProgressInfo> progressCallback = null);

    void SaveSitemapFile(SitemapFile file, Stream stream, Action<ExportImportProgressInfo> progressCallback = null);
}
