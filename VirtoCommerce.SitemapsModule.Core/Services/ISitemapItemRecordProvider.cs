using System.Collections.Generic;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapItemRecordProvider
    {
        ICollection<SitemapItemRecord> GetSitemapItemRecords(Store store, Sitemap sitemap);
    }
}