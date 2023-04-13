from Client import Client
import threading

class Interface:
    client = "null"
    cypher = ""

    def publish_query(self, cypher, target_node, data_type, publish_id):
        self.client.give_cypher(cypher)
        self.client.target_node = target_node
        self.client.data_type = data_type
        self.client.send_pub_query(publish_id)

    def subscribe_query(self, cypher, target_node, on_data_received):
        self.client.give_cypher(cypher)
        self.client.target_node = target_node
        self.client.send_sub_query(on_data_received)

    def create_client(self, id):
        self.client = Client(id)
        self.client.connect_to_broker()

        while not self.client.connected:
            pass

        thread = threading.Thread(self.client.start_heartbeat)
        thread.daemon = True
        thread.start()

    def publish_data(self, data, topic_id):
        self.client.publishData(data, topic_id)

    