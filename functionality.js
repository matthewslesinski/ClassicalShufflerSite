(function(angular) {
  'use strict';
angular.
 module('myServiceModule', []).
  controller('MyController', ['$scope', 'notify', function($scope, notify) {
    $scope.callNotify = function(msg) {
      notify(msg);
    };
  }]).
 factory('notify', ['$window', '$http', function(win, $http) {
    return function(msg) {
      $http.post('http://' + msg, 'longitude=-77&latitude=40').then(function(response) {
        win.alert(response.data)
      }, function(response) {
        win.alert(response.status)
      });
    };
  }]);
})(window.angular);