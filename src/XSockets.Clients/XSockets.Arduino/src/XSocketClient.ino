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
  if(client.connect("192.168.1.2",4502)){
      Serial.println("Connected!");
  }
  else{
      Serial.println("Error connecting");
  }
  
  client.setOnMessageDelegate(onMessage);
  delay(2000);
  client.addListener("generic","message","Hello from Arduino");
}

void loop() {
  client.receiveData();
  
  delay(500);
}

void onMessage(XSocketClient client, String data) {
  Serial.println(data);
}