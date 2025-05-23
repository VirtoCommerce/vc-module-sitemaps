using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.ListEntry;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Outlines;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Models.Search;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;
using static VirtoCommerce.CatalogModule.Core.Extensions.SeoExtensions;
using static VirtoCommerce.SitemapsModule.Core.ModuleConstants.Settings.ProductLinks;
using static VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.SEO;

namespace VirtoCommerce.SitemapsModule.Tests;

public class SitemapXmlGeneratorTests
{
    [Theory]
    [InlineData(SeoShort, "en", false, "sitemap_short_en.xml")]
    [InlineData(SeoShort, "en,de", false, "sitemap_short_en_de.xml")]
    [InlineData(SeoCollapsed, "en", false, "sitemap_collapsed_en.xml")]
    [InlineData(SeoCollapsed, "en,de", false, "sitemap_collapsed_en_de.xml")]

    [InlineData(SeoShort, "en", true, "sitemap_short_en_images.xml")]
    [InlineData(SeoShort, "en,de", true, "sitemap_short_en_de_images.xml")]
    [InlineData(SeoCollapsed, "en", true, "sitemap_collapsed_en_images.xml")]
    [InlineData(SeoCollapsed, "en,de", true, "sitemap_collapsed_en_de_images.xml")]
    public async Task GenerateSitemapXml_ShouldReturnValidXml(string seoLinksType, string languages, bool withImages, string expectedXmlFileName)
    {
        // Arrange
        var sitemapGenerator = new SitemapXmlGenerator(
            GetSitemapSearchServiceMock().Object,
            GetSitemapItemSearchServiceMock().Object,
            GetSitemapUrlBuilder(),
            [GetCatalogSitemapItemRecordProvider()],
            new Mock<ISettingsManager>().Object,
            new Mock<ILogger<SitemapXmlGenerator>>().Object,
            GetStoreServiceMock(seoLinksType, languages, withImages).Object);

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
                        new SitemapItem { ObjectType = "product", ObjectId = "product1" },
                        new SitemapItem { ObjectType = "product", ObjectId = "product2" },
                    ],
                });

        return sitemapItemSearchServiceMock;
    }

    private static CatalogSitemapItemRecordProvider GetCatalogSitemapItemRecordProvider()
    {
        return new CatalogSitemapItemRecordProvider(
            GetSitemapUrlBuilder(),
            new Mock<ISettingsManager>().Object,
            GetItemServiceMock().Object,
            GetListEntrySearchServiceMock().Object);
    }

    private static SitemapUrlBuilder GetSitemapUrlBuilder()
    {
        return new SitemapUrlBuilder();
    }

    private static Mock<IStoreService> GetStoreServiceMock(string seoLinksType, string languages, bool withImages)
    {
        var storeServiceMock = new Mock<IStoreService>();

        storeServiceMock
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([new Store
            {
                Id = "store1",
                Catalog = "catalog1",
                DefaultLanguage = "en",
                Languages = languages.Split(','),
                Settings = [
                    new ObjectSettingEntry { Name = SeoLinksType.Name, Value = seoLinksType },
                    new ObjectSettingEntry { Name = IncludeImages.Name, Value = withImages },
                ],
            }]);

        return storeServiceMock;
    }

    private static Mock<IItemService> GetItemServiceMock()
    {
        var itemServiceMock = new Mock<IItemService>();

        itemServiceMock
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> _, string responseGroup, bool _) =>
            {
                var product = new CatalogProduct { Id = "product1" };

                if (EnumUtility.SafeParseFlags(responseGroup, ItemResponseGroup.None).HasFlag(ItemResponseGroup.WithImages))
                {
                    product.Images = [new Image { Url = "https://images.local/image1.jpg" }];
                }

                return [product];
            });

        return itemServiceMock;
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
                        new CategoryListEntry
                        {
                            Id = "category3",
                            SeoInfos = GetSeoInfos("category3"),
                            Outlines = [
                                GetOutline("catalog2/category1/category3"),
                                GetOutline("catalog1/category1/category3"),
                                GetOutline("catalog1/category2/category3"),
                                GetOutline("catalog1/category4/category3"),
                            ],
                        },
                        new ProductListEntry
                        {
                            Id = "product1",
                            SeoInfos = GetSeoInfos("product1"),
                            Outlines = [
                                GetOutline("catalog2/category1/product1"),
                                GetOutline("catalog2/category1/category5/product1"),
                                GetOutline("catalog1/category1/product1"),
                                GetOutline("catalog1/category2/product1"),
                                GetOutline("catalog1/category4/product1"),
                                GetOutline("catalog1/category2/category3/product1"),
                            ],
                        },
                    ],
                });

        return listEntrySearchServiceMock;
    }

    private static Outline GetOutline(string template)
    {
        return new Outline
        {
            Items = template
                .Split('/')
                .Select(GetOutlineItem)
                .ToList(),
        };
    }

    private static OutlineItem GetOutlineItem(string id)
    {
        return new OutlineItem
        {
            Id = id,
            SeoObjectType = GetSeoType(id),
            SeoInfos = GetSeoInfos(id),
        };
    }

    private static string GetSeoType(string id)
    {
        if (id.StartsWith("catalog", StringComparison.OrdinalIgnoreCase))
        {
            return SeoCatalog;
        }

        if (id.StartsWith("category", StringComparison.OrdinalIgnoreCase))
        {
            return SeoCategory;
        }

        if (id.StartsWith("product", StringComparison.OrdinalIgnoreCase))
        {
            return SeoProduct;
        }

        throw new ArgumentException($"Cannot get object type from id '{id}'");
    }

    private static readonly string[] _seoStoreIds = [null, "store2", "store1"];
    private static readonly string[] _seoLanguages = [null, "en", "de"];

    private static List<SeoInfo> GetSeoInfos(string id)
    {
        var result = new List<SeoInfo>();


        foreach (var storeId in _seoStoreIds)
        {
            foreach (var language in _seoLanguages)
            {
                result.Add(new SeoInfo { StoreId = storeId, LanguageCode = language, SemanticUrl = $"{id}-{language}-{storeId}" });
            }
        }

        return result;
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
