using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace VirtoCommerce.SitemapsModule.Test
{
    public class SitemapUrlBuilderTests
    {
        [Theory]
        [InlineData("", 6)]
        [InlineData(".md,.html", 5)]
        public void CheckStaticContentExtensions(string acceptedExtensionsString, int expectedCount)
        {
            var urls = new[]
            {
                "cms/Pages/demo/static/goroda-ispanii.md",
                "cms/Pages/demo/static/o-kompanii.md",
                "cms/Pages/demo/static/opros.md",
                "cms/Pages/demo/static/partners.md",
                "cms/Pages/demo/static/picture.jpg",
                "cms/Pages/demo/static/no-extension"
            };

            // TODO: Call actual implementation

            var acceptedExtensions = acceptedExtensionsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var sitemapUrls = new List<string>();
            foreach (var url in urls)
            {
                var itemExtension = Path.GetExtension(url);
                if (!acceptedExtensions.Any() ||
                    string.IsNullOrEmpty(itemExtension) ||
                    acceptedExtensions.Contains(itemExtension, StringComparer.OrdinalIgnoreCase))
                {
                    sitemapUrls.Add(url);
                }
            }

            Assert.Equal(expectedCount, sitemapUrls.Count);
        }
    }
}
