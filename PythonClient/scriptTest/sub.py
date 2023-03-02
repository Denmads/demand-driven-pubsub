import paho.mqtt.client as mqtt

 
# Callback function when a message is received on the subscribed topic
def on_message(client, userdata, message):
    print("Received message: " + str(message.payload.decode("utf-8")))

# MQTT client setup
client = mqtt.Client()
client.connect("localhost", 1883, 60)

# Get the topic to subscribe to from user input
topic = input("Enter the topic to subscribe to: ")
#topic = "ddps/system/clientmanager/connect"

# Subscribe to the topic
client.subscribe(topic)

# Set the callback function for when a message is received
client.on_message = on_message

# Start the MQTT client loop
client.loop_forever()