import 'dart:convert';
import 'dart:html';
class Subscription {
  String topic;
  Function fn;
  Subscription(String _topic, Function _cb) {
    this.topic = _topic.toLowerCase();
    this.fn = _cb;
  }
}
class Subscriptions {
  List < Subscription > _subscriptions;
  Subscriptions() {
    this._subscriptions = new List < Subscription > ();
  }
  Subscription find(String topic) {
    return this._subscriptions.firstWhere((e) => e.topic == topic);
  }
  remove(String topic) {
    var _subscription = this._subscriptions.firstWhere((e) => e.topic == topic);
    this._subscriptions.remove(_subscription);
  }
  Subscription add(String topic, Function fn) {
    var subscription = new Subscription(topic, fn);
    this._subscriptions.add(subscription);
    return subscription;
  }
}
class XSocketsMessage {
  String C;
  String T;
  String D;
  /**
  * Create a new XSockets.NET Message (IMessage)
  * @param contoller Name of the controller that the message is targeting
  * @param topic The message topic (Action)
  * @param data The object (data) to pass with the message
  */
  XSocketsMessage(String controller, String topic, Object data) {
    this.C = controller.toLowerCase();
    this.T = topic.toLowerCase();
    this.D = JSON.encode(data);
  }
  toString() {
    return JSON.encode({
      "C": this.C,
      "T": this.T,
      "D": this.D
    });
  }
}

class ClientInfo
{
   Storage _localStorage;
   String connectionId;
   String persistentId;
   String controller;
   
   ClientInfo(){
     _localStorage = window.localStorage; 
   }
   set(String C,String PI,String CI){
     this.connectionId = C;
         this.persistentId = PI;
         this.controller = CI;
         _localStorage[this.controller] = this.toString();
   }
   get(String ctrl) {
     
     try{
      Map map = JSON.decode(_localStorage[ctrl]);
      
      this.connectionId = map["connectionId"];
      this.persistentId = map["persistentId"];
      this.controller = map["controller"];
      
      
      return JSON.decode(this.toString());

     }catch(ex){
       return new Map();
     }
   }
   toString(){
     return JSON.encode({
           "connectionId": this.connectionId,
           "persistentId": this.persistentId,
           "controller": this.controller
         });
   }  
}


class XSockets {
  WebSocket ws;
  Subscriptions _subscriptions;
  String _controller;
  String _url;
  /**
   * Function that will be called when the connection is ready
   */
  Function onopen;
  /**
   * Function that will be called when the server closes connection-
   */
  Function onclose;
  /**
   * Function that will be called when an error occures.
   */
  Function onerror;
  /**
   * Create a new connection to a XSockets.NET server.
   * @param url XSockets.NET server url i.e ws://10.0.0.1:80.
   * @param controller Controller to connect.
   * @returns New instace of XSockets
   */
  
  ClientInfo _clientInfo;
  
  XSockets(String url, String controller) {
      this._controller = controller;
      this._url = url;
      
      _clientInfo = new ClientInfo();
      
      _subscriptions = new Subscriptions();
      
      _subscriptions.add("0x14", (a) {
        
        Map map = JSON.decode(a);
        _clientInfo.set(map["PI"],map["CI"],map["C"]);
        
        
        if (this.onopen is Function) this.onopen(a);
      });
      _subscriptions.add("0x1f4", (ex){
        if(this.onerror is Function) this.onerror(ex);
      });
      
      Map _p = _clientInfo.get(this._controller);
       if(_p.isNotEmpty)
          this._url = this._url.contains('?') ? this._url = this._url + "&persistentId="  +_p["persistentId"] : 
            this._url = this._url + "?persistentId="  +_p["persistentId"];
    
      ws = new WebSocket(this._url, "XSockets.NET");
        
      
      ws.onMessage.listen((e) {
        Map map = JSON.decode(e.data);
        var _subscription = _subscriptions.find(map["T"]);
        _subscription.fn(map["D"]);
      });
      
      ws.onClose.listen((e){
        if(this.onclose is Function) this.onclose(e);
      });
      
      ws.onError.listen((ex) {
        if(this.onerror is Function) this.onerror(ex);
      });
      
      ws.onOpen.listen((e) {
        ws.send(new XSocketsMessage(this._controller, "0xcc", {}).toString());
      });
    }
    /**
     * Adds an listener for the specified topic and fires the Function (fn) provided when
     * a message receives on the topic. Server will be able to invoke this 'topic'
     * @param topic The topic of the message to 'listen' to.
     * @param fn Function to called  when a message is received.
     */
  XSockets on(String topic, Function fn) {
      this._subscriptions.add(topic, fn);
      return this;
    }
    /**
     * Remove a listener for the specified topic
     * @param topic Name of the topic to stop listen to.
     */
  XSockets off(String topic) {
      this._subscriptions.remove(topic);
      return this;
    }
    /**
     * Add a subscription that the server will be aware of.
     * @param The topic to subscribe to.
     * @param fn Function to called  when a message is received.
     */
  XSockets subscribe(String topic, Function fn) {
      var message = new XSocketsMessage(this._controller, "0x12c", {
        "T": topic
      });
      ws.send(message.toString());
      this._subscriptions.add(topic, fn);
      return this;
    }
    /**
     * Terminate a subscription on a specific topic. Server will be notified when this
     * methos is invoked.
     * @param Name of the topic ( subscription ) to remove
     */
  XSockets unsubscribe(String topic) {
      var message = new XSocketsMessage(this._controller, "0x12c", {
        "T": topic,
        "A": false
      });
      ws.send(message.toString());
      this._subscriptions.remove(topic);
      return this;
    }
    /**
     * Invoke a method on the controller specified.
     * @param topic Name of the topic (method) on the controller to invoke
     */
  XSockets invoke(String topic, Object data) {
      var message = new XSocketsMessage(this._controller, topic, data);
      ws.send(message.toString());
      return this;
    }
    /**
     * Publish a message on a specific topic.
     * @param topic Publish a message on the spcified topic.
     * @param data Message payload to pass to the contoller on the specified topic.
     */
  XSockets publish(String topic, Object data) {
      this.invoke(topic, data);
      return this;
    }
  /**
   * Close the connection to the XSockets.NET server an contoller
   */
  XSockets close() {
    ws.close();
    return this;
  }
}