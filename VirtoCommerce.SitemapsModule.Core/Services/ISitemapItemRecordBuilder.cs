using System.Collections.Generic;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapItemRecordBuilder
    {
        ICollection<SitemapItemRecord> CreateSitemapItemRecords(Store store, string urlTemplate, string sitemapItemType, ISeoSupport seoSupportItem = null);
    }
}