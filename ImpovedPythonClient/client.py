import MqttBroker as MqttBroker
import paho.mqtt.client as mqtt
import time
import select
import json


class Client:
    def __init__(self, id, broker, port, prefix, user, password, connectionTimeout=60):
        self.id = id
        self.broker = broker
        self.port = port
        
        self.request_id = 0 # goes up when sending a message on query_topic 

        self.should_publish = True

        #topics
        self.publish_topic = []
        self.subscribe_topics = []
        self.subscriptionId = {}
        self.requestToPublishid = {}
        self.publishIds = {}
        self.requests = {}

        self.create_topics(prefix)

        #for permision to read on topics 
        self.user = user
        self.password = password

        #heatbeat stuff 
        self.heartbeat_interval = 10
        self.connectionTimeout = connectionTimeout

        #start clients 
        self.client = mqtt.Client(id)
        self.client.on_connect = self.on_connect
        self.client.on_message = self.on_message


    def create_topics(self, prefix):
        self.response_topic = f"{prefix}{self.id}/response"

        self.connect_topic = f"{prefix}clientmanager/connect"
        self.query_topicc = f"{prefix}{self.id}/query"

        self.heartbeat_topic = f"{prefix}{self.id}/heartbeat"

        self.update_topic = f"{prefix}{self.id}/updates"

    def parse(self, dataType, message):
        if dataType == "string":
            return message
        else:
            if message == "":
                return None
            elif dataType == "int":
                return int(message)
            elif dataType == "float":
                return float(message)
            elif dataType == "bool":
                return bool(message)
            
    #dont know what the plan with this is right now
    def updateTopic(self):
        pass

    def connect_to_broker(self):
        print("connecting")
        self.client.connect(self.broker, int(self.port))
        
        while not self.client.is_connected():
            self.client.loop()
            print("not connected yet ")
            pass

        print("publish connection message")
        self.client.publish(self.connect_topic, "connect<>" + json.dumps({"ClientId": self.id, "ConnectionTimeout": self.connectionTimeout}), qos=1)

    def handleResponse(self, response):
        response_type = response.split("<>")[0]
        jsonResponse = response.split("<>")[1]
        j = json.loads(jsonResponse)
        print(response_type)
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

        elif response_type == "reconnect-ack":
            print("reconnect")
            self.heartbeatInterval = j["HeartbeatInterval"]
            publishes = j["Publishes"]
            subscriptions = j["Subscriptions"]
            for p in publishes:
                print(p.Id)
                self.publishIds[p.Id] = p.Topic
                self.add_publish_topic(p.Topic)
            
            for s in subscriptions:
                self.add_subscirbe_topic(s.topic)
                self.subscriptionId[s.Id] = s.Topic

        elif response_type == "connect-ack":
            print("connect-ack")
            self.heartbeatInterval = j["HeartbeatInterval"]
            # t = threading.Thread(target=self.start_heartbeat)
            # t.setDaemon(True)
            # t.start()
            self.connected = True

        elif response_type == "query-error":
            pass
            print("query response error")

        elif response_type == "publish-state-change":
            self.should_publish = j["Active"]

        elif response_type == "dependency-died":
            print("dependency died")

        elif response_type == "dependency-resurrected":
            print("dependency got resurrected")

        else:
            print("else")

    def handleDataReturn(self, payload):
        jsonResponse = payload.split("<>")[1]
        j = json.loads(jsonResponse)
        subscriptionId = j["SubscriptionId"]
        data =  j["Data"]
        returnObject = {}
        for nodeName,nodeValue in data.items():
            returnObject[nodeName] = self.parse(nodeValue["DataType"], nodeValue["Value"])

        callback = self.subscriptionId[subscriptionId]
        callback(returnObject)

    def on_message(self, client, userdata, msg):
        payload: str = msg.payload.decode('utf-8')
        print(f'Received message on topic {msg.topic}: {payload}')
        if msg.topic == self.response_topic:
            self.handleResponse(payload)
        
        else:
            self.handleDataReturn(payload)

    def on_connect(self, client, userdata, flags, rc):
        print("Connected with result code "+str(rc))
        client.subscribe(self.response_topic)
        client.subscribe(self.update_topic)

    def add_publish_topic(self, topic):
        self.publish_topic.append(topic)
    
    def add_subscirbe_topic(self, topic):
        self.subscribe_topics.append(topic)
        self.client.subscribe(topic)

    def send_pub_query(self, publishId, cypher, target_node, data_type):
        self.requests[(self.request_id, )] = "publish"
        self.requestToPublishid[(self.request_id, )] = publishId
        query = """publish<>{{"RequestId": {0}, "CypherQuery": "{1}", "TargetNode": "{2}", "DataType": "{3}", "PublishId": "{4}" }}""".format(self.request_id, cypher, target_node, data_type, publishId)
        self.request_id += 1
        self.client.publish(self.query_topicc, query, qos=1)
        return query

    def send_sub_query(self, callback, subscribion_id, cypher, target_node, transformations=None):
        self.requests[(self.request_id, )] = "subscribe"
        self.subscriptionId[subscribion_id] = callback
        query = ""
        if transformations == None:
            query = """subscribe<>{{"RequestId": {0}, "CypherQuery": "{1}", "TargetNodes": {2}, "SubscriptionId": "{3}" }}""".format(self.request_id, cypher, target_node, subscribion_id)
        else :
            query = """subscribe<>{{"RequestId": {0}, "CypherQuery": "{1}", "TargetNodes": {2}, "SubscriptionId": "{3}", "Transformations": "{4}" }}""".format(self.request_id, cypher, target_node, subscribion_id, transformations)
        print(query)
        self.request_id += 1
        self.client.publish(self.query_topicc, query, qos=1)
    
    def publishData(self, data, publishId):
        print("publish id")
        print(publishId)
        if self.should_publish:
            topic = self.publishIds[publishId]
            if self.publish_topic.__contains__(topic):
                self.client.publish(topic, payload=data)
            else:
                return "not a publish topic"
        else: 
            return "shouldn't publish, no one is listening"
        
    def start_heartbeat(self):
        while True:
            self.client.publish(self.heartbeat_topic, f"beat", qos=1)
            time.sleep(self.heartbeat_interval)
           
    def start_loop(self):
        while True:
            self.client.loop()
            time.sleep(1)