import paho.mqtt.client as mqtt
import json
import base64

broker = "localhost"
port = 1883
topic = "ddps/system/account/role"
client = mqtt.Client(42)

RequestId = 0
client.connect(broker, port, 60)


#user input
role = "admin"
username = "admin"
accountUser = "admin"
accountPassword = "admin"

encodedAccountPassword = base64.b64encode(accountPassword.encode("utf-8"))

publishString = "add<> " + json.dumps({"RequestId": RequestId, "Username": username, "Role": role, "Account": accountUser, "AccountPassword": encodedAccountPassword})

client.publish(topic, publishString)

