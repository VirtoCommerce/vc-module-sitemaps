using System.Collections.Generic;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapItemRecordProvider
    {
        ICollection<SitemapItemRecord> GetSitemapItemRecords(Sitemap sitemap);
    }
}