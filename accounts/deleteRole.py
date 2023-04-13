import paho.mqtt.client as mqtt
import json
import base64

broker = "localhost"
port = 1883
topic = "ddps/system/account/role"
client = mqtt.Client("someClient")

RequestId = 0
client.connect(broker, port, 60)


#user input
role = "LowLevel"
username = "mhjtest"
accountUser = "admin"
accountPassword = "admin"

encodedAccountPassword = base64.b64encode(accountPassword.encode("utf-8")).decode("utf-8")

publishString = "remove<> " + json.dumps({"ClientId": "seomClient", "RequestId": RequestId, "Username": username, "Role": role, "Account": accountUser, "AccountPassword": encodedAccountPassword})

client.publish(topic, publishString)

