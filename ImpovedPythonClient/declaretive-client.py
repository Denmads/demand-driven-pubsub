import os
import sys
import yaml
from client import Client

client = None

def help():
    print("First arg should be the broker config")
    print("Second arg should be the client config")
    print("Third arg should be the python file with function for subscribe function")

file1 = sys.argv[1] if len(sys.argv) > 1 else None
file2 = sys.argv[2] if len(sys.argv) > 2 else None
script = sys.argv[3] if len(sys.argv) > 3 else None

def read_broker_config(config_file):
    with open(config_file, "r") as file:
        data = yaml.load(file, Loader=yaml.FullLoader)

        broker = data["broker"]
        port = data["port"]
        prefix = data["prefix"]

        user = data["user"]
        password = data["password"]
         
        return [broker, port, prefix, user, password]

def read_client_config(config_file):
    with open(config_file, "r") as file:
        data = yaml.load(file, Loader=yaml.FullLoader)
        return data["client"]

def handle(file1, file2):
    config = read_broker_config(file1)
    client_yaml = read_broker_config(file2)
    client = Client(client_yaml["name"], config[0], config[1], config[2], config[3], config[4])

def get_function(file_name, function_name):
    module_name = os.path.splitext(script)[0]
    module = __import__(module_name)
    with open(file_name, "r") as file:
        data = yaml.load(file, Loader=yaml.FullLoader)
        method = getattr(module, function_name, None)
        return method  

if file1 and file2:
    handle(file1, file2)
else:
    help()

