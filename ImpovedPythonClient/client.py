import MqttBroker as MqttBroker

class Client:
    def __init__(self, id):
        self.id = id
        self.broker = MqttBroker(id)
        
        self.publish_topic = []
        self.subscribe_topics = []