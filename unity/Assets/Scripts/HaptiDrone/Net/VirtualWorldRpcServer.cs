using System;
using System.Threading.Tasks;
using Grpc.Core;
using HaptiOS;
using HaptiDrone;
using UnityEngine;
using GameObject = UnityEngine.GameObject;

public class VirtualWorldRpcServer : MonoBehaviour
{
    public Settings Settings;
    private Server _rpcServer;

    private static readonly Empty RpcEmptyObject = new Empty();

    public event EventHandler<GameObjectBlimpStatus> OnBlimpStatusReceived;

    public event EventHandler<HaptiOS.GameObject> OnTakeOff;

    public event EventHandler<HaptiOS.GameObject> OnLand;

    void Start()
    {
        var ip = Settings.VirtualWorldIp;
        var port = Settings.VirtualWorldRpcPort;


        var gameObjectList = LoadGameObjectList();

        var servicer = new RpcServicer(gameObjectList);
        servicer.RpcCallOnBlimpStatus += HandleRpcCallOnBlimpStatus;
        servicer.RpcCallOnTakeOff += HandleRpcCallOnTakeOff;
        servicer.RpcCallOnLand += HandleRpcCallOnLand;

        _rpcServer = new Server
        {
            Services = {VirtualWorld.BindService(servicer)},
            Ports = {new ServerPort(ip, port, ServerCredentials.Insecure)}
        };
        _rpcServer.Start();
        Debug.LogFormat("{0} ({1}:{2})", "RPC server started", ip, port);
    }

    private void HandleRpcCallOnBlimpStatus(object sender, GameObjectBlimpStatus e)
    {
        Debug.LogFormat("received blimp status {0}", e);
        OnBlimpStatusReceived?.Invoke(this, e);
    }

    private void HandleRpcCallOnTakeOff(object sender, HaptiOS.GameObject gameObject)
    {
        Debug.LogFormat("Take off {0}", gameObject);
        OnTakeOff?.Invoke(this, gameObject);
    }

    private void HandleRpcCallOnLand(object sender, HaptiOS.GameObject gameObject)
    {
        Debug.LogFormat("Land {0}", gameObject);
        OnLand?.Invoke(this, gameObject);
    }

    GameObjectList LoadGameObjectList()
    {
        var gameObjects = GameObject.FindGameObjectsWithTag("haptic");
        var result = new GameObjectList();
        Debug.LogFormat("Found haptic objects: {0}", gameObjects.Length);
        foreach (var o in gameObjects)
        {
            result.Objects.Add(new HaptiOS.GameObject()
            {
                Id = o.name,
                Position = new HaptiOS.GameObject.Types.Vector3()
                {
                    X = o.transform.position.x,
                    Y = o.transform.position.y,
                    Z = o.transform.position.z
                },
                Rotation = new HaptiOS.GameObject.Types.Quaternion()
                {
                    X = o.transform.rotation.x,
                    Y = o.transform.rotation.y,
                    Z = o.transform.rotation.z,
                    W = o.transform.rotation.w
                }
            });
        }
        return result;
    }

    void OnApplicationQuit()
    {
        _rpcServer.ShutdownAsync().Wait();
    }

    private class RpcServicer : VirtualWorld.VirtualWorldBase
    {
        private readonly GameObjectList _gameObjectList;

        internal event EventHandler<GameObjectBlimpStatus> RpcCallOnBlimpStatus;
        internal event EventHandler<HaptiOS.GameObject> RpcCallOnTakeOff;
        internal event EventHandler<HaptiOS.GameObject> RpcCallOnLand;

        public RpcServicer(GameObjectList gameObjectList)
        {
            _gameObjectList = gameObjectList;
        }

        public override Task<GameObjectList> GetObjects(Empty request, ServerCallContext context)
        {
            return Task.FromResult(_gameObjectList);
        }

        public override Task<Empty> SendBlimpStatus(GameObjectBlimpStatus request, ServerCallContext context)
        {
            RpcCallOnBlimpStatus?.Invoke(this, request);
            return Task.FromResult(RpcEmptyObject);
        }

        public override Task<Empty> OnTakeOff(HaptiOS.GameObject gameObject, ServerCallContext context)
        {
            RpcCallOnTakeOff?.Invoke(this, gameObject);
            return Task.FromResult(RpcEmptyObject);
        }

        public override Task<Empty> OnLand(HaptiOS.GameObject gameObject, ServerCallContext context)
        {
            RpcCallOnLand?.Invoke(this, gameObject);
            return Task.FromResult(RpcEmptyObject);
        }
    }
}