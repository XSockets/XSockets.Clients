#pragma SPARK_NO_PREPROCESSOR
#include <PhotonSocketClient.h>
#define SERVER_IP "192.168.254.154"//"hackzurich.cloudapp.net"
#define SERVER_PORT 4502

char json[64];
PhotonSocketClient client;

//When a message arrives we take action depending on the topic
void onMessage(PhotonSocketClient client, String data) {
  String topic = client.getValueAtIx(data,1);
  if(topic == "d7_on"){
    digitalWrite(D7, HIGH);
  }
  if(topic == "d7_off"){
    digitalWrite(D7, LOW);
  }
}

//Connect to the XSockets server
void setup()
{
    pinMode(D7, OUTPUT);
    for(int i = 5;i > 0;i--)
    {
        Serial.println("Starting in " + String(i,DEC));
        delay(1000);
    }

    if(client.connect(SERVER_IP,SERVER_PORT)){
      client.openController("generic");
      client.setOnMessageDelegate(onMessage);
    }
    else{
      Serial.println("Could not open connection :(");
    }
}

//Receive data in each loop to fire the onMessage method when data arrives
void loop()
{
  client.receiveData();
  delay(250);
}
