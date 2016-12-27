angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapItemsAddCustomItemController', ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
    var blade = $scope.blade;

    blade.toolbarCommands = [{
        name: 'sitemapsModule.blades.addCustomItem.toolbar.save',
        icon: 'fa fa-save',
        canExecuteMethod: function () {
            return $scope.formScope && $scope.formScope.$valid;
        },
        executeMethod: saveCustomSitemapItem
    }];

    function saveCustomSitemapItem() {
        blade.parentBlade.addItems([{
            title: blade.currentEntity.urlTemplate,
            imageUrl: null,
            objectType: 'Custom',
            urlTemplate: blade.currentEntity.urlTemplate,
            sitemapId: blade.currentEntity.sitemap.id
        }]);
        bladeNavigationService.closeBlade(blade);
    }

    $scope.setForm = function (form) { $scope.formScope = form; };
    blade.isLoading = false;
}]);