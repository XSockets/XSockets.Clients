## XSockets.Arduino
We have written a very very simple client for the Arduino.

###Client Setup
Download the Arduino client from GitHub and then add the library as shown here http://arduino.cc/en/Guide/Libraries

###How to establish a connection
Use the `connect` method to open a connection. The method returns false if the connection failed and true if it was ok.

	client.connect("192.168.0.108",4502)
    
- The first parameter is the IP of the server
- The second parameter is the port of the server

###How to send messages
The open method just opens the socket. When you communicate to a specific `controller` for the first time the controller will be created on the server. 

You always pass in `controller`, `topic` and `data` when sending from Arduino.

    client.send("chat","message","Hello from Arduino");
    
The code sample above would invoke the method "Message" on the controller "Chat" with the object/data "Hello from Arduino".

###How to receive messages
Set a message delegate to be able to receive data. This delegate will receive all data sent to the client from the server.

	//Set onMessage to handle data
	client.setOnMessageDelegate(onMessage);

	//The onMessage implementation
	void onMessage(XSocketClient client, String data) {
	  //Just write to serial port
	  Serial.println(data);
	}	

###Subscribe
If you use pub/sub in XSockets you will have to tell the server that you subscribe for specific topics.

	client.addListener("controllername","topic");

###Unsubscribe
When you wan to unsubscribe for a topic just use

	client.removeListener("controllername","topic");

###A Sample...
The code below connects to a XSockets server on 192.168.1.2 and port 4502.
If the connection was a success the client send a message (that will also fire the controller generic to be created).

Data received will be handled by the `onMessage` method.

	#include "Arduino.h"
	#include <Ethernet.h>
	#include <SPI.h>
	#include <XSocketClient.h>

	byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED };
	XSocketClient client;

	void setup() {
	  Serial.begin(9600);
	  Ethernet.begin(mac);
	  Serial.println("Start to connect");
	  if(client.connect("192.168.0.108",4502)){
	      Serial.println("Connected!");
	  }
	  else{
	      Serial.println("Error connecting");
	  }
  
	  client.setOnMessageDelegate(onMessage);
	  delay(1000);
  
	  client.send("generic","hello","Arduino connected");
	}

	void loop() {
	  client.receiveData();
  
	  delay(500);
	}

	void onMessage(XSocketClient client, String data) {
	  Serial.println(data);
	}

TIP: Test this in an easy way by using Putty like this

 1. Start XSockets.
 2. Connect the Arduino (by running sample above), but remember to change IP if needed.
 3. Open putty on the same machine as XSockets
 4. Enter 127.0.0.1 and port 4502
 5. Choose RAW as connection type
 6. When putty opens type "PuttyProtocol" and hit enter
 7. You are now connected....
 8. type `generic|message|Hello from putty`
 9. If all goes well you will see the message at the Arduino side.


