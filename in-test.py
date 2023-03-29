from threading import Thread
from PythonClient.BaseClient import BaseClient
import asyncio
import sys

mode = sys.argv[1]

def on_data_received(data):
    print(data)
    
def connect_client(client: BaseClient):
    client.connect_to_broker()
    
    while not client.connected:
            pass

    thread = Thread(target=client.start_heartbeat)
    thread.daemon = True
    thread.start()

if mode == "s":
    client = BaseClient("sub1")
    connect_client(client)
    
    client.give_cypher("MATCH (st:Sensor {type: 'temperature'}) MATCH (sc:Sensor {type: 'co2'})")
    client.target_node = ["st", "sc"]
    print("subbed test")
    
    
    client.send_sub_query(on_data_received, "sub1")
    
    input()
    
elif mode == "p1":
    client = BaseClient("publisher1")
    connect_client(client)
    
    print("connected")
    client.give_cypher("""
            MERGE (b:Building {name: 'OU44'})
            MERGE (f:Floor {name: 'ground', level: 0})
            MERGE (r:Room {name: 'Ø32-602b-2'})
            MERGE (s:Sensor {type: 'temperature'})
            MERGE (b)-[:has]->(f) 
            MERGE (f)-[:has]->(r) 
            MERGE (r)-[:contains]->(s)""")
    client.target_node = "s"
    client.data_type = "int"
    client.send_pub_query("pub1")
    
    user_input = ""
    while True:
        user_input = input("Send: ")
        if user_input == "quit":
            break
        
        next_val = int(user_input)
        client.publishData(next_val, "pub1")

elif mode == "p2":
    client = BaseClient("publisher2")
    connect_client(client)
    
    print("connected")
    client.give_cypher("""
            MERGE (b:Building {name: 'OU44'})
            MERGE (f:Floor {name: 'ground', level: 0})
            MERGE (r:Room {name: 'Ø32-602b-2'})
            MERGE (s:Sensor {type: 'co2'})
            MERGE (b)-[:has]->(f) 
            MERGE (f)-[:has]->(r) 
            MERGE (r)-[:contains]->(s)""")
    client.target_node = "s"
    client.data_type = "int"
    client.send_pub_query("pub1")
    
    user_input = ""
    while True:
        user_input = input("Send: ")
        if user_input == "quit":
            break
        
        next_val = int(user_input)
        client.publishData(next_val, "pub1")