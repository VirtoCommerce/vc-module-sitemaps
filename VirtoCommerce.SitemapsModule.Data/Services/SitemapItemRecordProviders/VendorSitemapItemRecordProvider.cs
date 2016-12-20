using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Domain.Customer.Services;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class VendorSitemapItemRecordProvider : ISitemapItemRecordProvider
    {
        public VendorSitemapItemRecordProvider(IMemberService memberService, ISitemapItemRecordBuilder sitemapItemRecordBuilder)
        {
            MemberService = memberService;
            SitemapItemRecordBuilder = sitemapItemRecordBuilder;
        }

        protected IMemberService MemberService { get; private set; }
        protected ISitemapItemRecordBuilder SitemapItemRecordBuilder { get; private set; }

        public virtual ICollection<SitemapItemRecord> GetSitemapItemRecords(Store store, Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var vendorSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Custom));
            var vendorIds = vendorSitemapItems.Select(si => si.ObjectId).ToArray();
            var members = MemberService.GetByIds(vendorIds);
            foreach (var member in members)
            {
                var vendor = member as Vendor;
                if (vendor != null)
                {
                    var vendorSitemapItemRecords = SitemapItemRecordBuilder.CreateSitemapItemRecords(store, sitemap.UrlTemplate, SitemapItemTypes.Vendor, vendor);
                    sitemapItemRecords.AddRange(vendorSitemapItemRecords);
                }
            }

            return sitemapItemRecords;
        }
    }
}