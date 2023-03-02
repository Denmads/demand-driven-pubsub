import random
from a import Interface

class Publisher:

    pubID = 1

    def __init__(self):
        self.a = Interface()

    def start(self):
        
        self.a.create_client("pub2")     
        while True:
            self.a.client.client.loop()
            random_int = random.randint(0, 20)
            print(random_int)
            sleep(2)

    def publish(self, cypher, target_node, data_type):
        self.a.publish_query(cypher, target_node, data_type, pubID)

if __name__ =="__main__":
    p = Publisher()
    p.start()