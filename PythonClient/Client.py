import paho.mqtt.client as mqtt
import time

class Client:
    def __init__(self, id):
        self.id = id
        self.broker = "localhost"
        self.port = 1883
        self.publish_topic = ""
        self.subscribe_topic = ""
        self.response_topic = f"ddps/system/{self.id}/response"
        
        self.connect_topic = f"ddps/system/clientmanager/connect"

        self.heartbeat_topic = f"ddps/system/{self.id}/heartbeat"
        self.heartbeat_interval = 5

        self.publish_id = ""
        self.chyper = ""
        self.return_value = ""
        self.return_value = ""

        self.client = mqtt.Client()

    def print(self):
        print(self.heartbeat_topic)

    def connect_to_broker(self):
        self.client.connect(self.broker, self.port, 60)

        self.client.publish(self.connect_topic, self.id)
    
    def on_connect(self, client, userdata, flags, rc):
        client.subscribe("income")

    def on_message(client, userdata, msg):
        print(f'Received message on topic {msg.topic}: {msg.payload}')
        if msg.topic == "income":
            client.publish("receive","got it!")

if __name__ =="__main__":
    c = Client("temp4")
    c.print()
    c.connect_to_broker()
    c.on_connect(c, userdata, flags, rc)

    i = 0
    while i < 100:
        c.client.loop()
        c.client.publish("heartbeat", "beat")
        time.sleep(5)

    c.client.disconnect()
    