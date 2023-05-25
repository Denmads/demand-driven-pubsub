from gateway import Gateway
import os
import sys
import yaml
import threading
import random
import time
import json


file1 = sys.argv[1] if len(sys.argv) > 1 else None
file2 = sys.argv[2] if len(sys.argv) > 2 else None
script = sys.argv[3] if len(sys.argv) > 3 else None


def get_function(module_name, function_name, *args, **kwargs):
    module_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), module_name)
    sys.path.append(module_path)
    module = __import__(os.path.splitext(module_name)[0])
    function = getattr(module, function_name, None)
    if not function:
        raise ValueError(f"The function {function_name} does not exist in the module {module_name}.")
    return lambda *f_args, **f_kwargs: function(*args, *f_args, **kwargs, **f_kwargs)


def readFile(clientFile):
    with open(clientFile, "r") as file:
        data = yaml.load(file, Loader=yaml.FullLoader)
        return data

def produce_random_data(produce_id, gateway):
    while True:
        data = random.randint(1, 10)
        print(data)
        gateway.publish_data(produce_id, data )
        time.sleep(2)


if file1 and file2 and script:
    clientData = readFile(file2)
    
    gateway = Gateway(clientData["client"]["name"], file1)

    heartbeat_thread = threading.Thread(target=gateway.start_heartbeat, args=())
    heartbeat_thread.daemon = True
    heartbeat_thread.start()
    
    loop_thread = threading.Thread(target=gateway.start_loop, args=())
    loop_thread.daemon = True
    loop_thread.start()

    time.sleep(5)

    try:
        print("subs:")
        for sub in clientData["client"]["subscribers"]:
            sub_data = clientData["client"]["subscribers"][sub]
            print(sub_data)
            f = get_function(script, sub_data["function"])
            f("heelo")
            transformations = None
            try:
                transformations = sub_data["Transformations"]
            except:
                pass 
            print("get here")
            gateway.subscribe_query(sub_data["cypher"], sub_data["targetNode"], f, sub_data["id"], transformations=transformations)
    except:
        print("no sub")
        pass


    try: 
        print("pubs:")
        for pub in clientData["client"]["publishers"]:
            pub_data = clientData["client"]["publishers"][pub]
            print(pub_data)
            roles =[]
            for role in pub_data["roles"]:
                roles.append(pub_data["roles"][role])
            gateway.publish_query(pub_data["cypher"], pub_data["targetNode"], pub_data["type"], pub_data["id"], roles)
            if pub_data["source"] == "random":
                print("starting producing random data for topic ")
                print(pub_data["id"])
                time.sleep(10)
                producer_thread = threading.Thread(target=produce_random_data, args=(pub_data["id"], gateway))
                producer_thread.daemon = True
                producer_thread.start()
    except:
        print("no pub")
        pass

    while True:
        pass
else:
    print("Invalid input, please provide two file names first broker info, second client info and a Python script name.")


    # python .\main.py .\broker-config.yml .\t\producer1.yml testScript.py
    # python .\main.py .\broker-config.yml .\t\subsribe1.yml testScript.py