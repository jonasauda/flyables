import logging
import json
import os.path


class DroneStore():

    def __init__(self):
        self.__devices = None
        self.__filename = 'drones.json'
        self.__init_store()

    def add(self, mac):

        self.__devices.append(mac)
        self.__write()

    def remove(self, mac):
        self.__devices = [d for d in self.__devices if d != mac]
        self.__write()

    def get(self, drone_id=0):

        if drone_id == 0:
            return self.__devices[1:]
        elif drone_id < len(self.__devices):
            return self.__devices[drone_id]
        else:
            return -1

    def count(self):
        if self.__devices is None:
            return 0
        else:
            return len(self.__devices) - 1

    def __write(self):
        data = {
            'drones': self.__devices
        }
        with open(self.__filename, "w") as write_file:
            json.dump(data, write_file)

    def __init_store(self):
        if not os.path.isfile(self.__filename):
            logging.info('Store %s not found -> creating' % self.__filename)
            self.__devices = ['']
            with open(self.__filename, "w") as write_file:
                json.dump({'drones': self.__devices}, write_file)
        else:
            logging.info("Loading drones store from %s" % self.__filename)
            self.__devices = []
            with open(self.__filename, "r") as read_file:
                data = json.load(read_file)
                for mac in data['drones']:
                    self.__devices.append(mac)
