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
        showSitemapRemoveConfirmationDialog(sitemap.id);
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
                return blade.parentBlade.currentEntity.url || blade.parentBlade.currentEntity.secureUrl;
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
        }, {
            name: 'sitemapsModule.blades.sitemapList.toolbar.download',
            icon: 'fa fa-download',
            canExecuteMethod: function () {
                return $scope.pageSettings.totalItems > 0;
            },
            executeMethod: function () {
                downloadSitemaps(blade.storeId);
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
        sitemapBlade.originalEntity = angular.copy(sitemapBlade.currentEntity);
        bladeNavigationService.showBlade(sitemapBlade, blade);
    }

    function showSitemapRemoveConfirmationDialog(sitemapIds) {
        var confirmDialog = {
            id: 'confirmDeleteSitemaps',
            title: 'sitemapsModule.dialogs.confirmRemoveSitemap.title',
            message: 'sitemapsModule.dialogs.confirmRemoveSitemap.message',
            callback: function (confirm) {
                if (confirm) {
                    removeSitemaps(sitemapIds);
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
            $scope.listEntries = response.results;
            blade.isLoading = false;
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function removeSitemaps(sitemapIds) {
        blade.isLoading = true;
        _.each(blade.childrenBlades, function (childBlade) {
            bladeNavigationService.closeBlade(childBlade);
        });
        sitemapsResource.remove({ ids: sitemapIds }, function () {
            blade.refresh();
            blade.isLoading = false;
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function downloadSitemaps(storeId) {
        blade.isLoading = true;
        sitemapsResource.downloadSitemaps({ storeId: storeId }, function (response) {
            $scope.zipDownloadUrl = response.url;
            blade.isLoading = false;
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }
}]);