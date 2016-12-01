using System.Collections.Generic;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Services;
using Xunit;

namespace VirtoCommerce.SitemapsModule.Test
{
    public class SitemapUrlBuilderTests
    {
        private readonly ISitemapUrlBuilder _sitemapUrlBuilder;

        private Store Store { get; set; }
        private SeoInfo SeoInfo { get; set; }

        public SitemapUrlBuilderTests()
        {
            _sitemapUrlBuilder = new SitemapUrlBuilder();

            Store = new Store
            {
                Id = "TestStore",
                Languages = new List<string> { "en-GB", "en-US", "ru-RU" },
                SecureUrl = "https://test.com",
                Url = "http://test.com"
            };

            SeoInfo = new SeoInfo
            {
                LanguageCode = "en-US",
                SemanticUrl = "apps",
                StoreId = Store.Id
            };
        }

        [Fact]
        public void StoreHasNoUrls_SeoInfo_Test()
        {
            Store.SecureUrl = null;
            Store.Url = null;

            var absoluteUrl = _sitemapUrlBuilder.ToAbsoluteUrl(Store, SeoInfo);

            Assert.Null(absoluteUrl);
        }

        [Fact]
        public void StoreHasUrl_SeoInfo_Test()
        {
            Store.SecureUrl = null;

            var absoluteUrl = _sitemapUrlBuilder.ToAbsoluteUrl(Store, SeoInfo);

            Assert.NotNull(absoluteUrl);
            Assert.True(absoluteUrl == "http://test.com/en-US/apps");
        }

        [Fact]
        public void StoreHasSecureUrl_SeoInfo_Test()
        {
            Store.Url = null;

            var absoluteUrl = _sitemapUrlBuilder.ToAbsoluteUrl(Store, SeoInfo);

            Assert.NotNull(absoluteUrl);
            Assert.True(absoluteUrl == "https://test.com/en-US/apps");
        }

        [Fact]
        public void StoreHasUrl_SeoInfo_SeoLanguageMismatch_Test()
        {
            SeoInfo.LanguageCode = "pt-PT";

            var absoluteUrl = _sitemapUrlBuilder.ToAbsoluteUrl(Store, SeoInfo);

            Assert.Null(absoluteUrl);
        }

        [Fact]
        public void StoreHasUrl_SeoInfo_SeoInfoNotActive_Test()
        {
            SeoInfo.IsActive = false;

            var absoluteUrl = _sitemapUrlBuilder.ToAbsoluteUrl(Store, SeoInfo);

            Assert.Null(absoluteUrl);
        }

        [Fact]
        public void StoreHasUrl_RelativeUrl_Test()
        {
            var relativeUrl = "apps";

            var absoluteUrl = _sitemapUrlBuilder.ToAbsoluteUrl(Store, relativeUrl);

            Assert.NotNull(absoluteUrl);
            Assert.True(absoluteUrl == "http://test.com/apps");
        }
    }
}