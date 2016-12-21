using System.Collections.Generic;
using System.IO;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public interface ISitemapXmlGenerator
    {
        ICollection<string> GetSitemapUrls(string storeId);

        Stream GenerateSitemapXml(string storeId, string sitemapUrl);
    }
}