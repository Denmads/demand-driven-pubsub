import paho.mqtt.client as mqtt

# The callback for when a PUBLISH message is received from the server.
def on_message(client, userdata, msg):
    print(msg.topic+" | "+str(msg.payload))

client = mqtt.Client()
client.on_message = on_message

client.connect("localhost", 1883, 60)

# Blocking call that processes network traffic, dispatches callbacks and
# handles reconnecting.
# Other loop*() functions are available that give a threaded interface and a
# manual interface.
client.loop_start()

mes = ""

while mes != "quit":
    mes = input("Action> ")
    
    tokens = mes.split()
    
    if tokens[0] == "subscribe":
        client.subscribe(tokens[1])
    else:
        client.publish(tokens[1], tokens[2])

client.loop_stop()