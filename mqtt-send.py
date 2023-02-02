import json
import paho.mqtt.client as mqtt

# The callback for when a PUBLISH message is received from the server.
def on_message(client, userdata, msg):
    print(msg.topic+" | "+str(msg.payload))

client = mqtt.Client()
client.on_message = on_message

client.connect("localhost", 1883, 60)

# Blocking call that processes network traffic, dispatches callbacks and
# handles reconnecting.
# Other loop*() functions are available that give a threaded interface and a
# manual interface.
client.loop_start()

mes = ""

while mes != "quit":
    mes = input("Action> ")
    
    if len(mes) == 0: continue
    
    tokens = mes.split()
    
    if tokens[0] == "connect": #connect clientid
        payload = {
            "ClientId": tokens[1],
            "ConnectionTimeout": 1000
        }
        client.publish("ddps/system/clientmanager/connect", f"connect<>{json.dumps(payload)}")
    elif tokens[0] == "query" and tokens[1] == "publish": #query publish clientid type
        payload = {
            "RequestId":1,
            "CypherQuery":f"MERGE(r:Room {{name:'my-room'}}) MERGE(s:Sensor {{type:'{tokens[3]}'}}) MERGE (r)-[:Contains]->(s)",
            "TargetNode":"s",
            "DataType":"float"
        }
        client.publish(f"ddps/system/{tokens[2]}/query", f"publish<>{json.dumps(payload)}")
    elif tokens[0] == "query" and tokens[1] == "subscribe": #query subscribe clientid name:type name:type ...
        targets = list(map(lambda x: x.split(":") ,tokens[3:]))
        
        query = "MATCH(r:Room {name:'my-room'})"
        for trg in targets:
            query += f"""
                 MATCH ({trg[0]}:Sensor {{type:'{trg[1]}'}})
                MATCH (r)-[:Contains]->({trg[0]})
            """
        
        payload = {
            "RequestId":1,
            "CypherQuery": query,
            "TargetNodes":list(map(lambda x: x[0] ,targets))}
        client.publish(f"ddps/system/{tokens[2]}/query", f"subscribe<>{json.dumps(payload)}")
    elif tokens[0] == "subscribe": #subscribe topic
        client.subscribe(tokens[1])
        print(f"Subscribe to '{tokens[1]}'")
    else: #publish topic data(can contain space)
        client.publish(tokens[1], " ".join(tokens[2:]))
        print(f"Published '{tokens[2]}'")

client.loop_stop()