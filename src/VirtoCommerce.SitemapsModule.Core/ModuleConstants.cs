using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.SitemapsModule.Core
{
    [ExcludeFromCodeCoverage]
    public static class ModuleConstants
    {
        public static class Security
        {
            public static class Permissions
            {
                public const string Access = "sitemaps:access";
                public const string Create = "sitemaps:create";
                public const string Read = "sitemaps:read";
                public const string Update = "sitemaps:update";
                public const string Delete = "sitemaps:delete";

                public static readonly string[] AllPermissions = { Access, Create, Read, Update, Delete };
            }
        }

        public static class Settings
        {
#pragma warning disable S3218

            public static class General
            {
                public static readonly SettingDescriptor RecordsLimitPerFile = new SettingDescriptor
                {
                    Name = "Sitemap.RecordsLimitPerFile",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.PositiveInteger,
                    DefaultValue = 10000
                };

                public static readonly SettingDescriptor FilenameSeparator = new SettingDescriptor
                {
                    Name = "Sitemap.FilenameSeparator",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.ShortText,
                    DefaultValue = "--"
                };

                public static readonly SettingDescriptor SearchBunchSize = new SettingDescriptor
                {
                    Name = "Sitemap.SearchBunchSize",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.PositiveInteger,
                    DefaultValue = 500
                };

                public static readonly SettingDescriptor AcceptedFilenameExtensions = new SettingDescriptor
                {
                    Name = "Sitemap.AcceptedFilenameExtensions",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.ShortText,
                    DefaultValue = ".md,.html"
                };
            }

            public static class ProductLinks
            {
                public static readonly SettingDescriptor ProductPageUpdateFrequency = new SettingDescriptor
                {
                    Name = "Sitemap.ProductPageUpdateFrequency",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.ShortText,
                    AllowedValues = new object[]
                    {
                        "always",
                        "hourly",
                        "daily",
                        "weekly",
                        "monthly",
                        "yearly",
                        "never"
                    },
                    DefaultValue = "daily"
                };

                public static readonly SettingDescriptor ProductPagePriority = new SettingDescriptor
                {
                    Name = "Sitemap.ProductPagePriority",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.Decimal,
                    DefaultValue = 1.0m,
                    IsRequired = true,
                };

                public static readonly SettingDescriptor IncludeImages = new SettingDescriptor
                {
                    Name = "Sitemap.IncludeImages",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.Boolean,
                    DefaultValue = false
                };
            }

            public static class CategoryLinks
            {
                public static readonly SettingDescriptor CategoryPageUpdateFrequency = new SettingDescriptor
                {
                    Name = "Sitemap.CategoryPageUpdateFrequency",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.ShortText,
                    AllowedValues = new object[]
                    {
                        "always",
                        "hourly",
                        "daily",
                        "weekly",
                        "monthly",
                        "yearly",
                        "never"
                    },
                    DefaultValue = "weekly"
                };

                public static readonly SettingDescriptor CategoryPagePriority = new SettingDescriptor
                {
                    Name = "Sitemap.CategoryPagePriority",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.Decimal,
                    DefaultValue = 0.7m,
                    IsRequired = true,
                };
            }

            public static class BlogLinks
            {
                public static readonly SettingDescriptor BlogPageUpdateFrequency = new SettingDescriptor
                {
                    Name = "Sitemap.BlogPageUpdateFrequency",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.ShortText,
                    AllowedValues = new object[]
                    {
                        "always",
                        "hourly",
                        "daily",
                        "weekly",
                        "monthly",
                        "yearly",
                        "never"
                    },
                    DefaultValue = "weekly",
                };

                public static readonly SettingDescriptor BlogPagePriority = new SettingDescriptor
                {
                    Name = "Sitemap.BlogPagePriority",
                    GroupName = "Sitemap",
                    ValueType = SettingValueType.Decimal,
                    DefaultValue = 0.5m,
                    IsRequired = true,
                };
            }

            public static IEnumerable<SettingDescriptor> AllSettings
            {
                get
                {
                    yield return General.RecordsLimitPerFile;
                    yield return General.FilenameSeparator;
                    yield return General.SearchBunchSize;
                    yield return General.AcceptedFilenameExtensions;
                    yield return CategoryLinks.CategoryPageUpdateFrequency;
                    yield return CategoryLinks.CategoryPagePriority;
                    yield return ProductLinks.ProductPageUpdateFrequency;
                    yield return ProductLinks.ProductPagePriority;
                    yield return ProductLinks.IncludeImages;
                    yield return BlogLinks.BlogPageUpdateFrequency;
                    yield return BlogLinks.BlogPagePriority;
                }
            }

            public static IEnumerable<SettingDescriptor> StoreLevelSettings
            {
                get
                {
                    yield return CategoryLinks.CategoryPageUpdateFrequency;
                    yield return CategoryLinks.CategoryPagePriority;
                    yield return ProductLinks.ProductPageUpdateFrequency;
                    yield return ProductLinks.ProductPagePriority;
                    yield return ProductLinks.IncludeImages;
                    yield return BlogLinks.BlogPageUpdateFrequency;
                    yield return BlogLinks.BlogPagePriority;

                }
            }

#pragma warning restore S3218
        }
    }
}
