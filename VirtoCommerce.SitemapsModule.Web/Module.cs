using Microsoft.Practices.Unity;
using VirtoCommerce.Platform.Core.Modularity;

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
    }
}