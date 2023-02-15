import threading
import paho.mqtt.client as mqtt
import time
import select
import json

class Client:
    def __init__(self, id, connectionTimeout=60):
        self.id = id
        self.broker = "localhost"
        self.port = 1883
        self.publish_topic = []
        self.subscribe_topics = []
        self.response_topic = f"ddps/system/{self.id}/response"
        
        self.connect_topic = f"ddps/system/clientmanager/connect"
        self.query_topicc = f"ddps/system/{self.id}/query"

        self.connectionTimeout = connectionTimeout
        self.heartbeat_topic = f"ddps/system/{self.id}/heartbeat"
        self.heartbeat_interval = 10

        self.subscriptionId = {}
        self.subscriptionIdCount = 0

        self.requestToPublishid = {}
        self.publishIds = {}

        self.request_id = 0 # goes up when sending a message on query_topic 
        self.requests = {}

        self.cypher = ""
        self.target_node = []
        self.data_type = "int"
        self.return_value = ""

        self.connected: bool = False

        self.client = mqtt.Client()
        self.client.on_connect = self.on_connect
        self.client.on_message = self.on_message
        
        # t = threading.Thread(target=self.start_loop)
        # t.setDaemon(True)
        # t.start()

    def print(self):
        print(self.heartbeat_topic)

    def parse(self, dataType, message):
        if dataType == "string":
            return message
        elif dataType == "int":
            return int(message)
        elif dataType == "float":
            return float(message)
        elif dataType == "bool":
            return bool(message)

    def connect_to_broker(self):
        self.client.connect(self.broker, self.port, 60)
        self.client.loop_start()

        self.client.publish(self.connect_topic, "connect<>" + json.dumps({"ClientId": self.id, "ConnectionTimeout": self.connectionTimeout}))

    def handleResponse(self, response):
        response_type = response.split("<>")[0]
        jsonResponse = response.split("<>")[1]
        j = json.loads(jsonResponse)
        if response_type == "query-result":
            topic = j["Topic"]
            request_id = j["RequestId"]
            requestType = self.requests[(request_id,)]
            if requestType == "publish":
                publishId = self.requestToPublishid[(request_id,)]
                self.publishIds[publishId] = topic
                self.add_publish_topic(topic)
            elif requestType == "subscribe":
                self.add_subscirbe_topic(topic)

        elif response_type == "connect-ack":
            self.heartbeatInterval = j["HeartbeatInterval"]
            # t = threading.Thread(target=self.start_heartbeat)
            # t.setDaemon(True)
            # t.start()
            self.connected = True

    def handleDataReturn(self, payload):
        jsonResponse = payload.split("<>")[1]
        j = json.loads(jsonResponse)
        subscriptionId = j["subscriptionId"]
        data =  j["Data"]
        returnObject = {}
        for nodeName,nodeValue in data.items():
            returnObject[nodeName] = self.parse(nodeValue["DataType"], nodeValue["Value"])

        callback = self.subscriptionId[(subscriptionId,)]
        callback(returnObject)

    def on_message(self, client, userdata, msg):
        payload: str = msg.payload.decode('utf-8')
        print(f'Received message on topic {msg.topic}: {payload}')
        if msg.topic == self.response_topic:
            self.handleResponse(payload)
        
        else:
            self.handleDataReturn(payload)

        #for subTopic in self.subscribe_topics:
        #    if msg.topic == subTopic:
        #        self.handleDataReturn(payload)

    def on_connect(self, client, userdata, flags, rc):
        print("Connected with result code "+str(rc))
        client.subscribe("income")  
        client.subscribe(self.response_topic)

    def add_publish_topic(self, topic):
        self.publish_topic.append(topic)
    
    def add_subscirbe_topic(self, topic):
        self.subscribe_topics.append(topic)
        self.client.subscribe(topic)

    def give_cypher(self, cypher):
        self.cypher = cypher

    def send_pub_query(self, publishId):
        self.requests[(self.request_id, )] = "publish"
        self.requestToPublishid[(self.request_id, )] = publishId
        query = """publish<>{{"RequestId": {0}, "CypherQuery": "{1}", "TargetNode": "{2}", "DataType": "{3}" }}""".format(self.request_id, self.cypher, self.target_node, self.data_type)
        self.request_id += 1
        self.client.publish(self.query_topicc, query)
        return query

    def send_sub_query(self, callback):
        self.requests[(self.request_id, )] = "subscribe"
        self.subscriptionId[(self.subscriptionIdCount, )] = callback
        query = """subscribe<>{{"RequestId": {0}, "CypherQuery": "{1}", "TargetNodes": {2}, "SubscriptionId": "{3}" }}""".format(self.request_id, self.cypher, self.target_node, self.subscriptionIdCount)
        print(query)
        self.request_id += 1
        self.subscriptionIdCount += 1
        self.client.publish(self.query_topicc, query)
        return query

    def old_start_heartbeat(self):
        while True:
            self.client.loop()
            self.client.publish(self.heartbeat_topic, f"falmebeat")

            ready = select.select([self.client._sock],[],[],self.heartbeat_interval)
            if ready[0]:
                self.client.loop() # handle incoming messages
            else:
                pass

    def publishData(self, data, publishId):
        topic = self.publishIds[publishId]
        if self.publish_topic.__contains__(topic):
            self.client.publish(topic, payload=data)
        else:
            return "not a publish topic"

    def start_heartbeat(self):
        while True:
            self.client.publish(self.heartbeat_topic, f"beat")
            time.sleep(self.heartbeat_interval)
           
    def start_loop(self):
        while True:
            print("loop")
            self.client.loop()
            time.sleep(1)

if __name__ =="__main__":
    c = Client("temp4")
    c.print()
    c.connect_to_broker()

    while True:
        c.client.loop()
        c.client.publish(c.heartbeat_topic, f"beat")

        ready = select.select([c.client._sock],[],[],c.heartbeat_interval)
        if ready[0]:
            c.client.loop() # handle incoming messages
        else:
            pass

    