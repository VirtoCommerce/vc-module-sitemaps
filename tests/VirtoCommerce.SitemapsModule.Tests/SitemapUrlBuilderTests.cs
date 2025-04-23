using System.Collections.Generic;
using VirtoCommerce.CoreModule.Core.Outlines;
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
        Id = "s1",
        Url = "https://store.com",
        Languages = ["en"],
        DefaultLanguage = "en",
        Catalog = "catalog1",
    };

    private static readonly Store _multiLanguageStore = new()
    {
        Id = "s2",
        Url = "https://store.com",
        Languages = ["en", "fr"],
        DefaultLanguage = "en",
        Catalog = "catalog2",
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
        Outlines = [
            new Outline
            {
                Items = [
                    new OutlineItem
                    {
                        Id = "catalog1",
                        SeoObjectType = "Catalog",
                        SeoInfos = [new SeoInfo { StoreId = "s1", LanguageCode = "en", SemanticUrl = "s1-en-catalog1" }],
                    },
                    new OutlineItem
                    {
                        Id = "category1",
                        SeoObjectType = "Category",
                        SeoInfos = [new SeoInfo { StoreId = "s1", LanguageCode = "en", SemanticUrl = "s1-en-category1" }],
                    },
                    new OutlineItem
                    {
                        Id = "category2",
                        SeoObjectType = "Category",
                        SeoInfos = [new SeoInfo { StoreId = "s1", LanguageCode = "en", SemanticUrl = "s1-en-category2" }],
                    },
                ],
            },
        ],
    };


    public static IEnumerable<object[]> TestData =>
        new List<object[]>
        {
            //             Store                 Base URL            Language Template                     Entity   Expected URL
            new object[] { null,                 null,               null,    "",                          null,    "~/" },
            new object[] { null,                 null,               null,    "test",                      null,    "~/test" },
            new object[] { null,                 null,               null,    "{slug}",                    _entity, "~/" },

            new object[] { _singleLanguageStore, null,               null,    "",                          null,    "https://store.com/" },
            new object[] { _singleLanguageStore, null,               null,    "test",                      null,    "https://store.com/test" },
            new object[] { _singleLanguageStore, null,               null,    "{slug}",                    _entity, "https://store.com/s1-en-category2" },
            new object[] { _singleLanguageStore, null,               "en",    "{slug}",                    _entity, "https://store.com/s1-en-category2" },
            new object[] { _singleLanguageStore, null,               "fr",    "{slug}",                    _entity, "https://store.com/s1-en-category2" },
            new object[] { _singleLanguageStore, null,               "fr",    "{slug_short}",              _entity, "https://store.com/s1-en-category2" },
            new object[] { _singleLanguageStore, null,               "fr",    "{slug_long}",               _entity, "https://store.com/s1-en-category1/s1-en-category2" },

            new object[] { _multiLanguageStore,  null,               null,    "",                          null,    "https://store.com/en/" },
            new object[] { _multiLanguageStore,  null,               null,    "test",                      null,    "https://store.com/en/test" },
            new object[] { _multiLanguageStore,  null,               null,    "{slug}",                    _entity, "https://store.com/en/s2-en-entity" },
            new object[] { _multiLanguageStore,  null,               "en",    "{slug}",                    _entity, "https://store.com/en/s2-en-entity" },
            new object[] { _multiLanguageStore,  null,               "fr",    "{slug}",                    _entity, "https://store.com/fr/s2-fr-entity" },
            new object[] { _multiLanguageStore,  null,               "en",    "",                          null,    "https://store.com/en/" },
            new object[] { _multiLanguageStore,  null,               "fr",    "test",                      null,    "https://store.com/fr/test" },
            new object[] { _multiLanguageStore,  null,               "de",    "test",                      null,    "https://store.com/en/test" },
            new object[] { _multiLanguageStore,  null,               "en",    "https://absolute.com/test", _entity, "https://absolute.com/test" },
            new object[] { _multiLanguageStore,  "https://base.com", "fr",    "{slug}",                    _entity, "https://base.com/fr/s2-fr-entity" },
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

    public class TestEntity : ISeoSupport, IHasOutlines
    {
        public string Id { get; set; }
        public string SeoObjectType { get; set; }
        public IList<SeoInfo> SeoInfos { get; set; }
        public IList<Outline> Outlines { get; set; }
    }
}
