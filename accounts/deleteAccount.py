import paho.mqtt.client as mqtt
import json
import base64

broker = "localhost"
port = 1883
topic = "ddps/system/account"
client = mqtt.Client("userClient")

RequestId = 0
client.connect(broker, port, 60)


#user input
username = "mhjtest"
accountUser = "admin"
accountPassword = "admin"

encodedAccountPassword = base64.b64encode(accountPassword.encode("utf-8")).decode("utf-8")

publishString = "delete<> " + json.dumps({"ClientId": "userClient", "RequestId": RequestId, "Username": username, "Account": accountUser, "AccountPassword": encodedAccountPassword})

client.publish(topic, publishString)

