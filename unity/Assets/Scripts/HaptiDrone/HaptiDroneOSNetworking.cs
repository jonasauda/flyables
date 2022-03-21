using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class HaptiDroneOSNetworking : ScriptableObject
{
    private UdpClient _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
    private IPEndPoint _receiver = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6042);


    public void SendUpdate(byte[] data)
    {
        //Debug.Log("Sending data...");
        _udpClient.Send(data, data.Length, _receiver);
    }
}
