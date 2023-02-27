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

def create_sub_cypher(room_name):
    cypher = """
    MATCH (sen:Sensor {{type: “temperature” }})-[:Where]->(r:Room {{type: "{0}"}})
    """.format(room_name)

    return cypher

def p(string):
    print(string)


#testYAML = input("What test to run? ")
testYAML = "test3"

with open("tests/"+testYAML+".yml", "r") as file:
    data = yaml.load(file, Loader=yaml.FullLoader)
    clients = data["clients"]

for client in clients:
    print(clients[client])
    c = clients[client]
    location = c["location"]
    cypher = ""
    if(c["publish"]):

        if(len(location) == 4):
            cypher = create_full_cypher(str(location["room"]), str(location["floor-name"]), str(location["floor"]), str(location["building"]), str(c["type"]), 0)
    else:
        cypher = create_sub_cypher(location["room"])
    print(cypher)