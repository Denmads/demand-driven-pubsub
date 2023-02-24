from Client import Client
import threading

class Interface:
    client = null
    cypher = ""

    def publish_query(self, cypher, target_node, data_type, publish_id):
        client.give_cypher(cypher)
        client.target_node = target_node
        client.data_type = data_type
        client.send_pub_query(publish_id)

    def subscribe_query(self, cypher, target_node, on_data_received):
        client.give_cypher(cypher)
        client.target_node = target_node
        client.send_sub_query(on_data_received)

    def create_client(self, id):
        self.client = Client(id)
        client.connect_to_broker()

        while not client.connected:
            pass

        thread = threading.Thread(self.client.start_heartbeat())
        thread.daemon = True
        thread.start()

    def publish_data(self, data, topic_id):
        self.client.publishData(data, topic_id)

    