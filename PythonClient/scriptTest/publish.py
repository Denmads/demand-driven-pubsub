import paho.mqtt.client as mqtt

# MQTT client setup
client = mqtt.Client()
client.connect("localhost", 1883, 60)

# Get the topic and message to publish from user input
topic = input("Enter the topic to publish to: ")
message = input("Enter the message to publish: ")

# Publish the message to the specified topic
client.publish(topic, message)

# Disconnect from the MQTT broker
client.disconnect()