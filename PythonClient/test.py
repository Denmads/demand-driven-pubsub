import paho.mqtt.client as mqtt
import time

def on_connect(client, userdata, flags, rc):
    print("Connected with result code "+str(rc))
    client.subscribe("income")

def on_message(client, userdata, msg):
    print(f'Received message on topic {msg.topic}: {msg.payload}')
    if msg.topic == "income":
        client.publish("receive","got it!")

client = mqtt.Client()
client.on_connect = on_connect
client.on_message = on_message

client.connect("localhost", 1883, 60) # replace localhost with the IP address of your broker

while True:
    client.loop()
    client.publish("heartbeat", "beat")
    time.sleep(5)