using Grpc.Core;
using HaptiDrone;
using HaptiOS;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameObject = HaptiOS.GameObject;

public class DroneStarter : MonoBehaviour
{

    UnityEngine.GameObject[] drones;

    public VirtualWorldRpcServer RpcServer;

    public Settings Settings;

    private Channel _haptiosChannel;

    private RealWorld.RealWorldClient _haptiosRpcClient;
    private GameObject _hapticGameObject;


    // Start is called before the first frame update
    void Start()
    {
        StartDrones();
    }


    public void StartDrones()
    {
        if (RpcServer == null)
        {
            Debug.Log("Rpc server not set, flight canceled");
            return;
        }

        if (Settings == null)
        {
            Debug.Log("Settings not set, flight canceled");
            return;
        }

        // rpc implementation to send control signals to haptios
        var haptiosIp = Settings.HaptiosIp;
        var haptiosPort = Settings.HaptiosRealWorldPort;
        var haptiosAddress = $"{haptiosIp}:{haptiosPort}";
        _haptiosChannel = new Channel(haptiosAddress, ChannelCredentials.Insecure);
        _haptiosRpcClient = new RealWorld.RealWorldClient(_haptiosChannel);
        drones = UnityEngine.GameObject.FindGameObjectsWithTag("haptic");
        foreach (var drone in drones)
        {
            try
            {

                Debug.Log("drone: " + drone);

                ;
                HapticObject hapticScript = drone.GetComponent<HapticObject>();

                var position = drone.gameObject.transform.position;
                var rotation = drone.gameObject.transform.rotation;
                var eulerAngles = drone.gameObject.transform.eulerAngles;
                _hapticGameObject = new GameObject()
                {

                    Id = hapticScript.ObjectName,
                    Position = new GameObject.Types.Vector3()
                    {
                        X = position.x,
                        Y = position.y,
                        Z = position.z
                    },
                    Rotation = new GameObject.Types.Quaternion()
                    {
                        X = rotation.x,
                        Y = rotation.y,
                        Z = rotation.z,
                        W = rotation.w
                    },
                    Eulers = new GameObject.Types.Eulers()
                    {
                        X = eulerAngles.x,
                        Y = eulerAngles.y,
                        Z = eulerAngles.z
                    }
                };
                Debug.Log("Starting drone: " + drone.name);
                _haptiosRpcClient.Start(_hapticGameObject);
            }
            catch (RpcException e)
            {
                Debug.LogError($"Could not run 'Start()' on haptios rpc endpoint; cause: {e.Message}");
                Debug.LogError(e.StackTrace);
            }
        }
    }

    void OnApplicationQuit()
    {
        StopFlight();
    }

    private void StopFlight()
    {
        Debug.Log($"Stopping flight of {_hapticGameObject.Id}");
        try
        {
            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            _haptiosRpcClient.Stop(_hapticGameObject, deadline: timeout);
        }
        catch (RpcException e)
        {
            Debug.LogError($"Could not run 'Stop()' on haptios rpc endpoint; cause: {e.Message}");
        }
    }

}
