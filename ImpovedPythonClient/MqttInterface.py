import yaml
import random

with open("broker-config.yml", "r") as file:
    data = yaml.load(file, Loader=yaml.FullLoader)
    print(data)

