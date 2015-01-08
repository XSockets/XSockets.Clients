angular.module("xsockets", []).provider('xs', [
    function () {
        var self = this;
        this.instance = undefined;
        this.$get = [
            '$q', '$rootScope', function ($q, $rootScope) {
                if (self.controllers.length === 0) {
                    throw "You need to configure the controllers";
                }
                self.instance = new XSockets.WebSocket(self.url, self.controllers);

                self.instance.onconnected = function (evt) {
                };

                var controller = function (c) {
                    var clientInfo;
                    var controllerInstance = self.instance.controller(c);

                    var invoke = function (t, d, cb) {
                        controllerInstance.invoke(t, d);
                        if (cb) cb(t, d);
                        return this;
                    };

                   var controllerName = c;

                    var on = function (t, fn) {
                        controllerInstance.on(t, function (msg) {
                            $rootScope.$apply(function (evt) {
                                fn(msg, evt);
                            });
                        });
                        return this;
                    }
                    var subscribe = function (t, fn, cb) {
                        controllerInstance.subscribe(t, function (msg) {
                            $rootScope.$apply(function (evt) {
                                fn(msg, evt);
                            });
                        }, typeof (cb) === "function" ? function (msg) {
                            $rootScope.$apply(function (evt) {
                                cb(msg, evt);
                            });
                        } : undefined);
                        return this;
                    };
                    var publish = function (t, d, cb) {
                        controllerInstance.publish(t, d);
                        if (cb) cb(t, d);
                        return this;
                    }
                    var unsubscribe = function (t, cb) {
                        controllerInstance.unsubscribe(t);
                        if (cb) cb(t);
                        return this;
                    };
                    var close = function (cb) {
                        controllerInstance.close();
                        if (cb) cb();
                        return this;
                    };

                    var onclose = function () {
                        var deferred = $q.defer();
                        controllerInstance.onclose = function (ci) {
                            deferred.resolve(ci);
                        };
                        return deferred.promise;
                    };

                    var one = function (t, fn, cb) {
                        controllerInstance.one(t, function (msg) {
                            $rootScope.$apply(function (evt) {
                                fn(msg, evt);
                            }, cb);
                        });
                        return this;
                    };

                    var many = function (t, n, fn) {
                        controllerInstance.many(t, n, function (msg) {
                            $rootScope.$apply(function (evt) {
                                fn(msg, evt);
                            });
                        });
                        return this;
                    }

                    var onerror = function () {
                        var deferred = $q.defer();
                        controllerInstance.onerror = function (ex) {
                            deferred.resolve(ex);
                        };
                        return deferred.promise;
                    };

                    var onopen = function () {
                        var deferred = $q.defer();
                        controllerInstance.onopen = function (ci) {
                            clientInfo = ci;
                            deferred.resolve(ci);
                        };
                        return deferred.promise;
                    };

                    var setEnum = function (name, value) {
                        controllerInstance.setEnum(name, value);
                    };

                    var setProperty = function (name, value) {
                        controllerInstance.setProperty(name, value);
                    };

                    return {
                        controllerName: controllerName,
                        on: on,
                        invoke: invoke,
                        publish: publish,
                        subscribe: subscribe,
                        unsubscribe: unsubscribe,
                        onopen: onopen(),
                        onclose: onclose(),
                        onerror: onerror(),
                        clientInfo: clientInfo,
                        close: close,
                        one: one,
                        many: many,
                        setEnum: setEnum,
                        setProperty: setProperty
                    }
                };




                var controllers = function () {
                    return self.controllerInstances;
                };

                return {
                    controllers: controllers,
                    controller: controller
                };

            }
        ];
        this.url = "ws://127.0.0.1:4502";
        this.controllers = [];


    }

]);


angular.module("xsockets").factory("xsDataSync", [function () {

    var dataSync = function (controller) {
        var syncName = controller.controllerName;
        var self = this;
        this.oninit = function () {
            throw "You need to add an oninit event handler";
        };
        this.onupdated = function () {
            throw "You need to add an onupdated event handler";
        };
        this.ondeleted = function () {
            throw "You need to add an ondeleted event handler";
        };
        controller.on("init:" + syncName, function (d) {
            self.oninit(d);
        });
        controller.subscribe("update:" + syncName, function (d) {
            self.onupdated(d);
        });
        controller.subscribe("add:" + syncName, function (d) {

            self.onupdated(d);
        });
        controller.subscribe("delete:" + syncName, function (d) {

            self.ondeleted(d);
        });
        this.addItem = function (f) {
            controller.invoke('update', { Topic: syncName, Object: f });
        };
        this.updateItem = function (f) {
            controller.invoke('update', f);
        };
        this.deleteItem = function (f) {
            controller.invoke('delete', { Id: f.Id });
        };
        return this;
    }

    return dataSync;


}]);