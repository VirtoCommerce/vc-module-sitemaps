﻿using Microsoft.Practices.Unity;
using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.Hosting;
using VirtoCommerce.ContentModule.Data.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Assets;
using VirtoCommerce.Platform.Data.Azure;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Repositories;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders;
using VirtoCommerce.SitemapsModule.Web.ExportImport;

namespace VirtoCommerce.SitemapsModule.Web
{
    public class Module : ModuleBase, ISupportExportImportModule
    {
        private const string _connectionStringName = "VirtoCommerce";
        private readonly IUnityContainer _container;

        public Module(IUnityContainer container)
        {
            _container = container;
        }

        public override void SetupDatabase()
        {
            using (var context = new SitemapRepository(_connectionStringName, _container.Resolve<AuditableInterceptor>()))
            {
                var initializer = new SetupDatabaseInitializer<SitemapRepository, Data.Migrations.Configuration>();
                initializer.InitializeDatabase(context);
            }
        }

        public override void Initialize()
        {
            Func<string, IContentBlobStorageProvider> contentProviderFactory = rootPath =>
            {
                var settingManager = _container.Resolve<ISettingsManager>();
                var connectionString = settingManager.GetValue("VirtoCommerce.Content.CmsContentConnectionString", string.Empty);
                var configConnectionString = ConfigurationManager.ConnectionStrings["CmsContentConnectionString"];
                if (configConnectionString != null && !string.IsNullOrEmpty(configConnectionString.ConnectionString))
                {
                    connectionString = configConnectionString.ConnectionString;
                }

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("CmsContentConnectionString not defined. Please define module setting VirtoCommerce.Content.CmsContentConnectionString or in web.config");
                }
                var blobConnectionString = BlobConnectionString.Parse(connectionString);
                if (string.Equals(blobConnectionString.Provider, FileSystemBlobProvider.ProviderName, StringComparison.OrdinalIgnoreCase))
                {
                    var storagePath = Path.Combine(NormalizePath(blobConnectionString.RootPath), rootPath.Replace("/", "\\"));
                    //Use content api/content as public url by default
                    var publicUrl = VirtualPathUtility.ToAbsolute("~/api/content/" + rootPath) + "?relativeUrl=";
                    if (!string.IsNullOrEmpty(blobConnectionString.PublicUrl))
                    {
                        publicUrl = blobConnectionString.PublicUrl + "/" + rootPath;
                    }
                    //Do not export default theme (Themes/default) its will distributed with code
                    return new FileSystemContentBlobStorageProvider(storagePath, publicUrl);
                }
                else if (string.Equals(blobConnectionString.Provider, AzureBlobProvider.ProviderName, StringComparison.OrdinalIgnoreCase))
                {
                    return new AzureContentBlobStorageProvider(blobConnectionString.ConnectionString, Path.Combine(blobConnectionString.RootPath, rootPath));
                }
                throw new InvalidOperationException("Unknown storage provider: " + blobConnectionString.Provider);
            };
            _container.RegisterInstance(contentProviderFactory);

            _container.RegisterType<ISitemapRepository>(new InjectionFactory(c => new SitemapRepository(_connectionStringName, new EntityPrimaryKeyGeneratorInterceptor(), _container.Resolve<AuditableInterceptor>())));
            _container.RegisterType<ISitemapItemService, SitemapItemService>();
            _container.RegisterType<ISitemapService, SitemapService>();
            _container.RegisterType<ISitemapUrlBuilder, SitemapUrlBuilder>();
            _container.RegisterType<ISitemapItemRecordProvider, CatalogSitemapItemRecordProvider>("CatalogSitemapItemRecordProvider");
            _container.RegisterType<ISitemapItemRecordProvider, CustomSitemapItemRecordProvider>("CustomSitemapItemRecordProvider");
            _container.RegisterType<ISitemapItemRecordProvider, VendorSitemapItemRecordProvider>("VendorSitemapItemRecordProvider");
            _container.RegisterType<ISitemapItemRecordProvider, StaticContentSitemapItemRecordProvider>("StaticContentSitemapItemRecordProvider");
            _container.RegisterType<ISitemapXmlGenerator, SitemapXmlGenerator>();
        }

     
        public void DoExport(System.IO.Stream outStream, PlatformExportManifest manifest, Action<ExportImportProgressInfo> progressCallback)
        {
            var job = _container.Resolve<SitemapExportImport>();
            job.DoExport(outStream, progressCallback);
        }

        public void DoImport(System.IO.Stream inputStream, PlatformExportManifest manifest, Action<ExportImportProgressInfo> progressCallback)
        {
            var job = _container.Resolve<SitemapExportImport>();
            job.DoImport(inputStream, progressCallback);
        }

        public string ExportDescription
        {
            get
            {
                var settingManager = _container.Resolve<ISettingsManager>();
                return settingManager.GetValue("Sitemap.ExportImport.Description", string.Empty);
            }
        }

        private string NormalizePath(string path)
        {
            string result;

            if (path.StartsWith("~"))
            {
                result = HostingEnvironment.MapPath(path);
            }
            else if (Path.IsPathRooted(path))
            {
                result = path;
            }
            else
            {
                result = HostingEnvironment.MapPath("~/");
                result += path;
            }

            return result != null ? Path.GetFullPath(result) : null;
        }
    }
}