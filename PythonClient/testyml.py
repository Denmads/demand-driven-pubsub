import yaml

with open("D:\\school\\demand-driven-pubsub\\PythonClient\\test1.yml", "r") as file:
    data = yaml.load(file, Loader=yaml.FullLoader)
    print(data)