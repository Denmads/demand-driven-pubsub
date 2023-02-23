import json
import sys
import paho.mqtt.client as mqtt

if len(sys.argv) != 2:
    print("missing client id as 1. parameter")
    exit()

# The callback for when a PUBLISH message is received from the server.
def on_message(client, userdata, msg):
    print(f"Message on '{msg.topic}':")
    print(str(msg.payload))
    print()

client = mqtt.Client()
client.on_message = on_message

client.connect("localhost", 1883, 60)
# while not client.is_connected():
#     pass



# Blocking call that processes network traffic, dispatches callbacks and
# handles reconnecting.
# Other loop*() functions are available that give a threaded interface and a
# manual interface.
client.loop_start()

while not client.is_connected():
    pass
print("Connected to broker.\n")

client.subscribe(f"ddps/system/{sys.argv[1]}/query")
client.subscribe(f"ddps/system/{sys.argv[1]}/response")

input()

client.loop_stop()