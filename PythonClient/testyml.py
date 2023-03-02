from Client import Client
import yaml
import random

def create_cypher(room_name, floor_name, floor_level, building_name, sensor_type, sensor_id):
    cypher = """    MERGE (b:Building {{name: {0}}}
    MERGE (f:Floor {{name: {1}, level: {2}}})
    MERGE (r:Room {{name: {3}}})
    MERGE (s:Sensor {{type: {4}}})
    MERGE (b)-[:has]->(f) 
    MERGE (f)-[:has]->(r) 
    MERGE (r)-[:contains]->(s)""".format(building_name, floor_name, floor_level, room_name, sensor_type, sensor_id)
    return cypher

def publish_random_int(client):
    random_int = random.randint(0, 20)
    client.publishData(random_int, "pub1")

with open("test1.yml", "r") as file:
    data = yaml.load(file, Loader=yaml.FullLoader)
    print(data)

test = data["test"]
temperatureSensor = test["temperatureSensor"]
location = temperatureSensor["location"]
sensor = temperatureSensor["sensor"]

cypher = create_cypher(location["room"], location["floor-name"], location["floor"], location["building"], sensor["type"], "id")

print(cypher)


c = Client(temperatureSensor["name"])
c.print()
c.connect_to_broker()

client.give_cypher(cypher)
client.target_node = "s"
client.data_type = sensor["int"]
client.send_pub_query(temperatureSensor["name"])


while True:
    c.client.loop()

    while not client.connected:
        pass

    c.client.publish(c.heartbeat_topic, f"beat")

    ready = select.select([c.client._sock],[],[],c.heartbeat_interval)
    ready = select.select([c.client._sock],[],[],5)
    if ready[0]:
        c.client.loop() # handle incoming messages
    else:
        pass


    