angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapItemsAddController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.sitemapsModule.sitemaps', function ($scope, bladeNavigationService, sitemapsResource) {
    var blade = $scope.blade;
    blade.headIcon = 'fa fa-sitemap';
    blade.isLoading = false;

    $scope.addCatalogItems = function () {
        var selectedItems = [];
        var newBlade = {
            id: 'addSitemapCatalogItems',
            title: 'sitemapsModule.blades.addCatalogItems.title',
            controller: 'virtoCommerce.catalogModule.catalogItemSelectController',
            template: 'Modules/$(VirtoCommerce.Catalog)/Scripts/blades/common/catalog-items-select.tpl.html',
            breadcrumbs: [],
            toolbarCommands: [{
                name: 'sitemapsModule.blades.addCatalogItems.toolbar.addSelected',
                icon: 'fa fa-plus',
                canExecuteMethod: function () {
                    return selectedItems.length > 0;
                },
                executeMethod: function (catalogBlade) {
                    addItemsToSitemap(selectedItems, blade.parentBlade);
                    bladeNavigationService.closeBlade(catalogBlade);
                }
            }],
            options: {
                allowCheckingCategory: true,
                checkItemFn: function (listItem, isSelected) {
                    if (isSelected) {
                        if (_.all(selectedItems, function (x) { return x.id != listItem.id; })) {
                            selectedItems.push(listItem);
                        }
                    } else {
                        selectedItems = _.reject(selectedItems, function (x) { return x.id == listItem.id; });
                    }
                    blade.error = undefined;
                }
            }
        }
        bladeNavigationService.showBlade(newBlade, blade.parentBlade);
    }

    function addItemsToSitemap(items, blade) {
        blade.isLoading = true;
        var sitemapItems = [];
        _.each(items, function (item) {
            sitemapItems.push({
                title: item.name,
                imageUrl: item.imageUrl,
                objectId: item.id,
                objectType: item.type
            });
        });
        if (blade.currentEntity.isNew) {
            blade.currentEntity.items = sitemapItems;
            blade.isLoading = false;
        } else {
            sitemapsResource.addSitemapItems({
                sitemapId: blade.currentEntity.id
            }, sitemapItems, function (response) {
                blade.refresh();
            }, function (error) {
                bladeNavigationService.setError('Error ' + error.status, blade);
                blade.isLoading = false;
            });
        }
    }
}]);