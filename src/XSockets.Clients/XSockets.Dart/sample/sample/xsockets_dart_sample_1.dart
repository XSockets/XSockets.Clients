import 'dart:html';
import '..//lib/XSockets.dart';

void showMessage(message) {
  var el = querySelector("#immediate");
  var p = document.createElement("pre");
  p.text = message;
  el.append(p);
}

void main() {
  var ws = new XSockets("ws://localhost:41514?apa=101", "generic");
  // When we are connected to the XSockets.NET server we will recieve some ClientInfo
  ws.onopen = (connection) {
    showMessage(connection);
    // Subscribe to 'mytopic' , when we got a message display it
    ws.subscribe("mytopic", (msg) {
      showMessage(msg);
    });
  };
  // When the connection is closed or the connection cannot be made.
  ws.onclose = (e){
    showMessage("Closed");
  };
  // If an error occurs one the client / server side this function
  // will be invoked
  ws.onerror = (ex){
    showMessage("An error occured");
  };
  // When a "user" hit's the button, send a message on 'mytopic'
  querySelector("#send").addEventListener("click", (e) {
    ws.publish("mytopic", {
      "a": "Hello fron Dart...."
    });
  });
}