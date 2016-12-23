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

        public virtual void LoadSitemapItemRecords(Sitemap sitemap, string baseUrl)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();
            var vendorOptions = new SitemapItemOptions();

            var vendorSitemapItems = sitemap.Items.Where(x => x.ObjectType.EqualsInvariant(SitemapItemTypes.Vendor));
            var vendorIds = vendorSitemapItems.Select(x => x.ObjectId).ToArray();
            var members = MemberService.GetByIds(vendorIds);
            foreach (var sitemapItem in vendorSitemapItems)
            {
                var vendor = members.FirstOrDefault(x=>x.Id == sitemapItem.ObjectId) as Vendor;
                if (vendor != null)
                {                    
                    sitemapItem.ItemsRecords = GetSitemapItemRecords(vendorOptions, sitemap.UrlTemplate, baseUrl, vendor);
                }
            }
        }
    }
}