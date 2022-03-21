using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using UnityEngine;
using Debug = UnityEngine.Debug;
using GameObject = HaptiOS.GameObject;

public class HapticObject : MonoBehaviour
{
    public string ObjectName;
    public string ReceiverIp;
    public int ReceiverPort;

    private const float PositionTolerance = 0f;
    private const float RotationTolerance = 0f;

    private float _x;
    private float _y;
    private float _z;
    private float _rotationX;
    private float _rotationY;
    private float _rotationZ;
    private float _rotationW;
    private Transform _transform;
    private const float Factor = 1000f;

    //private UdpClient _udpClient;
    //private IPEndPoint _receiver;

    private Stopwatch _updateWatch;

    HaptiDroneOSNetworking HaptiDroneOSNetworking;

    // Use this for initialization
    void Start()
    {
        _transform = GetComponent<Transform>();


        HaptiDroneOSNetworking = (HaptiDroneOSNetworking)ScriptableObject.CreateInstance(typeof(HaptiDroneOSNetworking));
        Debug.Log("Created HaptiDroneOSNetworking Instance...");

        //_udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        //_receiver = new IPEndPoint(IPAddress.Parse(ReceiverIp), ReceiverPort);
        _updateWatch = Stopwatch.StartNew();
    }

    void Awake()
    {
        tag = "haptic";
        Debug.Log("Tagging object haptic:" + this.ObjectName);
    }

    // Update is called once per frame
    void Update()
    {
        var position = _transform.position;
        var x = position.x * Factor;
        var y = position.y * Factor;
        var z = position.z * Factor;
        var rotation = _transform.rotation;
        var rotationX = rotation.x;
        var rotationY = rotation.y;
        var rotationZ = rotation.z;
        var rotationW = rotation.w;


        if (Math.Abs(x - _x) > PositionTolerance
            || Math.Abs(y - _y) > PositionTolerance
            || Math.Abs(z - _z) > PositionTolerance
            || Math.Abs(rotationX - _rotationX) > RotationTolerance
            || Math.Abs(rotationY - _rotationY) > RotationTolerance
            || Math.Abs(rotationZ - _rotationZ) > RotationTolerance
            || Math.Abs(rotationW - _rotationW) > RotationTolerance)
        {
            RestartUpdateWatch();
            SendUpdate();
        }
        else if (_updateWatch.ElapsedMilliseconds >= 1000f)
        {
            RestartUpdateWatch();
            SendUpdate();
        }
    }

    private void RestartUpdateWatch()
    {
        _updateWatch.Restart();
    }

    private void SendUpdate()
    {
        var position = _transform.position;
        _x = position.x * Factor*-1f;
        _y = position.y * Factor;
        _z = position.z * Factor;

        var rotation = _transform.rotation;
        _rotationX = rotation.x;
        _rotationY = rotation.y;
        _rotationZ = rotation.z;
        _rotationW = rotation.w;
        var eulers = _transform.eulerAngles;

        var hapticGameObject = new GameObject()
        {
            Id = ObjectName != "" ? ObjectName : name,
            Position = new GameObject.Types.Vector3 { X = _x, Y = _y, Z = _z },
            Rotation = new GameObject.Types.Quaternion
            {
                X = _rotationX,
                Y = _rotationY,
                Z = _rotationZ,
                W = _rotationW
            },
            Eulers = new GameObject.Types.Eulers()
            {
                X = eulers.x,
                Y = eulers.y,
                Z = eulers.z
            }
        };

        //Debug.Log("HapticGameObject: " + hapticGameObject);

        var data = hapticGameObject.ToByteArray();

        //_udpClient.Send(data, data.Length, _receiver);
        HaptiDroneOSNetworking.SendUpdate(data);
    }
}