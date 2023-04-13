import base64
import threading
import paho.mqtt.client as mqtt
import time
import select
import json

class BaseClient:
    def __init__(self, id, connectionTimeout=60):
        self.id = id
        self.broker = "localhost"
        self.port = 1883
        self.publish_topic = []
        self.subscribe_topics = []
        self.response_topic = f"ddps/system/{self.id}/response"
        self.updates_topic = f"ddps/system/{self.id}/updates"
        
        self.connect_topic = f"ddps/system/clientmanager/connect"
        self.query_topicc = f"ddps/system/{self.id}/query"

        self.connectionTimeout = connectionTimeout
        self.heartbeat_topic = f"ddps/system/{self.id}/heartbeat"
        self.heartbeat_interval = 10

        self.update_topic = f"ddps/system/{self.id}/updates"
        self.should_publish = True

        self.subscriptionId = {}

        self.requestToPublishid = {}
        self.publishIds = {}

        self.request_id = 0 # goes up when sending a message on query_topic 
        self.requests = {}

        self.cypher = ""
        self.target_node = []
        self.data_type = "int"
        self.return_value = ""

        self.connected: bool = False

        self.client = mqtt.Client(self.id)
        self.client.on_connect = self.on_connect
        self.client.on_message = self.on_message
        self.client.loop_start()

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

    def updateTopic(self):
        pass
    
    def connect_to_broker(self):
        print("connecting")
        self.client.connect(self.broker, self.port, 60)
        
        while not self.client.is_connected():
            self.client.loop()
            pass

        print("publish connection message")
        self.client.publish(self.connect_topic, "connect<>" + json.dumps({"ClientId": self.id, "ConnectionTimeout": self.connectionTimeout}), qos=1)


    def handleResponse(self, response):
        response_type = response.split("<>")[0]
        print(response_type)
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

        elif response_type == "reconnect-ack":
            self.heartbeatInterval = j["HeartbeatInterval"]
            publishes = j["Publishes"]
            subscriptions = j["Subscriptions"]
            for p in publishes:
                self.publishIds[p["Id"]] = p["Topic"]
                self.add_publish_topic(p["Topic"])
            
            for s in subscriptions:
                self.add_subscirbe_topic(s.topic)
                self.subscriptionId[s["Id"]] = s["Topic"]

            self.connected = True

        elif response_type == "connect-ack":
            print("connect-ack")
            self.heartbeatInterval = j["HeartbeatInterval"]
            # t = threading.Thread(target=self.start_heartbeat)
            # t.setDaemon(True)
            # t.start()
            self.connected = True

        elif response_type == "query-error":
            print(j)
        
        elif response_type == "query-exists":
            self.requestToPublishid.pop((j["RequestID"],), None)
            self.requests.pop((j["RequestID"],), None)
            print("query-exists")

        elif response_type == "publish-state-change":
            print("topic state change")
            print(j)

        elif response_type == "dependency-died":
            print("dependency died")
            print(j)

        elif response_type == "dependency-resurrected":
            print("dependency ressurected")
            print(j)

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
        #print(f'Received message on topic {msg.topic}: {payload}')
        if msg.topic == self.response_topic or msg.topic == self.updates_topic:
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

    def give_cypher(self, cypher):
        self.cypher = cypher

    def send_pub_query(self, publishId, roles: list[str] = None):
        self.requests[(self.request_id, )] = "publish"
        self.requestToPublishid[(self.request_id, )] = publishId
        query = """publish<>{{"RequestId": {0}, "CypherQuery": "{1}", "TargetNode": "{2}", "DataType": "{3}", "PublishId": "{4}", "Roles": {5} }}""".format(self.request_id, self.cypher, self.target_node, self.data_type, publishId, json.dumps(roles) if roles is not None else [])
        self.request_id += 1
        self.client.publish(self.query_topicc, query, qos=1)
        return

    def send_sub_query(self, callback, subscribion_id, user=None):
        self.requests[(self.request_id, )] = "subscribe"
        self.subscriptionId[subscribion_id] = callback
        
        encodedPassword = base64.b64encode(user[1].encode("utf-8")).decode("utf-8") if user is not None else ""
        user_str = f'"Account": "{user[0]}", "AccountPassword": "{encodedPassword}",' if user is not None else ""
        
        query = """subscribe<>{{"RequestId": {0}, "CypherQuery": "{1}", "TargetNodes": {2}, "SubscriptionId": "{3}", {4} Transformations: {{ "temp": ["a+b"] }} }}""".format(self.request_id, self.cypher, self.target_node, subscribion_id, user_str)
        print(query)
        self.request_id += 1
        self.client.publish(self.query_topicc, query, qos=1)
        return query
    
    def publishData(self, data, publishId):
        if self.should_publish:
            topic = self.publishIds[publishId]
            if self.publish_topic.__contains__(topic):
                print("Sending")
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
            print("loop")
            self.client.loop()
            time.sleep(1)