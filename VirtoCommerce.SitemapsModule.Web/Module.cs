using Microsoft.Practices.Unity;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.Repositories;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders;

namespace VirtoCommerce.SitemapsModule.Web
{
    public class Module : ModuleBase
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
            _container.RegisterType<ISitemapRepository>(new InjectionFactory(c => new SitemapRepository(_connectionStringName, new EntityPrimaryKeyGeneratorInterceptor(), _container.Resolve<AuditableInterceptor>())));
            _container.RegisterType<ISitemapItemService, SitemapItemService>();
            _container.RegisterType<ISitemapService, SitemapService>();
            _container.RegisterType<ISitemapUrlBuilder, SitemapUrlBuilder>();
            _container.RegisterType<ISitemapItemRecordProvider, CatalogSitemapItemRecordProvider>("CatalogSitemapItemRecordProvider");
            _container.RegisterType<ISitemapItemRecordProvider, CustomSitemapItemRecordProvider>("CustomSitemapItemRecordProvider");
            _container.RegisterType<ISitemapItemRecordProvider, VendorSitemapItemRecordProvider>("VendorSitemapItemRecordProvider");
            _container.RegisterType<ISitemapXmlGenerator, SitemapXmlGenerator>();
        }

        public override void PostInitialize()
        {
            base.PostInitialize();

            var settingManager = _container.Resolve<ISettingsManager>();
            var sitemapSettings = settingManager.GetModuleSettings("VirtoCommerce.Sitemaps");
            settingManager.RegisterModuleSettings("VirtoCommerce.Sitemaps", sitemapSettings);
        }
    }
}