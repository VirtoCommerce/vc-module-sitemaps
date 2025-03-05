using System.Collections.Generic;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;
using Xunit;

namespace VirtoCommerce.SitemapsModule.Tests;

public class SitemapUrlBuilderTests
{
    private readonly SitemapUrlBuilder _sitemapUrlBuilder = new();

    private static readonly Store _singleLanguageStore = new()
    {
        Id = "s2",
        Url = "https://store.com",
        Languages = ["en"],
        DefaultLanguage = "en",
    };

    private static readonly Store _multiLanguageStore = new()
    {
        Id = "s2",
        Url = "https://store.com",
        Languages = ["en", "fr"],
        DefaultLanguage = "en",
    };

    private static readonly TestEntity _entity = new()
    {
        SeoInfos =
        [
            new SeoInfo
            {
                StoreId = "s1",
                LanguageCode = "en",
                SemanticUrl = "s1-en-entity",
            },
            new SeoInfo
            {
                StoreId = "s1",
                LanguageCode = "fr",
                SemanticUrl = "s1-fr-entity",
            },
            new SeoInfo
            {
                StoreId = "s2",
                LanguageCode = "en",
                SemanticUrl = "s2-en-entity",
            },
            new SeoInfo
            {
                StoreId = "s2",
                LanguageCode = "fr",
                SemanticUrl = "s2-fr-entity",
            },
            new SeoInfo
            {
                SemanticUrl = "entity",
            },
        ],
    };


    public static IEnumerable<object[]> TestData =>
        new List<object[]>
        {
            //             Store                 Base URL            Language Template                     Entity   Expected URL
            new object[] { null,                 null,               null,    "",                          null,    "~/" },
            new object[] { null,                 null,               null,    "test",                      null,    "~/test" },
            new object[] { null,                 null,               null,    "/{slug}",                   _entity, "~/" },

            new object[] { _singleLanguageStore, null,               null,    "",                          null,    "https://store.com/" },
            new object[] { _singleLanguageStore, null,               null,    "test",                      null,    "https://store.com/test" },
            new object[] { _singleLanguageStore, null,               null,    "/{slug}",                   _entity, "https://store.com/s2-en-entity" },
            new object[] { _singleLanguageStore, null,               "en",    "/{slug}",                   _entity, "https://store.com/s2-en-entity" },
            new object[] { _singleLanguageStore, null,               "fr",    "/{slug}",                   _entity, "https://store.com/s2-fr-entity" },

            new object[] { _multiLanguageStore,  null,               null,    "",                          null,    "https://store.com/en/" },
            new object[] { _multiLanguageStore,  null,               null,    "test",                      null,    "https://store.com/en/test" },
            new object[] { _multiLanguageStore,  null,               null,    "/{slug}",                   _entity, "https://store.com/en/s2-en-entity" },
            new object[] { _multiLanguageStore,  null,               "en",    "/{slug}",                   _entity, "https://store.com/en/s2-en-entity" },
            new object[] { _multiLanguageStore,  null,               "fr",    "/{slug}",                   _entity, "https://store.com/fr/s2-fr-entity" },
            new object[] { _multiLanguageStore,  null,               "en",    "",                          null,    "https://store.com/en/" },
            new object[] { _multiLanguageStore,  null,               "fr",    "test",                      null,    "https://store.com/fr/test" },
            new object[] { _multiLanguageStore,  null,               "de",    "test",                      null,    "https://store.com/en/test" },
            new object[] { _multiLanguageStore,  null,               "en",    "https://absolute.com/test", _entity, "https://absolute.com/test" },
            new object[] { _multiLanguageStore,  "https://base.com", "fr",    "/{slug}",                   _entity, "https://base.com/fr/s2-fr-entity" },
        };

    [Theory]
    [MemberData(nameof(TestData))]
    public void BuildStoreUrl_ShouldReturnExpectedUrl(Store store, string baseUrl, string language, string urlTemplate, IEntity entity, string expectedUrl)
    {
        // Act
        var result = _sitemapUrlBuilder.BuildStoreUrl(store, language, urlTemplate, baseUrl, entity);

        // Assert
        Assert.Equal(expectedUrl, result);
    }

    public class TestEntity : ISeoSupport
    {
        public string Id { get; set; }
        public string SeoObjectType { get; set; }
        public IList<SeoInfo> SeoInfos { get; set; }
    }
}
