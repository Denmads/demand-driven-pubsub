import yaml
from client import Client
import threading

class Gateway:

    def __init__(self, name, clientFileParth):
        self.name = name
        self.create_client(clientFileParth, name)

        self.client.connect_to_broker()
        thread = threading.Thread(self.client.start_heartbeat())
        thread.daemon = True
        thread.start()
        
        thread2 = threading.Thread(self.client.start_loop())
        thread2.daemon = True
        thread2.start()

    def publish_query(self, cypher, target_node, data_type, publish_id):
        self.client.send_pub_query(publish_id, cypher, target_node, data_type)

    def subscribe_query(self, cypher, target_node, on_data_received_function, transformations=None):
        self.client.send_sub_query(on_data_received_function, 5, cypher, target_node, transformations)

    def create_client(self, clientFileParth, clientId):
        with open(clientFileParth, "r") as file:
            try:
                data = yaml.load(file, Loader=yaml.FullLoader) 

                broker = data["broker"]
                port = data["port"]
                prefix = data["prefix"]

                user = data["user"]
                password = data["password"]
                self.client = Client(clientId, broker, port, prefix, user, password)
            except:
                print("file for broker config is wrong")

    def publish_data(self, publish_id, data):
        self.client.publishData(data, publish_id)
