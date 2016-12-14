angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapController', ['$scope', 'platformWebApp.bladeUtils', 'platformWebApp.bladeNavigationService', 'platformWebApp.uiGridHelper', 'virtoCommerce.sitemapsModule.sitemaps', function ($scope, bladeUtils, bladeNavigationService, uiGridHelper, sitemapsResource) {
    bladeUtils.initializePagination($scope);
    var blade = $scope.blade;
    blade.headIcon = 'fa fa-sitemap';
    blade.isLoading = false;
    blade.toolbarCommands = getBladeToolbarCommands();
    blade.currentEntity.urlTemplate = '{storeUrl}/{slug}';
    blade.refresh = function () {
        if (!blade.currentEntity.isNew) {
            getSitemapById(blade.currentEntity.id);
            searchSitemapItems(blade.currentEntity.id, ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount, $scope.pageSettings.itemsPerPageCount);
        }
    }
    blade.removeSitemapItems = function (items) {
        removeSitemapItems(blade.currentEntity, _.map(items, function (i) { return i.id }));
    }

    $scope.setForm = function (form) {
        $scope.formScope = form;
    }

    $scope.setGridOptions = function (gridOptions) {
        uiGridHelper.initialize($scope, gridOptions, function (gridApi) {
        });
        bladeUtils.initializePagination($scope);
    }

    function getBladeToolbarCommands() {
        return [{
            name: 'sitemapsModule.blades.sitemap.toolbar.addItems',
            icon: 'fa fa-plus',
            canExecuteMethod: function () {
                return true;
            },
            executeMethod: function () {
                showAddItemsBlade($scope.gridApi.selection.getSelectedRows());
            }
        }, {
            name: 'sitemapsModule.blades.sitemap.toolbar.removeItems',
            icon: 'fa fa-trash',
            canExecuteMethod: function () {
                return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
            },
            executeMethod: function () {
                blade.removeSitemapItems($scope.gridApi.selection.getSelectedRows());
            }
        }, {
            name: 'sitemapsModule.blades.sitemap.toolbar.saveSitemap',
            icon: 'fa fa-save',
            canExecuteMethod: function () {
                return $scope.formScope && $scope.formScope.$valid && !angular.equals(blade.originalEntity, blade.currentEntity);
            },
            executeMethod: function () {
                blade.isLoading = true;
                saveChanges(blade.currentEntity);
            }
        }];
    }

    function showAddItemsBlade(selectedItems) {
        var addItemsBlade = {
            id: 'addSitemapItemsBlade',
            title: 'sitemapsModule.blades.addItems.title',
            controller: 'virtoCommerce.sitemapsModule.sitemapItemsAddController',
            template: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/blades/sitemap-items-add.tpl.html',
            selectedItems: selectedItems,
            sitemap: blade.currentEntity
        }
        bladeNavigationService.showBlade(addItemsBlade, blade);
    }

    function saveChanges(sitemap) {
        if (sitemap.isNew) {
            addSitemap(sitemap);
        } else {
            updateSitemap(sitemap);
        }
    }

    function getSitemapById(sitemapId) {
        blade.isLoading = true;
        sitemapsResource.getSitemapById({
            id: sitemapId
        }, function (response) {
            blade.currentEntity = response;
            blade.isLoading = false;
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function addSitemap(sitemap) {
        blade.isLoading = true;
        sitemapsResource.addSitemap({}, sitemap, function (response) {
            blade.currentEntity = response;
            blade.refresh();
            blade.parentBlade.refresh();
            blade.isLoading = false;
            if (sitemap.items && sitemap.items.length) {
                addSitemapItems(response.id, sitemap.items);
            }
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function updateSitemap(sitemap) {
        blade.isLoading = true;
        sitemapsResource.updateSitemap({}, sitemap, function (response) {
            blade.isLoading = false;
            blade.parentBlade.refresh();
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function searchSitemapItems(sitemapId, skip, take) {
        blade.isLoading = true;
        sitemapsResource.searchSitemapItems({}, {
            sitemapId: sitemapId,
            skip: skip,
            take: take
        }, function (response) {
            $scope.pageSettings.totalItems = response.totalCount;
            blade.currentEntity.items = response.results;
            blade.currentEntity.totalItemsCount = response.totalCount;
            blade.isLoading = false;
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function addSitemapItems(sitemapId, sitemapItems) {
        blade.isLoading = true;
        sitemapsResource.addSitemapItems({
            sitemapId: sitemapId
        }, sitemapItems, function (response) {
            blade.refresh();
            //blade.parentBlade.refresh();
            blade.isLoading = false;
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function removeSitemapItems(sitemap, itemIds) {
        if (sitemap.isNew) {
            _.each(itemIds, function (itemId) {
                var item = _.find(sitemap.items, function (i) { return i.id == itemId });
                if (item) {
                    sitemap.items = _.without(sitemap.items, item);
                }
            });
        } else {
            blade.isLoading = true;
            sitemapsResource.removeSitemapItems({
                itemIds: itemIds
            }, function (response) {
                blade.refresh();
                blade.parentBlade.refresh();
                blade.isLoading = false;
            }, function (error) {
                bladeNavigationService.setError('Error ' + error.status, blade);
                blade.isLoading = false;
            });
        }
    }
}]);