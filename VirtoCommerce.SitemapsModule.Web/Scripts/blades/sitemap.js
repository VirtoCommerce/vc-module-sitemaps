angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapController', ['$scope', 'platformWebApp.bladeUtils', 'platformWebApp.uiGridHelper', 'platformWebApp.bladeNavigationService', 'virtoCommerce.sitemapsModule.sitemaps', function ($scope, bladeUtils, uiGridHelper, bladeNavigationService, sitemapsResource) {
    bladeUtils.initializePagination($scope);
    var blade = $scope.blade;
    blade.headIcon = 'fa fa-sitemap';
    blade.isLoading = false;
    blade.toolbarCommands = getBladeToolbarCommands();
    blade.addItems = function (sitemapItems) {
        blade.isLoading = true;
        if (blade.currentEntity.isNew) {
            _.each(sitemapItems, function (sitemapItem) {
                blade.currentEntity.items.push(sitemapItem);
            });
            blade.isLoading = false;
        } else {
            addSitemapItems(sitemapItems);
        }
    }
    blade.removeItems = function (sitemapItems) {
        blade.isLoading = true;
        if (blade.currentEntity.isNew) {
            _.each(sitemapItems, function (sitemapItem) {
                blade.currentEntity.items = _.without(blade.currentEntity.items, sitemapItem);
            });
            blade.isLoading = false;
        } else {
            removeSitemapItems(sitemapItems);
        }
    }
    blade.refresh = function () {
        if (!blade.currentEntity.isNew) {
            getSitemap();
        }
    }
    blade.saveSitemap = function () {
        saveSitemap();
    }

    $scope.setForm = function (form) {
        $scope.formScope = form;
    }

    $scope.setGridOptions = function (gridOptions) {
        uiGridHelper.initialize($scope, gridOptions, function (gridApi) { });
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
        }, {
            name: 'sitemapsModule.blades.sitemap.toolbar.saveSitemap',
            icon: 'fa fa-save',
            canExecuteMethod: function () {
                return $scope.formScope && $scope.formScope.$valid && !angular.equals(blade.originalEntity, blade.currentEntity);
            },
            executeMethod: function () {
                $scope.errorMessage = null;
                saveSitemap();
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
            getSitemapItems();
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
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
            blade.isLoading = false;
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function addSitemapItems(sitemapItems) {
        blade.isLoading = true;
        sitemapsResource.addSitemapItems({
            sitemapId: blade.currentEntity.id
        }, sitemapItems, function (response) {
            blade.isLoading = false;
            blade.refresh();
            blade.parentBlade.refresh();
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function removeSitemapItems(sitemapItems) {
        blade.isLoading = true;
        var sitemapItemIds = _.map(sitemapItems, function (i) { return i.id });
        sitemapsResource.removeSitemapItems({
            itemIds: sitemapItemIds
        }, function (response) {
            blade.isLoading = false;
            blade.refresh();
            blade.parentBlade.refresh();
        }, function (error) {
            bladeNavigationService.setError('Error ' + error.status, blade);
            blade.isLoading = false;
        });
    }

    function saveSitemap() {
        blade.isLoading = true;
        if (blade.currentEntity.filename.toLowerCase() === 'sitemap.xml') {
            $scope.errorMessage = 'sitemapsModule.blades.sitemap.formSitemap.sitemapIndexErrorMessage';
            blade.isLoading = false;
        }
        sitemapsResource.searchSitemaps({}, {
            storeId: blade.currentEntity.storeId,
            filename: blade.currentEntity.filename,
        }, function (response) {
            if (!blade.currentEntity.id) {
                if (response.totalCount > 0) {
                    $scope.errorMessage = 'sitemapsModule.blades.sitemap.formSitemap.sitemapSitemapLocationExistErrorMessage';
                    blade.isLoading = false;
                }
            } else {
                var existingSitemap = _.find(response.results, function (s) { return s.filename === blade.currentEntity.filename && s.id !== blade.currentEntity.id });
                if (existingSitemap) {
                    $scope.errorMessage = 'sitemapsModule.blades.sitemap.formSitemap.sitemapSitemapLocationExistErrorMessage';
                    blade.isLoading = false;
                }
            }
            if (!$scope.errorMessage) {
                if (blade.currentEntity.isNew) {
                    sitemapsResource.addSitemap({}, blade.currentEntity, function (response) {
                        blade.currentEntity = response;
                        blade.isLoading = false;
                        if (blade.currentEntity.items.length) {
                            addSitemapItems(blade.currentEntity.items);
                        }
                        blade.refresh();
                        blade.parentBlade.refresh();
                    }, function (error) {
                        bladeNavigationService.setError('Error ' + error.status, blade);
                        blade.isLoading = false;
                    });
                } else {
                    sitemapsResource.updateSitemap({}, blade.currentEntity, function (response) {
                        blade.isLoading = false;
                        blade.refresh();
                        blade.parentBlade.refresh();
                    }, function (error) {
                        bladeNavigationService.setError('Error ' + error.status, blade);
                        blade.isLoading = false;
                    });
                }
            }
        });
    }
}]);