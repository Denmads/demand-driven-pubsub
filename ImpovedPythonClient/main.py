from gateway import Gateway
import os
import sys
import yaml


file1 = sys.argv[1] if len(sys.argv) > 1 else None
file2 = sys.argv[2] if len(sys.argv) > 2 else None
script = sys.argv[3] if len(sys.argv) > 3 else None

def get_function(file_name):
    module_name = os.path.splitext(script)[0]
    module = __import__(module_name)
    with open(file_name, "r") as file:
        data = yaml.load(file, Loader=yaml.FullLoader)
        f = data["function"]
        method = getattr(module, f, None)
        return method    

def readFile(clientFile):
    with open(clientFile, "r") as file:
        data = yaml.load(file, Loader=yaml.FullLoader)
        return data


if file1 and file2 and script:
    clientData = readFile(file2)
    gateway = Gateway(clientData["client"]["name"], file1)
    
    for sub in clientData["client"]["subscribers"]:
        print(sub)

    for pub in clientData["client"]["publishers"]:
        print(pub)

else:
    print("Invalid input, please provide two file names first broker info, second client info and a Python script name.")
