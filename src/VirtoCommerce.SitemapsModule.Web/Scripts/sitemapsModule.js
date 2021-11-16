var moduleName = 'virtoCommerce.sitemapsModule';

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .run(['platformWebApp.widgetService', 'platformWebApp.toolbarService', 'platformWebApp.breadcrumbHistoryService', function (widgetService, toolbarService, breadcrumbHistoryService) {
        widgetService.registerWidget({
            controller: 'virtoCommerce.sitemapsModule.storeSitemapsWidgetController',
            template: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/widgets/store-sitemaps-widget.tpl.html'
        }, 'storeDetail');

        // register back-button
        toolbarService.register(breadcrumbHistoryService.getBackButtonInstance(), 'virtoCommerce.sitemapsModule.staticContentItemSelectController');
    }]);
