angular.module('virtoCommerce.sitemapsModule')
.factory('virtoCommerce.sitemapsModule.sitemaps', ['$resource', function ($resource) {
    return $resource('api/sitemaps', {}, {
        getAll: { method: 'GET', isArray: true },
        getById: { method: 'GET', url: 'api/sitemaps/:sitemapId' },
        add: { method: 'POST' },
        update: { method: 'PUT' },
        remove: { method: 'DELETE' }
    });
}]);