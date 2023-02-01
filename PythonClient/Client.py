import paho.mqtt.client as mqtt
import time
import select
import json

class Client:
    def __init__(self, id, connectionTimeout=60):
        self.id = id
        self.broker = "localhost"
        self.port = 1883
        self.publish_topic = ""
        self.subscribe_topic = ""
        self.response_topic = f"ddps/system/{self.id}/response"
        
        self.connect_topic = f"ddps/system/clientmanager/connect"
        self.query_topicc = f"ddps/system/{self.id}/query"

        self.connectionTimeout = connectionTimeout
        self.heartbeat_topic = f"ddps/system/{self.id}/heartbeat"
        self.heartbeat_interval = 10

        self.request_id = 1 # goes up when sending a message on query_topic 
        self.chyper = ""
        self.return_value = ""
        self.return_value = ""

        self.client = mqtt.Client()
        self.client.on_connect = self.on_connect
        self.client.on_message = self.on_message

    def print(self):
        print(self.heartbeat_topic)

    def connect_to_broker(self):
        self.client.connect(self.broker, self.port, 60)

        self.client.publish(self.connect_topic, self.id)

    def handleResponse(self, response):
        jsonResponse = response.split("<>")[1]
        j = json.loads(jsonResponse)
        heartbeatInterval = j["HeartbeatInterval"]
        return heartbeatInterval

    def on_message(self, client, userdata, msg):
        payload = msg.payload.decode('utf-8')
        print(f'Received message on topic {msg.topic}: {payload}')
        if msg.topic == "income":
            print("got it")
            client.publish("receive","got it!")
        elif msg.topic == self.response_topic:
            heartbeatInterval = self.handleResponse(payload)
            self.heartbeat_interval = heartbeatInterval

    def on_connect(self, client, userdata, flags, rc):
        print("Connected with result code "+str(rc))
        client.subscribe("income")  
        client.subscribe(self.response_topic)
    


if __name__ =="__main__":
    c = Client("temp4")
    c.print()
    c.connect_to_broker()

    while True:
        c.client.loop()
        c.client.publish(c.heartbeat_topic, f"falmebeat")

        ready = select.select([c.client._sock],[],[],c.heartbeat_interval)
        if ready[0]:
            c.client.loop() # handle incoming messages
        else:
            pass

    