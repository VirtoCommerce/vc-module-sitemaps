angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapController', ['$scope', 'platformWebApp.bladeUtils', 'platformWebApp.uiGridHelper', 'platformWebApp.bladeNavigationService', 'virtoCommerce.sitemapsModule.sitemaps', function ($scope, bladeUtils, uiGridHelper, bladeNavigationService, sitemapsResource) {
    var blade = $scope.blade;
    blade.updatePermission = 'sitemaps:update';

    blade.toolbarCommands = getBladeToolbarCommands();
    blade.addedSitemapItems = [];
    blade.removedSitemapItems = [];
    blade.addItems = function (sitemapItems) {
        _.each(sitemapItems, function (sitemapItem) {
            blade.addedSitemapItems.push(sitemapItem);
            blade.currentEntity.items.push(sitemapItem);
        });
    };

    blade.removeItems = function (sitemapItems) {
        _.each(sitemapItems, function (sitemapItem) {
            blade.currentEntity.items = _.without(blade.currentEntity.items, sitemapItem);
            blade.removedSitemapItems.push(sitemapItem);
        });
    };

    blade.refresh = function (parentRefresh) {
        if (blade.currentEntity.isNew) {
            blade.title = 'sitemapsModule.blades.sitemap.newSitemapTitle';
            blade.isLoading = false;
        } else {
            blade.title = blade.currentEntity.location;
            getSitemap();
        }

        if (parentRefresh) {
            blade.parentBlade.refresh();
        }
    };

    blade.onClose = function (closeCallback) {
        bladeNavigationService.showConfirmationIfNeeded(isDirty(), canSave(), blade, saveSitemap, closeCallback, "sitemapsModule.dialogs.discardChanges.title", "sitemapsModule.dialogs.discardChanges.message");
    };

    $scope.setForm = function (form) { $scope.formScope = form; };

    $scope.setGridOptions = function (gridOptions) {
        uiGridHelper.initialize($scope, gridOptions);
        bladeUtils.initializePagination($scope);
    };

    blade.refresh();

    function isDirty() {
        return !angular.equals(blade.origEntity, blade.currentEntity);
    }

    function canSave() {
        return isDirty() && $scope.formScope && $scope.formScope.$valid;
    }

    function getBladeToolbarCommands() {
        return [{
            name: 'sitemapsModule.blades.sitemap.toolbar.saveSitemap',
            icon: 'fa fa-save',
            canExecuteMethod: canSave,
            executeMethod: saveSitemap
        }, {
            name: "platform.commands.reset", icon: 'fa fa-undo',
            executeMethod: function () {
                angular.copy(blade.origEntity, blade.currentEntity);
            },
            canExecuteMethod: isDirty,
            permission: blade.updatePermission
        },
            {
                name: 'sitemapsModule.blades.sitemap.toolbar.addItems',
                icon: 'fa fa-plus',
                canExecuteMethod: function () {
                    return true;
                },
                executeMethod: function () {
                    showAddItemsBlade();
                }
            }, {
                name: 'sitemapsModule.blades.sitemap.toolbar.removeItems',
                icon: 'fa fa-trash',
                canExecuteMethod: function () {
                    return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
                },
                executeMethod: function () {
                    blade.removeItems($scope.gridApi.selection.getSelectedRows());
                }
            }];
    }

    function showAddItemsBlade() {
        var addItemsBlade = {
            id: 'addSitemapItemsBlade',
            title: 'sitemapsModule.blades.addItems.title',
            controller: 'virtoCommerce.sitemapsModule.sitemapItemsAddController',
            template: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/blades/sitemap-items-add.tpl.html',
            sitemap: blade.currentEntity
        }
        bladeNavigationService.showBlade(addItemsBlade, blade);
    }

    function getSitemap() {
        blade.isLoading = true;
        sitemapsResource.getSitemapById({
            id: blade.currentEntity.id
        }, function (response) {
            blade.currentEntity = response;
            getSitemapItems(($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount, $scope.pageSettings.itemsPerPageCount);
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
        });
    }

    function getSitemapItems(skip, take) {
        blade.isLoading = true;
        sitemapsResource.searchSitemapItems({}, {
            sitemapId: blade.currentEntity.id,
            skip: skip,
            take: take
        }, function (response) {
            $scope.pageSettings.totalItems = response.totalCount;
            blade.currentEntity.items = response.results;
            blade.currentEntity.totalItemsCount = response.totalCount;
            blade.origEntity = angular.copy(blade.currentEntity);
            blade.addedSitemapItems = [];
            blade.removedSitemapItems = [];
            blade.isLoading = false;
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
        });
    }

    function addSitemapItems(sitemapItems) {
        blade.isLoading = true;
        sitemapsResource.addSitemapItems({
            sitemapId: blade.currentEntity.id
        }, sitemapItems, function () {
            blade.refresh(true);
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
        });
    }

    function removeSitemapItems(sitemapItems) {
        blade.isLoading = true;
        var sitemapItemIds = _.map(sitemapItems, function (i) { return i.id });
        sitemapsResource.removeSitemapItems({
            itemIds: sitemapItemIds
        }, function () {
            blade.refresh(true);
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
        });
    }

    function saveSitemap() {
        $scope.errorMessage = null;

        if (blade.currentEntity.location.toLowerCase() === 'sitemap.xml') {
            $scope.errorMessage = 'sitemapsModule.blades.sitemap.formSitemap.sitemapIndexErrorMessage';
        }
        sitemapsResource.searchSitemaps({}, {
            storeId: blade.currentEntity.storeId,
            location: blade.currentEntity.location,
        }, function (response) {
            if (!blade.currentEntity.id) {
                if (response.totalCount > 0) {
                    $scope.errorMessage = 'sitemapsModule.blades.sitemap.formSitemap.sitemapSitemapLocationExistErrorMessage';
                }
            } else {
                var existingSitemap = _.find(response.results, function (s) { return s.location === blade.currentEntity.location && s.id !== blade.currentEntity.id });
                if (existingSitemap) {
                    $scope.errorMessage = 'sitemapsModule.blades.sitemap.formSitemap.sitemapSitemapLocationExistErrorMessage';
                }
            }
            if (!$scope.errorMessage) {
                blade.isLoading = true;
                if (blade.currentEntity.isNew) {
                    sitemapsResource.addSitemap({}, blade.currentEntity, function (response) {
                        blade.currentEntity = response;
                        if (blade.addedSitemapItems.length) {
                            addSitemapItems(blade.addedSitemapItems);
                        }
                        blade.refresh(true);
                    }, function (error) {
                        bladeNavigationService.setError('Error ' + error.status, blade);
                    });
                } else {
                    var addedSitemapItems = blade.addedSitemapItems;
                    var removedSitemapItems = blade.removedSitemapItems;
                    sitemapsResource.updateSitemap({}, blade.currentEntity, function (response) {
                        if (blade.addedSitemapItems.length) {
                            addSitemapItems(blade.addedSitemapItems);
                        }
                        if (blade.removedSitemapItems.length) {
                            removeSitemapItems(blade.removedSitemapItems);
                        }
                        blade.refresh(true);
                    }, function (error) {
                        bladeNavigationService.setError('Error ' + error.status, blade);
                    });
                }
            }
        });
    }
}]);