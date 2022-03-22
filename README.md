# Flyables: Haptic Input Devices for Virtual Reality using Quadcopters

Jonas Auda, Nils Verheyen, Sven Mayer and Stefan Schneegass

## Support

If you have any questions, please do not hesitate to write us an email.

## Contact
jonas.auda@uni-due.de

## Video

https://www.youtube.com/watch?v=udBv0pOpJZM

## Citation/BibTeX

```
@inproceedings{10.1145/3489849.3489855,
author = {Auda, Jonas and Verheyen, Nils and Mayer, Sven and Schneegass, Stefan},
title = {Flyables: Haptic Input Devices for Virtual Reality Using Quadcopters},
year = {2021},
isbn = {9781450390927},
publisher = {Association for Computing Machinery},
address = {New York, NY, USA},
url = {https://doi.org/10.1145/3489849.3489855},
doi = {10.1145/3489849.3489855},
articleno = {40},
numpages = {11},
keywords = {Virtual Reality, Flyables, Quadcopter, Haptics, Toolkit., Drones},
location = {Osaka, Japan},
series = {VRST '21}
}

```

# Instructions
## Setup Instructions:
### Raspberry Pi Setup:
1. Install Python on a Raspberry Pi
2. Add a Bluetooth dongle
3. Download the "dronecontrol" folder from git
4. Make sure the server.py is executable
5. Start it with `sudo $HOME/.virtualenvs/dronecontrol/bin/python server.py [HaptiOS-IP] [HaptiOS-Port (5000)]`

### Drone Setup:
1. Start the drone
2. Run the dronecontrol server.py with the argument ```--scan```
3. Note the MAC Address of the drone
4. Add it to the drones.json file

1. Attach OptiTrack markers to the drone
2. Calibrate the RigidBody to have z pointing forward, x left and y up
3. Make sure the RigidBody gets detected reliably

### PC Setup:
1. VinteR:
	1. Install VinteR from https://github.com/jonasauda/VinteR
	2. Make sure Ports 6040 and 6041 for HaptiOS and Unity are in VinteR Receivers
2. HaptiOS:
	1. Tested with Visual Studio 2022 Community
	2. HaptiOS can be configured with the "haptios.config.json" in `haptios/HaptiOS.Src`
	3. Make sure the drones are configured and enabled correctly
	4. HaptiOS can be run with the "StartHaptios.bat" in `haptios/HaptiOS.Src` or via Visual Studio
3. Unity:
	1. Requires Unity Version 2020.3
	3. Demo Scene in Assets/Scenes folder is provided as a start point for own implementations
	4. Make sure the IPs are configured correctly in the Inspector

## Startup Instructions:
1. Start VinteR and OptiTrack Motive Software
2. Start Pi Software as described above
3. Repeat for every flight
	1. Start HaptiOS
	2. Start Drone (Has to be in blinking state until Unity is started)
	3. Start Unity scene
