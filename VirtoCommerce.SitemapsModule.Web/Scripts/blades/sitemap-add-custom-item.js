angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapItemsAddCustomItemController', ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
    var blade = $scope.blade;
    blade.headIcon = 'fa fa-sitemap';
    blade.isLoading = false;
    blade.toolbarCommands = getBladeToolbarCommands();

    $scope.setForm = function (form) {
        $scope.formScope = form;
    }

    function getBladeToolbarCommands() {
        return [{
            name: 'sitemapsModule.blades.addCustomItem.toolbar.save',
            icon: 'fa fa-save',
            canExecuteMethod: function () {
                return $scope.formScope && $scope.formScope.$valid;
            },
            executeMethod: function () {
                saveCustomSitemapItem();
            }
        }];
    }

    function saveCustomSitemapItem() {
        blade.parentBlade.addItems([{
            title: blade.currentEntity.urlTemplate,
            objectType: 'Custom',
            urlTemplate: blade.currentEntity.urlTemplate,
            sitemapId: blade.currentEntity.sitemap.id
        }]);
        bladeNavigationService.closeBlade(blade);
    }
}]);