angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapListController', ['$scope', 'platformWebApp.bladeUtils', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.uiGridHelper', 'virtoCommerce.sitemapsModule.sitemaps', function ($scope, bladeUtils, bladeNavigationService, dialogService, uiGridHelper, sitemapsResource) {
    bladeUtils.initializePagination($scope);
    var blade = $scope.blade;
    blade.headIcon = 'fa fa-sitemap';
    blade.storeId = blade.parentBlade.currentEntity.id;
    blade.isLoading = false;
    blade.toolbarCommands = getBladeToolbarCommands();
    blade.refresh = function () {
        searchSitemaps(blade.storeId, ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount, $scope.pageSettings.itemsPerPageCount);
    }
    blade.selectSitemap = function (sitemap) {
        showSitemapBlade(sitemap);
    }
    blade.removeSitemap = function (sitemap) {
        showSitemapRemoveConfirmationDialog(sitemap.storeId, sitemap.id);
    }

    blade.refresh();

    $scope.setGridOptions = function (gridOptions) {
        uiGridHelper.initialize($scope, gridOptions, function (gridApi) {
        });
        bladeUtils.initializePagination($scope);
    }

    function getBladeToolbarCommands() {
        return [{
            name: 'sitemapsModule.blades.sitemapList.toolbar.addSitemap',
            icon: 'fa fa-plus',
            canExecuteMethod: function () {
                return true;
            },
            executeMethod: function () {
                showSitemapBlade();
            }
        }, {
            name: 'sitemapsModule.blades.sitemapList.toolbar.refresh',
            icon: 'fa fa-refresh',
            canExecuteMethod: function () {
                return true;
            },
            executeMethod: function () {
                blade.refresh();
            }
        }];
    }

    function showSitemapBlade(sitemap) {
        var sitemapBlade = {
            id: 'sitemap',
            title: 'sitemapsModule.blades.sitemap.newSitemapTitle',
            controller: 'virtoCommerce.sitemapsModule.sitemapController',
            template: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/blades/sitemap.tpl.html',
            currentEntity: {
                isNew: true,
                storeId: blade.storeId
            }
        }
        if (sitemap) {
            sitemapBlade.title = sitemap.filename;
            sitemapBlade.currentEntity = angular.copy(sitemap);
            sitemapBlade.isNew = false;
        }
        sitemapBlade.originalSitemap = angular.copy(sitemapBlade.currentEntity);
        bladeNavigationService.showBlade(sitemapBlade, blade);
    }

    function showSitemapRemoveConfirmationDialog(storeId, sitemapIds) {
        var confirmDialog = {
            id: 'confirmDeleteSitemaps',
            title: 'sitemapsModule.dialogs.confirmRemoveSitemap.title',
            message: 'sitemapsModule.dialogs.confirmRemoveSitemap.message',
            callback: function (confirm) {
                if (confirm) {
                    removeSitemaps(storeId, sitemapIds);
                }
            }
        }
        dialogService.showConfirmationDialog(confirmDialog);
    }

    function searchSitemaps(storeId, skip, take) {
        blade.isLoading = true;
        sitemapsResource.searchSitemaps({}, {
            storeId: storeId,
            skip: skip,
            take: take
        }, function (response) {
            $scope.pageSettings.totalItems = response.totalCount;
            $scope.listEntries = response.items;
            blade.isLoading = false;
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function removeSitemaps(storeId, sitemapIds) {
        blade.isLoading = true;
        sitemapsResource.remove({ storeId: storeId, sitemapIds: sitemapIds }, function () {
            blade.refresh();
            blade.isLoading = false;
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }
}]);

//angular.module('virtoCommerce.sitemapsModule')
//.controller('virtoCommerce.sitemapsModule.sitemapListController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.uiGridHelper', 'virtoCommerce.sitemapsModule.sitemaps', function ($scope, bladeNavigationService, dialogService, uiGridHelper, sitemapsResource) {
//    var blade = $scope.blade;
//    blade.headIcon = 'fa fa-sitemap';
//    blade.toolbarCommands = getBladeToolbar();
//    blade.storeId = $scope.blade.parentBlade.currentEntity.id;
//    blade.isLoading = false;
//    blade.refresh = function () {
//        getSitemaps(this.storeId);
//    }
//    blade.selectSitemap = function (sitemap) {
//        showSitemapBlade(sitemap);
//    }
//    blade.removeSitemap = function (sitemap) {
//        showConfirmationDialog(sitemap.storeId, sitemap.id);
//    }

//    blade.refresh();

//    $scope.setGridOptions = function (gridOptions) {
//        uiGridHelper.initialize($scope, gridOptions, function (gridApi) {
//        });
//    }

//    function getBladeToolbar() {
//        return [{
//            name: 'sitemapsModule.blades.sitemapList.toolbar.addSitemap',
//            icon: 'fa fa-plus',
//            canExecuteMethod: function () {
//                return true;
//            },
//            executeMethod: function () {
//                showSitemapBlade();
//            }
//        }, {
//            name: 'sitemapsModule.blades.sitemapList.toolbar.refresh',
//            icon: 'fa fa-refresh',
//            canExecuteMethod: function () {
//                return true;
//            },
//            executeMethod: function () {
//                blade.refresh();
//            }
//        }];
//    }

//    function getSitemaps(storeId) {
//        blade.isLoading = true;
//        sitemapsResource.getAll({
//            storeId: storeId
//        }, function (response) {
//            blade.currentEntities = response;
//            blade.isLoading = false;
//        }, function (error) {
//            showError(blade, error);
//        });
//    }

//    function removeSitemaps(storeId, sitemapIds) {
//        blade.isLoading = true;
//        sitemapsResource.remove({ storeId: storeId, sitemapIds: sitemapIds }, function () {
//            blade.refresh();
//            _.each(blade.childrenBlades, function (b) { bladeNavigationService.closeBlade(b); });
//            blade.isLoading = false;
//        }, function (error) {
//            showError(blade, error);
//        });
//    }

//    function showConfirmationDialog(storeId, sitemapIds) {
//        var confirmDialog = {
//            id: 'confirmDeleteSitemaps',
//            title: 'sitemapsModule.dialogs.confirmRemoveSitemap.title',
//            message: 'sitemapsModule.dialogs.confirmRemoveSitemap.message',
//            callback: function (confirm) {
//                if (confirm) {
//                    removeSitemaps(storeId, sitemapIds);
//                }
//            }
//        }
//        dialogService.showConfirmationDialog(confirmDialog);
//    }

//    function showSitemapBlade(sitemap) {
//        var newBlade = {
//            id: 'sitemap',
//            title: 'sitemapsModule.blades.sitemap.newSitemapTitle',
//            controller: 'virtoCommerce.sitemapsModule.sitemapController',
//            template: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/blades/sitemap.tpl.html',
//            currentEntity: {
//                isNew: true,
//                storeId: blade.storeId
//            }
//        }
//        if (sitemap) {
//            newBlade.title = sitemap.filename;
//            newBlade.currentEntity = angular.copy(sitemap);
//            newBlade.isNew = false;
//        }
//        newBlade.originalSitemap = angular.copy(newBlade.currentEntity);
//        bladeNavigationService.showBlade(newBlade, blade);
//    }

//    function showError(blade, error) {
//        bladeNavigationService.setError('Error ' + error.status, blade);
//        blade.isLoading = false;
//    }
//}]);