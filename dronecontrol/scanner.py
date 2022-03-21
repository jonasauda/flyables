# coding: utf-8

import logging

try:
    from bluepy.btle import Scanner, DefaultDelegate
except Exception as e:
    from dummy import DefaultDelegate
    logging.error("Import from bluepy failed; cause: %s", e)


class ScanDelegate(DefaultDelegate):
    def __init__(self):
        DefaultDelegate.__init__(self)

    def handleDiscovery(self, dev, isNewDev, isNewData):
        if isNewDev:
            logging.debug("Discovered device %s" % dev.addr)
        elif isNewData:
            logging.debug("Received new data from %s" % dev.addr)


class DroneScanner():

    def scan(self, seconds=10):
        logging.info("Starting scan")

        scanner = Scanner().withDelegate(ScanDelegate())
        devices = scanner.scan(seconds)

        for dev in devices:
            #print ("Device %s (%s), RSSI=%d dB" % (dev.addr, dev.addrType, dev.rssi))
            for (adtype, desc, value) in dev.getScanData():
                #print("  %s = %s" % (desc, value))
                if (desc == "Complete Local Name"):
                    if "Mambo" in value or "RS" in value:
                        logging.info("Found '%s'/%s, RSSI=%d dB, %s" %
                                     (dev.addr, dev.addrType, dev.rssi, value))
                        yield dev.addr
        logging.info("Scan finished")
