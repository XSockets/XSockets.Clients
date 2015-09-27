#ifndef XSOCKETCLIENT_H
#define XSOCKETCLIENT_H_
#include <application.h>

class PhotonSocketClient {
	public:
		typedef void (*OnMessageDelegate)(PhotonSocketClient client, String data);
		typedef void (*OnDisconnectedDelegate)();
		typedef void (*OnConnectedDelegate)();
		void SetClientId(String guid);
		bool connect(char hostname[], int port = 80);
        bool connected();
        void disconnect();
		void receiveData();
		void setOnMessageDelegate(OnMessageDelegate onMessageDelegate);
		void setOnDisconnectedDelegate(OnDisconnectedDelegate onDisconnectedDelegate);
		void setOnConnectedDelegate(OnConnectedDelegate onConnectedDelegate);

		void send(String controller, String topic, String data);

		void openController(String controller);
		void closeController(String controller);

		void storageSet(String controller, String key, String value);
		void storageGet(String controller, String key);
		void storageRemove(String controller, String key);
		void storageClear(String controller);

		void addListener(String controller, String topic);
		void removeListener(String controller, String topic);
		String getValueAtIx(String data, int index);
	private:
				void transmit(String controller, String topic, String data);
				void dispatch(String data);
        void sendHandshake();
        TCPClient _client;
				String clientId = NULL;
        OnMessageDelegate _onMessageDelegate;
				OnDisconnectedDelegate _onDisconnectedDelegate;
				OnConnectedDelegate _onConnectedDelegate;
        bool readHandshake();
        String readHandshakeLine();
};

#endif
