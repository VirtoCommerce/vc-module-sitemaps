angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapPreviewController', ['$scope', '$http', 'platformWebApp.bladeNavigationService', function ($scope, $http, bladeNavigationService) {
    var blade = $scope.blade;
    blade.headIcon = 'fa fa-sitemap';

    $http.get('api/sitemaps/xml?storeId=' + blade.storeId + '&sitemapFilename=' + blade.filename).then(
    function (response) {
        $scope.xml = response.data;
        blade.isLoading = false;
    }, function (response) {
        bladeNavigationService.setError('Error ' + response.status, blade);
        blade.isLoading = false;
    });
}]);