## XSockets.anguarJS
This is a beta version of a angularJS provider for XSockets.NET version 4.0 (generation 4).  As this is a beta, we just provide a briefly describe version of the angularJS API.  Consult our developer forum if you run into any kind of problems.

###Configuring the provider
Below you find an example how to configure the endpoint (`url`) and the controllers to use and connect.

    var myApp = angular.module("myApp", ['ngRoute', 'myAppControllers', 'xsockets.angularjs']);
    
    myApp.config(["xsocketsProvider", function (xsocketsProvider) {
    xsocketsProvider.url = "ws://127.0.0.1:4502";
    xsocketsProvider.controllers = ["zoo","animal"];
    }]);
 The snippet below will connect to the endpoint and use the zoo, animal controller.
 
###Using the provider from an angularJS controller 

The controller below will illustrate how to subscribe, publish etc.   The AngularJS API is pretty much streamlined to the XSockets.NET JavaScript client. https://github.com/XSockets/XSockets.Clients/tree/master/src/XSockets.Clients/XSockets.JavaScript

####Controller ( Example )

    myAppControllers.controller("homeController", ['$scope', 'xsockets', function ($scope, xsockets) {
    $scope.chatMessages = [];
    var generic = xsockets.controller("generic");
    generic.onopen.then(function (ci) {
        // ci will contain connection information
        console.log("ci", ci);
        generic.subscribe("chatmessage", function (data) {
            $scope.chatMessages.push(data);
        });
        // using RPC
        //generic.on("chatMessage", function (data) {
        //    $scope.says.push(data);
        //});
    });
    generic.onclose.then(function () {
        // do op
    });
    // when a user hits a button..   
    $scope.say = function () {
        generic.publish("chatmessage", {
            msg: "Just a phrase"
        });
        //generic.invoke("chatMessage", {
        //    msg: "Just a phrase"
        //});
    };
    }]);


####View

    <button ng-click="say()">
    Say</button>
    <ul ng-repeat="cm in chatMessages">
    <li>
        {{cm.msg}}
    </li>
    /ul>