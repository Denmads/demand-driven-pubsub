from PythonClient.Client import Client
import asyncio

mode = input("What Mode (p1/p2/s)? ")

def on_data_received(data):
    print(data)

if mode == "s":
    client = Client("sub1")
    client.connect_to_broker()

    while not client.connected:
        pass
    
    client.give_cypher("MATCH (st:Sensor {type: 'temperature'}) MATCH (sc:Sensor {type: 'co2'})")
    client.target_node = ["st", "sc"]
    print("subbed test")
    
    
    client.send_sub_query(on_data_received)
    
    input()
    
elif mode == "p1":
    client = Client("publisher1")
    client.connect_to_broker()

    print("waiting")
    while not client.connected:
        pass
    
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
    client = Client("publisher2")
    client.connect_to_broker()

    print("waiting")
    while not client.connected:
        pass
    
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