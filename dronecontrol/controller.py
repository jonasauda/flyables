# coding: utf-8

import base64
import logging
import protobuf.haptios_pb2 as model
import threading
import time
import urllib.request
from datetime import datetime
from pyparrot.Minidrone import Mambo
from scanner import DroneScanner
from storage import DroneStore


TakeoffMethods = ['safe', 'danger']


def current_millis():
    return int(round(time.time() * 1000))


def timed(func):

    def func_wrapper(*args, **kwargs):
        start = current_millis()
        func_result = func(*args, **kwargs)
        end = current_millis()
        diff = end - start
        logging.debug('{0:>20} took {1:5} ms to complete'.format(
            func.__name__, diff))
        return func_result
    return func_wrapper


class DroneController():

    def __init__(self):
        self.drone_store = DroneStore()
        self.haptios_ip = '127.0.0.1'
        self.haptios_port = 5000
        self.drones = {}
        self.fly_duration = 0.1
        self.fly_thread = None
        self.max_vertical_speed = 0
        self.takeoff_method = TakeoffMethods[0]

    def _drone_call(call):

        def wrapper(self, drone_id, *args):
            if 'drone' not in self.drones[drone_id]:
                return {'status': 0}

            return call(self, drone_id, *args)
        return wrapper

    def scan(self, seconds=10):
        scanner = DroneScanner()
        for address in scanner.scan(seconds):
            if address in self.drone_store.get():
                self.drone_store.remove(address)

            self.drone_store.add(address)

    def _all_drone_numbers(self):
        for i in range(1, self.drone_store.count() + 1):
            yield i

    def find_drones(self):
        logging.info("Searching for drones")
        # this has to be done from console as super user
        # https://pyparrot.readthedocs.io/en/latest/quickstartminidrone.html#ble-connection
        # from pyparrot.scripts import findMinidrone
        values = []
        for number in self._all_drone_numbers():
            mac = self.drone_store.get(number)
            values.append({'number': number, 'mac': mac})
        return {'values': values}

    def connect(self, drone_id, num_retries, max_tilt=10):
        logging.info("Connecting to %s, num retries: %d" %
                     (drone_id, num_retries))
        mac = self.drone_store.get(drone_id)
        drone = Mambo(mac)
        print("Connecting to id:", drone_id)
        self.drones[drone_id] = {'drone': drone}
        print("id:", drone_id)
        print("mac:", mac)
        success = drone.connect(num_retries=num_retries)

        if success:
            logging.info('Using max tilt of %d' % max_tilt)
            drone.set_max_tilt(max_tilt)
            if self.max_vertical_speed > 0:
                logging.info('Using max vertical speed of %f' %
                             self.max_vertical_speed)
                drone.set_max_vertical_speed(self.max_vertical_speed)

        return {'connected': success}

    def disconnect(self, drone_id=0):

        if drone_id == 0:
            self._disconnect_all()
            return

        if drone_id not in self.drones:
            logging.warn("Drone %d was not connected" % drone_id)
            return

        mac = self.drone_store.get(drone_id)
        if 'flight' in self.drones[drone_id]:
            self.wait_for_flight(drone_id)

        logging.info("Disconnecting from %d/%s" % (drone_id, mac))
        try:
            self._disconnect(drone_id)
            return {'status': 200}
        except Exception:
            logging.exception('Could not disconnect from drone %d' % drone_id)
            return {'status': 500}

    def _disconnect_all(self):
        for number in self._all_drone_numbers():
            if number in self.drones:
                self.disconnect(number)

    @_drone_call
    def _disconnect(self, drone_id):
        drone = self.__get_drone(drone_id)
        try:
            drone.disconnect()
        except Exception as e:
            logging.error('Could not disconnect from drone %d;'
                          ' cause: %s' % (drone_id, e))
        return {'status': 200}

    @_drone_call
    def take_off(self, drone_id, timeout):

        logging.info("Taking off!")
        try:
            drone = self.__get_drone(drone_id)

            if self.takeoff_method == 'safe':
                logging.info('Using safe takeoff')
                drone.safe_takeoff(timeout['seconds'])
            elif self.takeoff_method == 'danger':
                logging.info('Danger danger takeoff')
                drone.takeoff()

            self.start_flight(drone_id)
            return {'status': 200}
        except Exception:
            logging.exception('Could not take off drone %d' % drone_id)
            return {'status': 500}

    def land(self, drone_id, timeout):
        logging.info("Landing %d" % drone_id)
        try:
            self.stop_flight(drone_id)
            self.__land(drone_id, timeout['seconds'])
            return {'status': 200}
        except Exception:
            logging.exception('Could not land drone %d' % drone_id)
            return {'status': 500}

    @_drone_call
    def __land(self, drone_id, timeout):
        drone = self.__get_drone(drone_id)
        drone.safe_land(timeout)
        drone.smart_sleep(timeout)
        return {'status': 200}

    def wait_for_flight(self, drone_id):
        logging.info("Waiting until flight is done")
        if 'flight' not in self.drones[drone_id]:
            logging.info("Not in flight")
            return

        t = self.drones[drone_id]['flight']
        while (t.is_alive()):
            time.sleep(10)
        logging.info("Flight done")

    def start_flight(self, drone_id):
        haptios_url = 'http://%s:%d' % (self.haptios_ip, self.haptios_port)
        fly_thread = FlyThread(haptios_url, drone_id,
                               self.__get_drone(drone_id),
                               self.fly_duration)
        self.drones[drone_id]['flight'] = fly_thread
        #fly_thread.start()

    def stop_flight(self, drone_id):
        if 'flight' not in self.drones[drone_id]:
            return

        logging.info("Stopping flight thread")
        t = self.drones[drone_id]['flight']
        t.event.set()
        t.join()
        self.drones[drone_id].pop('flight', None)

    def __get_drone(self, drone_id):
        return self.drones[drone_id]['drone']
      
    def fly(self, data):
        #print("flying")
        drone_id = int(data["DroneId"])
        roll = int(data["Roll"])
        pitch = int(data["Pitch"])
        yaw = int(data["Yaw"])
        vertical_movement  = int(data["VerticalMovement"])
        print("Drone ID:", str(drone_id))
        try:
            drone = self.drones[drone_id]["drone"]
            #print(drone)
            drone.fly_direct(roll, pitch, yaw, vertical_movement, self.fly_duration)
            print("flying")
        except KeyError as e:
            print("KeyError")
            print(self.drones)


class FlyThread(threading.Thread):

    def __init__(self, haptios_url, drone_id, drone, fly_duration):
        threading.Thread.__init__(self)
        self.drone_fly_cmd_url = '%s/api/droneflight/command/%d' % (
            haptios_url, drone_id)
        self.update_status_url = '%s/api/droneflight/status/%d' % (
            haptios_url, drone_id)
        self.event = threading.Event()
        self.drone = drone
        self.fly_duration = fly_duration
        logging.info("fly duration %f" % self.fly_duration)

    @timed
    def get_command(self):
        url = self.drone_fly_cmd_url
        headers = {
            'Accept': 'application/x-protobuf'
        }
        req = urllib.request.Request(url, method='GET', headers=headers)
        try:
            with urllib.request.urlopen(req) as f:
                response = f.read()
                return response
        except urllib.error.URLError as e:
            msg = "Could not get command from haptios; cause: %s" % str(e)
            print(url)
            logging.error(msg)

    @timed
    def post_status(self):
        url = ''.join((self.update_status_url, '/',
                       self.drone.sensors.battery))
        req = urllib.request.Request(url, method='POST')
        try:
            with urllib.request.urlopen(req) as f:
                response = f.read()
                return response
        except urllib.error.URLError as e:
            msg = "Could not send status update; cause: %s" % str(e)
            logging.error(msg)

    @timed
    def parse_command(self, content):
        decoded_content = base64.b64decode(content)
        cmd = model.DroneControl()
        cmd.ParseFromString(decoded_content)
        return cmd

    @timed
    def fly(self, command):
        if not self.event.is_set():
            self.drone.fly_direct(command.roll,
                                  command.pitch,
                                  command.yaw,
                                  command.verticalMovement,
                                  100)
        fmt = 'cmd -> roll: {:=+4d}, pitch: {:=+4d}, yaw: {:=+4d}, vert: {:=+4d}'.format(
            command.roll, command.pitch, command.yaw, command.verticalMovement)
        logging.info(fmt)

    def run(self):
        while not self.event.is_set():
            try:
                response = self.get_command()
                if response is not None:
                    cmd = self.parse_command(response)
                    self.fly(cmd)
            except Exception:
                logging.exception(
                    "An error occured during flight, retrying in 1s")
                time.sleep(1)
