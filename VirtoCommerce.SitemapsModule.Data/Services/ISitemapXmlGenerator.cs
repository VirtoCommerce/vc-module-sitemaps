using System.IO;
using VirtoCommerce.SitemapsModule.Data.Models.Xml;

namespace VirtoCommerce.SitemapsModule.Data.Services
{
    public interface ISitemapXmlGenerator
    {
        SitemapIndexXmlRecord GetSitemapSchema(string storeId);

        Stream GenerateSitemapXml(string storeId, SitemapIndexXmlRecord sitemapSchema, string sitemapFilename);
    }
}