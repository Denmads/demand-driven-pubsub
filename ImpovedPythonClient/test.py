import yaml


def create_full_cypher(room_name, floor_name, floor_level, building_name, sensor_type, sensor_id):
    cypher = """
    MERGE (b:Building {{name: {0}}}
    MERGE (f:Floor {{name: {1}, level: {2}}})
    MERGE (r:Room {{name: {3}}})
    MERGE (s:Sensor {{type: {4}}})
    MERGE (b)-[:has]->(f) 
    MERGE (f)-[:has]->(r) 
    MERGE (r)-[:contains]->(s)""".format(building_name, floor_name, floor_level, room_name, sensor_type, sensor_id)
    return cypher

testfile = "test1"

parth = "tests/" + testfile + ".yml"


with open(parth, "r") as file:
    data = yaml.load(file, Loader=yaml.FullLoader)
    clients = data["clients"]

for client in clients:
    c = clients[client]

    if(c["publish"]):
        location = c["location"]
        
        if(len(location) == 4):
            cypher = create_full_cypher(str(location["room"]), str(location["floor-name"]), str(location["floor"]), str(location["building"]), str(c["type"]), 0)
            print(cypher)