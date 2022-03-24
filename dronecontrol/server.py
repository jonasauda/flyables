# coding: utf-8

import os
import argparse
import logging
import logging.config

from flask import Flask, request
from flask_pbj import api, json, protobuf

from controller import DroneController, TakeoffMethods
from protobuf.haptios_pb2 import Empty, DroneIds, ConnectResult, Timeout, \
    ConnectParams, DisconnectParams, TakeOffParams, LandParams, DroneControl

logging.config.fileConfig('logging.conf')

SECRET_KEY = 'haptidrone development key'
DEBUG = True

app = Flask(__name__)
app.config.from_object(__name__)

controller = DroneController()


@app.route('/finddrones', methods=['POST'])
@api(json, protobuf(receives=Empty, sends=DroneIds))
def find_drones():
    logging.debug('/finddrones')
    ids = controller.find_drones()
    return ids


@app.route('/connect', methods=['POST'])
@api(json, protobuf(receives=ConnectParams, sends=ConnectResult))
def connect():
    logging.debug('/connect')
    logging.debug("request: %s" % request.data_dict)
    drone_id = request.data_dict['droneId']
    num_retries = request.data_dict['numTries']
    max_tilt = request.data_dict['maxTilt']
    return controller.connect(drone_id, num_retries, max_tilt)


@app.route('/disconnect', methods=['POST'])
@api(json, protobuf(receives=DisconnectParams, sends=Empty))
def disconnect():
    logging.debug('/disconnect')
    drone_id = request.data_dict['droneId']
    return controller.disconnect(drone_id)


@app.route('/takeoff', methods=['POST'])
@api(json, protobuf(receives=TakeOffParams, sends=Empty))
def takeoff():
    logging.debug('/takeoff')
    drone_id = request.data_dict['droneId']
    timeout = request.data_dict['timeout']
    return controller.take_off(drone_id, timeout)

@app.route('/setcommand', methods=['POST'])
def set_command():    
    #print(request.data)
    data = request.get_json()
    controller.fly(data)
    return "ok"

@app.route('/land', methods=['POST'])
@api(json, protobuf(receives=LandParams, sends=Empty))
def land():
    logging.debug('/land')
    drone_id = request.data_dict['droneId']
    timeout = request.data_dict['timeout']
    return controller.land(drone_id, timeout)


if __name__ == "__main__":

    parser = argparse.ArgumentParser()
    parser.add_argument('haptios_ip', help='''Ip address that the haptios
        web server runs on''', type=str)
    parser.add_argument('haptios_port', help='''Port that the haptios
        web server runs on''', type=int)
    scan_help = '''Use this flag to perform a initial bluetooth
        scan for drones. This takes some time but is useful to get current
        available drones. IMPORTANT! This must be run as superuser.
        '''
    parser.add_argument('--host', help='''Server name that this web application
        uses. By default the ip 0.0.0.0 is used to listen on any address
        ''', type=str, default='0.0.0.0')
    parser.add_argument('--port', help='Port the web server uses to run on',
                        type=int, default=7373)
    parser.add_argument('--scan', '-s', help=scan_help, action='store_true')
    parser.add_argument('--scan-time', help='Time in seconds to scan',
                        type=int, default=10)
    parser.add_argument('--fly-duration', help='''Time in millis a drone
        should process pitch, yaw, roll and vertical movement until it asks
        for new values
    ''', type=float, default=100)
    parser.add_argument('--max-vertical-speed', help='''Maximum speed in
        m/s that the drone uses for vertical movement
        ''', type=float, default=0)
    parser.add_argument('--takeoff', help='''To start the drone use
        "safe" to perform normal take via drone.safe_takeoff(<timeout>) or
        "danger" to run drone.takeoff
    ''', choices=TakeoffMethods, default=TakeoffMethods[0])
    args = parser.parse_args()

    try:
        controller.haptios_ip = args.haptios_ip
        controller.haptios_port = args.haptios_port
        #controller.fly_duration = args.fly_duration / 1000.0
        #controller.max_vertical_speed = args.max_vertical_speed
        #controller.takeoff_method = args.takeoff
        if args.scan:
            controller.scan(args.scan_time)
        #controller.haptios_ip = "192.168.1.6"
        #controller.haptios_port = 5000
        controller.fly_duration = 100 / 1000.0
        controller.max_vertical_speed = 0
        #controller.takeoff_method = TakeoffMethods[0]

        logging.info('Starting web application')
        os.environ['FLASK_ENV'] = 'development'
        app.run(host="0.0.0.0", port=7373)
    except KeyboardInterrupt as interrupt:
        controller.stop_flight()
        controller.disconnect()
        logging.info('Server stopped')
