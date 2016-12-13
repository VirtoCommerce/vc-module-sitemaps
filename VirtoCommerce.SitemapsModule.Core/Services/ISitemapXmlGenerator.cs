using System.Collections.Generic;
using System.IO;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapXmlGenerator
    {
        ICollection<SitemapMapping> GetSitemapSchema(string storeId, bool includeItems = false);

        Stream GenerateSitemapXml(ICollection<SitemapMapping> sitemapMappings, string sitemapUrl);
    }
}