#include <PhotonSocketClient.h>

//CONTROLLER INFO
const String INIT = "1";
const String OPENED = "2";
const String CLOSED = "3";
const String ERR = "4";
//PUBSUB
const String SUBSCRIBE = "5";
const String UNSUBSCRIBE = "6";
// HEARTBEAT
const String PING = "7";
const String PONG = "8";
//STORAGE
const String STORAGESET = "s1";
const String STORAGEGET = "s2";
const String STORAGEREMOVE = "s3";
const String STORAGECLEAR = "s4";
//HANDSHAKE
const String HANDSHAKE = "PhotonProtocol";

void PhotonSocketClient::SetClientId(String guid) {
	clientId = guid;
}

bool PhotonSocketClient::connect(char hostname[], int port) {
	bool result = false;
	if (_client.connect(hostname, port)) {
		sendHandshake();
		result = readHandshake();
	}
	return result;
}

bool PhotonSocketClient::connected() {
	return _client.connected();
}

void PhotonSocketClient::disconnect() {
	_client.flush();
	_client.stop();
}

void PhotonSocketClient::receiveData () {
	char character;

	if(!_client.connected()){
		if (_onDisconnectedDelegate != NULL) {
			_onDisconnectedDelegate();
		}
	}
  //Serial.print(_client.available());
	if (_client.available() > 0 && (character = _client.read()) == 0) {
		String data = "";
		bool endReached = false;
		while (!endReached) {
			character = _client.read();
			endReached = character == (char)255;
			//Serial.print(data);
			if (!endReached) {
				data += character;
			}
		}

		if (_onMessageDelegate != NULL) {
			_onMessageDelegate(*this, data);
		}
		//dispatch(data);

	}
	//else{
		//Serial.println("Nothing to read...");
	//}
}

void PhotonSocketClient::dispatch(String data){
  String topic = getValueAtIx(data, 1);

	if(topic == STORAGEGET) {

		Serial.println("Got data from storage");
	}
	else {
		if (_onMessageDelegate != NULL) {
			_onMessageDelegate(*this, data);
		}
	}
}

void PhotonSocketClient::setOnMessageDelegate(OnMessageDelegate onMessageDelegate) {
	_onMessageDelegate = onMessageDelegate;
}

void PhotonSocketClient::setOnDisconnectedDelegate(OnDisconnectedDelegate onDisconnectedDelegate) {
	_onDisconnectedDelegate = onDisconnectedDelegate;
}

void PhotonSocketClient::setOnConnectedDelegate(OnConnectedDelegate onConnectedDelegate) {
	_onConnectedDelegate = onConnectedDelegate;
}


void PhotonSocketClient::sendHandshake() {

 	if(clientId == NULL)
		_client.print(HANDSHAKE);
	else{
		_client.print(HANDSHAKE + "?PersistentId=" + clientId);
	}
	_client.flush();
}

bool PhotonSocketClient::readHandshake() {
	bool result = false;
	char character;
	String handshake = "", line;
	int maxAttempts = 300, attempts = 0;

	while(_client.available() == 0 && attempts < maxAttempts)
	{
		delay(100);
		attempts++;
	}

	line = readHandshakeLine();
	Serial.println(line);
	if(line != "welcome") {
		Serial.println("handshake bad");
		_client.stop();
		return false;
	}
	else{
		if (_onConnectedDelegate != NULL) {
			_onConnectedDelegate();
		}
	}
	//Serial.println("handshake ok");
	return true;
}

String PhotonSocketClient::readHandshakeLine() {
	String line = "";
	char character;
	while(_client.available() > 0 && (character = _client.read()) != (char)255) {
		if (character != (char)0 && character != -1) {
			line += character;
		}
	}
	return line;
}

void PhotonSocketClient::transmit (String controller, String topic, String data){
	if(!_client.connected())return;

	_client.print((char)0);
	_client.print(controller);
	_client.print("|");
	_client.print(topic);
	_client.print("|");
	_client.print(data);
	_client.print((char)255);
	_client.flush();
}

void PhotonSocketClient::send (String controller, String topic, String data) {
	transmit(controller, topic, data);
}

void PhotonSocketClient::openController(String controller){
	transmit(controller, INIT,"");
}

void PhotonSocketClient::closeController(String controller){
	transmit(controller, CLOSED,"");
}

void PhotonSocketClient::storageSet(String controller, String key, String value){
	transmit(controller, STORAGESET, key +","+value);
}
void PhotonSocketClient::storageGet(String controller, String key){
	transmit(controller, STORAGEGET, key);
}
void PhotonSocketClient::storageRemove(String controller, String key){
	transmit(controller, STORAGEREMOVE, key);
}
void PhotonSocketClient::storageClear(String controller){

}

void PhotonSocketClient::addListener (String controller, String topic) {
	transmit(controller, SUBSCRIBE, topic);
}

void PhotonSocketClient::removeListener (String controller, String topic) {
	transmit(controller, UNSUBSCRIBE, topic);
}

String PhotonSocketClient::getValueAtIx(String data, int index){
	int found = 0;
	int strIndex[] = { 0, -1  };
	int maxIndex = data.length()-1;

	for(int i=0; i<=maxIndex && found<=index; i++){
    		if(data.charAt(i)=='|' || i==maxIndex){
      			found++;
      			strIndex[0] = strIndex[1]+1;
      			strIndex[1] = (i == maxIndex) ? i+1 : i;
    		}
  	}
  	return found>index ? data.substring(strIndex[0], strIndex[1]) : "";
}
