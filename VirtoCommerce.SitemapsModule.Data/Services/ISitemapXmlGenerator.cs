using System.IO;
using VirtoCommerce.SitemapsModule.Data.Models.Xml;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public interface ISitemapXmlGenerator
    {
        SitemapIndexRecord GetSitemapSchema(string storeId);

        Stream GenerateSitemapXml(string storeId, SitemapIndexRecord sitemapSchema, string sitemapFilename);
    }
}