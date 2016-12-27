angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapListController', ['$window', '$scope', '$modal', 'platformWebApp.bladeUtils', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.uiGridHelper', 'virtoCommerce.sitemapsModule.sitemaps', function ($window, $scope, $modal, bladeUtils, bladeNavigationService, dialogService, uiGridHelper, sitemapsResource) {
    bladeUtils.initializePagination($scope);
    var blade = $scope.blade;
    blade.headIcon = 'fa fa-sitemap';
    blade.toolbarCommands = getBladeToolbarCommands();
    blade.refresh = function () {
        searchSitemaps(blade.store.id, ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount, $scope.pageSettings.itemsPerPageCount);
    };

    blade.selectNode = function (listItem) {
        $scope.selectedNodeId = listItem.id;

        showSitemapBlade(listItem);
    };

    blade.removeSitemaps = function (sitemaps) {
        showSitemapRemoveConfirmationDialog(sitemaps);
    };

    blade.downloadSitemaps = function () {
        showBaseUrlDialog(blade.store.id, blade.store.url || blade.store.secureUrl);
    };

    blade.refresh();

    $scope.setGridOptions = function (gridOptions) {
        uiGridHelper.initialize($scope, gridOptions);
        bladeUtils.initializePagination($scope);
    };

    function getBladeToolbarCommands() {
        return [{
            name: 'sitemapsModule.blades.sitemapList.toolbar.refresh',
            icon: 'fa fa-refresh',
            canExecuteMethod: function () {
                return true;
            },
            executeMethod: blade.refresh
        }, {
            name: 'sitemapsModule.blades.sitemapList.toolbar.addSitemap',
            icon: 'fa fa-plus',
            canExecuteMethod: function () {
                return true;
            },
            executeMethod: function () {
                $scope.selectedNodeId = undefined;
                showSitemapBlade();
            }
        }, {
            name: 'sitemapsModule.blades.sitemapList.toolbar.removeSitemap',
            icon: 'fa fa-trash',
            canExecuteMethod: function () {
                return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
            },
            executeMethod: function () {
                blade.removeSitemaps($scope.gridApi.selection.getSelectedRows());
            }
        }, {
            name: 'sitemapsModule.blades.sitemapList.toolbar.download',
            icon: 'fa fa-download',
            canExecuteMethod: function () {
                return $scope.pageSettings.totalItems > 0;
            },
            executeMethod: function () {
                blade.downloadSitemaps();
            }
        }];
    }

    function showSitemapBlade(sitemap) {
        var sitemapBlade = {
            id: 'sitemap',
            controller: 'virtoCommerce.sitemapsModule.sitemapController',
            template: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/blades/sitemap.tpl.html',
            currentEntity: {
                isNew: true,
                location: 'sitemap/',
                urlTemplate: '{language}/{slug}',
                storeId: blade.store.id,
                items: []
            }
        };

        if (sitemap) {
            sitemapBlade.currentEntity = angular.copy(sitemap);
        }
        bladeNavigationService.showBlade(sitemapBlade, blade);
    }

    function showSitemapRemoveConfirmationDialog(sitemaps) {
        var sitemapIds = _.map(sitemaps, function (s) { return s.id; });
        var confirmDialog = {
            id: 'confirmDeleteSitemaps',
            title: 'sitemapsModule.dialogs.confirmRemoveSitemap.title',
            message: 'sitemapsModule.dialogs.confirmRemoveSitemap.message',
            callback: function (confirm) {
                if (confirm) {
                    removeSitemaps(sitemapIds);
                }
            }
        };
        dialogService.showConfirmationDialog(confirmDialog);
    }

    function showBaseUrlDialog(storeId, baseUrl) {
        var confirmDialog = {
            id: 'confirmBaseUrl',
            originalBaseUrl: angular.copy(baseUrl),
            baseUrl: baseUrl,
            templateUrl: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/dialogs/confirm-base-url-dialog.tpl.html',
            controller: 'virtoCommerce.sitemapsModule.baseUrlDialogController',
            resolve: {
                dialog: function () {
                    return confirmDialog;
                }
            }
        };
        var dialogInstance = $modal.open(confirmDialog);
        dialogInstance.result.then(function (baseUrl) {
            if (baseUrl) {
                downloadSitemaps(storeId, baseUrl);
            }
        });
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
        });
    }

    function removeSitemaps(sitemapIds) {
        blade.isLoading = true;
        _.each(blade.childrenBlades, function (childBlade) {
            bladeNavigationService.closeBlade(childBlade);
        });
        sitemapsResource.remove({ ids: sitemapIds },
            blade.refresh,
            function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
    }

    function downloadSitemaps(storeId, baseUrl) {
        window.open('api/sitemaps/download?storeId=' + storeId + '&baseUrl=' + baseUrl, '_blank');
    }
}]);