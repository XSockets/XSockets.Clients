#include <XSocketClient.h>
#include <WString.h>
#include <string.h>
#include <stdlib.h>

const String SUBSCRIBE = "0x12c";
const String UNSUBSCRIBE = "0x12d";

bool XSocketClient::connect(char hostname[], int port) {
	bool result = false;

	if (_client.connect(hostname, port)) {
		sendHandshake();
		result = readHandshake();
	}

	return result;
}


bool XSocketClient::connected() {
	return _client.connected();
}

void XSocketClient::disconnect() {
	_client.stop();
}

void XSocketClient::receiveData () {
	char character;

	if (_client.available() > 0 && (character = _client.read()) == 0) {
		String data = "";
		bool endReached = false;
		while (!endReached) {
			character = _client.read();
			endReached = character == (char)255;

			if (!endReached) {
				data += character;
			}
		}

		if (_onMessageDelegate != NULL) {
			_onMessageDelegate(*this, data);
		}
	}
}

void XSocketClient::setOnMessageDelegate(OnMessageDelegate onMessageDelegate) {
	_onMessageDelegate = onMessageDelegate;
}


void XSocketClient::sendHandshake() {    
	_client.println("TextProtocol");
}

bool XSocketClient::readHandshake() {
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

	if(line != "Welcome to TextProtocol") {
		_client.stop();
		return false;
	}

	return true;
}

String XSocketClient::readHandshakeLine() {
	String line = "";
	char character;
	while(_client.available() > 0 && (character = _client.read()) != (char)255) {
		if (character != (char)0 && character != -1) {		
			line += character;
		}
	}
	return line;
}

void XSocketClient::send (String controller, String topic, String data) {
	_client.print((char)0);
	_client.print(controller);
	_client.print("|");
	_client.print(topic);
	_client.print("|");
	_client.print(data);
	_client.print((char)255);
}

void XSocketClient::addListener (String controller, String topic) {
	_client.print((char)0);
	_client.print(controller);
	_client.print("|");
	_client.print(SUBSCRIBE);
	_client.print("|");
	_client.print(topic);
	_client.print((char)255);
}

void XSocketClient::removeListener (String controller, String topic) {
	_client.print((char)0);
	_client.print(controller);
	_client.print("|");
	_client.print(UNSUBSCRIBE);
	_client.print("|");
	_client.print(topic);
	_client.print((char)255);
}