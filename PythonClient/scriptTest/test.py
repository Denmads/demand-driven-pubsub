from pydoc_data.topics import topics
import paho.mqtt.client as mqtt
import time
import select


json_data = {
    "body": {
        "temp": 30,
        "heart": 80
    },
    "timestamp": "2023-01-27T12:00:00Z"
}

heart_value = json_data["body"]["heart"]
print(heart_value)
# Output: 80

