import yaml
import random
from MqttBroker import MqttBroker 


def createClient():
    with open("ImpovedPythonClient/broker-config.yml", "r") as file:
        data = yaml.load(file, Loader=yaml.FullLoader)

        yamlData = data

        broker = data["broker"]
        port = data["port"]
        prefix = data["prefix"]
        r = data["roles"]
        roles = [] 
        for role in r:
             roles.append(role)

        user = data["user"]
        password = data["password"]
         
        return [broker, port, prefix, roles, user, password]
        