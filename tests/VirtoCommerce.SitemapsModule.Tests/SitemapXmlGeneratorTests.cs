using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VirtoCommerce.CatalogModule.Core.Model.ListEntry;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Outlines;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Models.Search;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;
using static VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.SEO;

namespace VirtoCommerce.SitemapsModule.Tests;

public class SitemapXmlGeneratorTests
{
    [Theory]
    [InlineData(SeoShort, "sitemap_short.xml")]
    //[InlineData(SeoCollapsed, "sitemap_collapsed.xml")]
    public async Task GenerateSitemapXml_ShouldReturnValidXml(string seoLinksType, string expectedXmlFileName)
    {
        // Arrange  
        var sitemapGenerator = new SitemapXmlGenerator(
            GetSitemapSearchServiceMock().Object,
            GetSitemapItemSearchServiceMock().Object,
            GetSitemapUrlBuilder(),
            [GetCatalogSitemapItemRecordProvider()],
            new Mock<ISettingsManager>().Object,
            new Mock<ILogger<SitemapXmlGenerator>>().Object,
            GetStoreServiceMock(seoLinksType).Object);

        var expectedXml = await GetEmbeddedResourceString($"Resources.{expectedXmlFileName}");

        // Act  
        var stream = await sitemapGenerator.GenerateSitemapXmlAsync("store1", "https://test.local", "products.xml");
        var actualXml = await ReadAsString(stream);

        // Assert  
        Assert.Equal(expectedXml, actualXml);
    }


    private static Mock<ISitemapSearchService> GetSitemapSearchServiceMock()
    {
        var sitemapSearchServiceMock = new Mock<ISitemapSearchService>();
        sitemapSearchServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<SitemapSearchCriteria>()))
            .ReturnsAsync(
                new SitemapSearchResult { Results = [new Sitemap { Location = "products.xml", UrlTemplate = "{slug}" }] });

        return sitemapSearchServiceMock;
    }

    private static Mock<ISitemapItemSearchService> GetSitemapItemSearchServiceMock()
    {
        var sitemapItemSearchServiceMock = new Mock<ISitemapItemSearchService>();
        sitemapItemSearchServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<SitemapItemSearchCriteria>()))
            .ReturnsAsync(
                new SitemapItemsSearchResult
                {
                    Results =
                    [
                        new SitemapItem { ObjectType = "category", ObjectId = "category1" },
                        new SitemapItem { ObjectType = "category", ObjectId = "category2" },
                    ],
                });
        return sitemapItemSearchServiceMock;
    }

    private static SitemapUrlBuilder GetSitemapUrlBuilder()
    {
        return new SitemapUrlBuilder(new Mock<ICategoryService>().Object);
    }

    private static CatalogSitemapItemRecordProvider GetCatalogSitemapItemRecordProvider()
    {
        return new CatalogSitemapItemRecordProvider(
            GetSitemapUrlBuilder(),
            new Mock<ISettingsManager>().Object,
            new Mock<IItemService>().Object,
            GetListEntrySearchServiceMock().Object);
    }

    private static Mock<IStoreService> GetStoreServiceMock(string seoLinksType)
    {
        var storeServiceMock = new Mock<IStoreService>();

        storeServiceMock
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([new Store
            {
                Id = "store1",
                Catalog = "catalog1",
                DefaultLanguage = "en",
                Languages = ["en"],
                Settings = [new ObjectSettingEntry { Name = SeoLinksType.Name, Value = seoLinksType }],
            }]);

        return storeServiceMock;
    }

    private static Mock<IListEntrySearchService> GetListEntrySearchServiceMock()
    {
        var listEntrySearchServiceMock = new Mock<IListEntrySearchService>();

        listEntrySearchServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<CatalogListEntrySearchCriteria>()))
            .ReturnsAsync(
                new ListEntrySearchResult
                {
                    Results =
                    [
                        new ProductListEntry
                        {
                            Id = "product1",
                            SeoInfos = [
                                new SeoInfo { StoreId = "store1", LanguageCode = "en", SemanticUrl = "product1-en" },
                                new SeoInfo { StoreId = "store1", LanguageCode = "de", SemanticUrl = "product1-de" },
                            ],
                            Outlines = [
                                new Outline
                                {
                                    Items = [
                                        new OutlineItem
                                        {
                                            Id = "catalog1",
                                            SeoObjectType = "Catalog",
                                            SeoInfos = [
                                                new SeoInfo { StoreId = "store1", LanguageCode = "en", SemanticUrl = "catalog1-en" },
                                                new SeoInfo { StoreId = "store1", LanguageCode = "de", SemanticUrl = "catalog1-de" },
                                            ],
                                        },
                                        new OutlineItem
                                        {
                                            Id = "category1",
                                            SeoObjectType = "Category",
                                            SeoInfos = [
                                                new SeoInfo { StoreId = "store1", LanguageCode = "en", SemanticUrl = "category1-en" },
                                                new SeoInfo { StoreId = "store1", LanguageCode = "de", SemanticUrl = "category1-de" },
                                            ],
                                        },
                                        new OutlineItem
                                        {
                                            Id = "product1",
                                            SeoObjectType = "CatalogProduct",
                                            SeoInfos = [
                                                new SeoInfo { StoreId = "store1", LanguageCode = "en", SemanticUrl = "product1-en" },
                                                new SeoInfo { StoreId = "store1", LanguageCode = "de", SemanticUrl = "product1-de" },
                                            ],
                                        },
                                    ],
                                },
                                new Outline
                                {
                                    Items = [
                                        new OutlineItem
                                        {
                                            Id = "catalog1",
                                            SeoObjectType = "Catalog",
                                            SeoInfos = [
                                                new SeoInfo { StoreId = "store1", LanguageCode = "en", SemanticUrl = "catalog1-en" },
                                                new SeoInfo { StoreId = "store1", LanguageCode = "de", SemanticUrl = "catalog1-de" },
                                            ],
                                        },
                                        new OutlineItem
                                        {
                                            Id = "category2",
                                            SeoObjectType = "Category",
                                            SeoInfos = [
                                                new SeoInfo { StoreId = "store1", LanguageCode = "en", SemanticUrl = "category2-en" },
                                                new SeoInfo { StoreId = "store1", LanguageCode = "de", SemanticUrl = "category2-de" },
                                            ],
                                        },
                                        new OutlineItem
                                        {
                                            Id = "product1",
                                            SeoObjectType = "CatalogProduct",
                                            SeoInfos = [
                                                new SeoInfo { StoreId = "store1", LanguageCode = "en", SemanticUrl = "product1-en" },
                                                new SeoInfo { StoreId = "store1", LanguageCode = "de", SemanticUrl = "product1-de" },
                                            ],
                                        },
                                    ],
                                },
                            ],
                        },
                    ],
                });

        return listEntrySearchServiceMock;
    }

    private async Task<string> GetEmbeddedResourceString(string filePath)
    {
        var currentAssembly = GetType().Assembly;
        var resourcePath = $"{currentAssembly.GetName().Name}.{filePath}";
        var stream = currentAssembly.GetManifestResourceStream(resourcePath);

        if (stream is null)
        {
            return null;
        }

        var text = await ReadAsString(stream);

        return text.TrimEnd();
    }

    private static async Task<string> ReadAsString(Stream stream)
    {
        using var streamReader = new StreamReader(stream);
        return await streamReader.ReadToEndAsync();
    }
}
