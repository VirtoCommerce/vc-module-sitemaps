using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Domain.Customer.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class VendorSitemapItemRecordProvider : SitemapItemRecordProviderBase, ISitemapItemRecordProvider
    {
        public VendorSitemapItemRecordProvider(
            ISitemapUrlBuilder sitemapUrlBuilder,
            ISettingsManager settingsManager,
            IMemberService memberService)
            : base(settingsManager, sitemapUrlBuilder)
        {
            MemberService = memberService;
        }

        protected IMemberService MemberService { get; private set; }

        public virtual ICollection<SitemapItemRecord> GetSitemapItemRecords(Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var vendorSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Vendor));
            var vendorIds = vendorSitemapItems.Select(si => si.ObjectId).ToArray();
            var members = MemberService.GetByIds(vendorIds);
            foreach (var member in members)
            {
                var vendor = member as Vendor;
                if (vendor != null)
                {
                    var vendorSitemapItemRecords = CreateSitemapItemRecords(sitemap, sitemap.UrlTemplate, SitemapItemTypes.Vendor, vendor);
                    sitemapItemRecords.AddRange(vendorSitemapItemRecords);
                }
            }

            return sitemapItemRecords;
        }
    }
}