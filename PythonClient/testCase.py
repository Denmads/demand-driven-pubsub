import asyncio
import time
import select
import json
from Client import Client

def create_cypher(room_name, floor_name, floor_level, building_name, sensor_type, sensor_id):
    cypher = """MERGE (b:Building {{name: {0}}}
    MERGE (f:Floor {{name: {1}, level: {2}}})
    MERGE (r:Room {{name: {3}}})
    MERGE (s:Sensor {{type: {4}}})
    MERGE (b)-[:has]->(f) 
    MERGE (f)-[:has]->(r) 
    MERGE (r)-[:contains]->(s)""".format(building_name, floor_name, floor_level, room_name, sensor_type, sensor_id)
    return cypher


async def test():
    cypher = create_cypher("O32-602b-2", "ground", "0", "OU44", "temperature", "id")
    target_node = ["s"]

    c = Client("temp4")
    c.cypher = cypher
    c.target_node = target_node
    c.data_type = "int"
    c.connect_to_broker()
    asyncio.create_task(c.start_heartbeat())
    asyncio.create_task(c.start_loop())
    q = c.send_query()
    print(q)

    while True:
        await asyncio.sleep(1)


if __name__ =="__main__":
    asyncio.run(test())
