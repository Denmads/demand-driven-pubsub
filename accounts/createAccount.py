import paho.mqtt.client as mqtt
import json
import base64

broker = "localhost"
port = 1883
topic = "ddps/system/account"
client = mqtt.Client("someClient")

RequestId = 0
client.connect(broker, port, 60)


#user input
username = "mhjtest"
password = "test1234"
accountUser = "admin"
accountPassword = "admin"


encodedPassword = base64.b64encode(password.encode("utf-8")).decode("utf-8")
encodedAccountPassword = base64.b64encode(accountPassword.encode("utf-8")).decode("utf-8")

publishString = "create<> " + json.dumps({"ClientId": "userClient", "RequestId": RequestId, "Username": username, "Password": encodedPassword, "Account": accountUser, "AccountPassword": encodedAccountPassword})

client.publish(topic, publishString)

