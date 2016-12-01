using System.IO;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapXmlGenerator
    {
        SitemapIndexXmlRecord GenerateSitemapIndex(string storeId, SitemapOptions options);

        SitemapXmlRecord GenerateSitemap(string storeId, string sitemapFilename, SitemapOptions options);
    }
}