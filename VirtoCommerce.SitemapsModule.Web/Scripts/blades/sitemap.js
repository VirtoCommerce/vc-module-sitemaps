angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.uiGridHelper', 'virtoCommerce.sitemapsModule.sitemaps', function ($scope, bladeNavigationService, uiGridHelper, sitemapsResource) {
    var blade = $scope.blade;
    blade.headIcon = 'fa fa-sitemap';
    blade.toolbarCommands = getBladeToolbar();
    blade.originalEntity = angular.copy(blade.currentEntity);
    blade.selectedItems = [];
    blade.isLoading = false;
    blade.refresh = function () {
        getSitemapById(blade.parentBlade.storeId, blade.currentEntity.id);
    }
    blade.removeSitemapItems = function (items) {
        _.each(items, function (item) {
            blade.currentEntity.items = _.reject(blade.currentEntity.items, function (i) { return i.id === item.id });
        });
    }

    $scope.setForm = function (form) {
        $scope.formScope = form;
    }

    $scope.setGridOptions = function (gridOptions) {
        uiGridHelper.initialize($scope, gridOptions, function (gridApi) {
        });
    }

    blade.refresh();

    function getBladeToolbar() {
        return [{
            name: 'sitemapsModule.blades.sitemap.toolbar.addItems',
            icon: 'fa fa-plus',
            canExecuteMethod: function () {
                return true;
            },
            executeMethod: function () {
                showAddItemsBlade(blade.currentEntity.items);
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
                return $scope.formScope && $scope.formScope.$valid && !angular.equals(blade.originalEntity, blade.currentEntity) && filenameIsUnique();
            },
            executeMethod: function () {
                if (blade.currentEntity.isNew) {
                    addSitemap(blade.currentEntity);
                } else {
                    updateSitemap(blade.currentEntity);
                }
            }
        }, {
            name: 'sitemapsModule.blades.sitemap.toolbar.previewXml',
            icon: 'fa fa-file-code-o',
            canExecuteMethod: function () {
                return false;
            },
            executeMethod: function () {
            }
        }];
    }

    function filenameIsUnique() {
        //var isUnique = !_.some(blade.parentBlade.currentEntities, function (s) { return s.filename == blade.currentEntity.filename });
        //if (!isUnique) {
        //    $scope.formScope.filename.$setValidity('unique', false);
        //} else {
        //    $scope.formScope.filename.$setValidity('unique', true);
        //}
        //return isUnique;
        return true;
    }

    function showAddItemsBlade(selectedItems) {
        var addItemsBlade = {
            id: 'addSitemapItemsBlade',
            title: 'sitemapsModule.blades.addItems.title',
            controller: 'virtoCommerce.sitemapsModule.sitemapItemsAddController',
            template: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/blades/sitemap-items-add.tpl.html',
            selectedItems: selectedItems
        }
        bladeNavigationService.showBlade(addItemsBlade, blade);
    }

    function getSitemapById(storeId, sitemapId) {
        if (!sitemapId) {
            return;
        }
        blade.isLoading = true;
        sitemapsResource.getById({
            storeId: storeId,
            sitemapId: sitemapId
        }, function (response) {
            blade.isLoading = false;
            blade.currentEntity = response;
        }, function (error) {
            showError(blade, error);
        });
    }

    function addSitemap(sitemap) {
        blade.isLoading = true;
        sitemapsResource.add({}, sitemap, function (response) {
            blade.isLoading = false;
            blade.parentBlade.refresh();
        }, function (error) {
            showError(blade, error);
        });
    }

    function updateSitemap(sitemap) {
        blade.isLoading = true;
        sitemapsResource.update({}, sitemap, function (response) {
            blade.isLoading = false;
            blade.parentBlade.refresh();
        }, function (error) {
            showError(blade, error);
        });
    }

    function showError(blade, error) {
        bladeNavigationService.setError('Error ' + error.status, blade);
        blade.isLoading = false;
    }
}]);