import threading
import paho.mqtt.client as mqtt
import time
import select
import json

class MqttBroker:
    def __init__(self, id, connectionTimeout=60):
        self.id = id

        self.should_publish = False

        self.connected: bool = False

        self.client = mqtt.Client(self.id)
        self.client.on_connect = self.on_connect
        self.client.on_message = self.on_message
        self.client.loop_start()

    def on_message(self, client, userdata, msg):
        payload: str = msg.payload.decode('utf-8')
        print(f'Received message on topic {msg.topic}: {payload}')
        
    def on_connect(self, client, userdata, flags, rc):
        print("Connected with result code "+str(rc))
        client.subscribe(self.response_topic)
        client.subscribe(self.update_topic)

    def publishData(self, data, topic):
        self.client.publish(topic, payload=data)

    def start_heartbeat(self, heartbeat_topic, heartbeat_interval):
        while True:
            self.client.publish(heartbeat_topic, f"beat", qos=1)
            time.sleep(heartbeat_interval)

    def start_loop(self):
        while True:
            print("loop")
            self.client.loop() 
            time.sleep(1)

    