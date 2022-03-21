using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Google.Protobuf;
using System.Threading;
using HaptiOS.Src.Vinter;
using HaptiOS.Src.Udp;
using HaptiOS.Src.RealWorldRpc;
using Grpc.Core;
using System.Net.Http;
using VinteR.Model.Gen;
using HaptiOS.Src.Drones;
using System.Numerics;
using HaptiOS.Src.Serialization;
using System.Linq;
using System.Net.Http.Headers;

namespace HaptiOS.Src.DroneControl
{
    class DroneManager : IDroneManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _config;

        private CancellationTokenSource _cancelToken;

        private static readonly byte[] EmptyObject = new Empty() { Status = 200 }.ToByteArray();


        public List<Drone> Drones;

        private readonly IDeserializer<GameObject> _gameObjectDeserializer;

        private readonly IVinterClient _vinterClient;
        private readonly IUdpReceiver _unityReceiver;

        private readonly Server _realWorldServer;
        private readonly IRealWorldServicer _realWorldServicer;
        private HttpClient _droneApiClient;

        public void Start()
        {
            Logger.Info("Starting DroneManager...");


            Logger.Info("Starting DroneManager started!");
        }

        private string _droneApiUrl;

        private Channel _unityChannel;
        public VirtualWorld.VirtualWorldClient _unityRpcClient;

        private const int RetryFindDronesTimeMs = 10 * 1000;
        public const int RetryConnectTimeMs = 10 * 1000;

        public bool IsStarted => _cancelToken != null && !_cancelToken.IsCancellationRequested;

        public DroneManager(
            IConfiguration config,
            IVinterClient vinterClient,
            IRealWorldServicer realWorldServicer,
            IUdpReceiver unityReceiver,
            IDeserializer<GameObject> gameObjectDeserializer)
        {
            Logger.Info("Setting up drone manager...");

            this.Drones = new List<Drone>();
            this._vinterClient = vinterClient;
            this._realWorldServicer = realWorldServicer;
            this._config = config;
            this._unityReceiver = unityReceiver;
            this._gameObjectDeserializer = gameObjectDeserializer;
            this._droneApiClient = new HttpClient();
            this._droneApiClient.DefaultRequestHeaders
                .Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));

            var ip = _config.GetValue<string>("flight.controller:drone_control_bridge_ip");
            var port = _config.GetValue<int>("flight.controller:drone_control_bridge_port");

            _droneApiUrl = $"http://{ip}:{port}";
            Logger.Info("_droneApiUrl: " + _droneApiUrl);

            _cancelToken = new CancellationTokenSource();
            // local rpc server that is called by the unity to start/stop flights
            var realWorldIp = _config.GetValue<string>("unity:rpc.ip");
            var realWorldPort = _config.GetValue<int>("unity:rpc.local.port");

            var unityIp = _config.GetValue<string>("unity:ip");
            var unityPort = _config.GetValue<int>("unity:remote.port");
            var unityAddress = $"{unityIp}:{unityPort}";
            _unityChannel = new Channel(unityAddress, ChannelCredentials.Insecure);
            _unityRpcClient = new VirtualWorld.VirtualWorldClient(_unityChannel);


            Logger.Info("Initializing drones...");


            InitDrones();


            Logger.Info("Done!");

            // connect to unity and bind eventhandler
            var unityReceiverPort = _config.GetValue<int>("unity:udp.local.ports:drone.control");

            _unityReceiver.OnDataReceived += GameObjectAvailableHandler;
            _unityReceiver.StartListening(unityReceiverPort);


            Logger.Info("Starting VinteR client...");
            // connect to vinter and bind eventhandler
            _vinterClient.OnMocapFrameReceived += VinterReceiverFrameAvailable;
            _vinterClient.Start();
            Logger.Info("Done!");

            Logger.Info("Starting Real World Server...");
            _realWorldServer = new Server
            {
                Services = { RealWorld.BindService(_realWorldServicer as RealWorld.RealWorldBase) },
                Ports = { new ServerPort(realWorldIp, realWorldPort, ServerCredentials.Insecure) }
            };
            _realWorldServicer.OnStart += OnRealWorldStart;
            _realWorldServicer.OnStop += OnRealWorldStop;
            _realWorldServer.Start();
            Logger.Info("{0} ({1}:{2})", "Real world RPC server started", realWorldIp, realWorldPort);
            Logger.Info("Done setting up Drone Manager!");
        }

        private void InitDrones()
        {
            IEnumerable<IConfigurationSection> configuredDrones = _config.GetSection("flight.controller:drones").GetChildren();

            foreach (IConfigurationSection section in configuredDrones)
            {
                Drone drone = new Drone(section.Key, _config, this);
                this.Drones.Add(drone);
                Logger.Info("Added drone: " + section.Key);
            }
        }

        private void OnRealWorldStart(object sender, GameObject gameObject)
        {
            Logger.Info("OnRealWorldStart");
            Logger.Info("gameObject Name: " + gameObject.Id);
            foreach (Drone drone in this.Drones)
            {
                if (drone.Name.Equals(gameObject.Id))
                {
                    Logger.Info("For drone: " + drone.Name + " start called by rpc service");
                    if (drone.IsStarted)
                    {
                        Logger.Warn("Drone controller already running");
                        return;
                    }
                    drone.SetVirtualObject(gameObject);
                    Logger.Info("Drone: " + drone.Name + " gets GameObject: " + gameObject.Id + " assigned!");
                    drone.StartFlight();
                    Logger.Info("Start called by rpc service");
                }
            }
        }

        public void Stop()
        {
            _cancelToken.Cancel();

            StopServices();

            _realWorldServicer.OnStart -= OnRealWorldStart;
            _realWorldServicer.OnStop -= OnRealWorldStop;
            _realWorldServer.ShutdownAsync().Wait();
            var realWorldServerPort = _realWorldServer.Ports.First();
            Logger.Info("Realworld rpc server stopped ({0}:{1})",
                realWorldServerPort.Host,
                realWorldServerPort.Port);

            _unityChannel.ShutdownAsync().Wait();
            Logger.Info("{0} ({1})", "Unity RPC client stopped", _unityChannel.Target);

            _unityReceiver.OnDataReceived -= GameObjectAvailableHandler;
            _unityReceiver.Stop();

            _vinterClient.OnMocapFrameReceived -= VinterReceiverFrameAvailable;
            _vinterClient.Stop();

            _cancelToken = new CancellationTokenSource();

            Logger.Info("Drone controller stopped");
        }

        private void GameObjectAvailableHandler(object sender, byte[] data)
        {
            var gameObject = _gameObjectDeserializer.Deserialize(data);

            foreach (Drone drone in Drones)
            {
                if (drone._virtualObject == null)
                {
                    return;
                }

                if (drone._virtualObject.Id.Equals(gameObject.Id))
                {
                    drone._virtualObject.Position = new GameObject.Types.Vector3()
                    {
                        X = gameObject.Position.X,
                        Y = gameObject.Position.Y,
                        Z = gameObject.Position.Z
                    };

                    var rotation = Quaternion.CreateFromYawPitchRoll(gameObject.Eulers.Y.ToRadians(), 0, 0);
                    drone._virtualObject.Rotation = new GameObject.Types.Quaternion()
                    {
                        X = rotation.X,
                        Y = rotation.Y,
                        Z = rotation.Z,
                        W = rotation.W
                    };
                }
            }
        }

        private void OnRealWorldStop(object sender, GameObject gameObject)
        {
            Logger.Info("Stop called by rpc service");
            StopServices();
        }


        public List<Drone> FindDrones()
        {
            Logger.Info("Searching for drones");
            List<Drone> foundDrones = new List<Drone>();

            while (!_cancelToken.IsCancellationRequested)
            {
                try
                {
                    var response = CallDroneControlApi("finddrones", EmptyObject);
                    var ids = DroneIds.Parser.ParseFrom(response);

                    if (ids.Values.Count == 0)
                    {
                        Logger.Warn("No drones found");
                        Logger.Info("Retrying in {0} s", RetryFindDronesTimeMs / 1000);
                        Thread.Sleep(RetryFindDronesTimeMs);
                    }
                    else
                    {
                        Logger.Info("Found drones with IDs: " + ids);

                        foreach (var id in ids.Values)
                        {
                            var _droneId = id;
                            Logger.Info("Drones found (ids = {0}), using {1}", string.Join(", ", ids), _droneId);
                            //TODO: init drones here.. map MAC ADDRESSES
                        }

                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Could not find drones; cause: {0}", e.Message);
                }
            }
            return foundDrones;
        }

        private void VinterReceiverFrameAvailable(object sender, MocapFrame frame)
        {
            foreach (Drone drone in this.Drones)
            {
                foreach (MocapFrame.Types.Body body in frame.Bodies)
                {
                    if (drone.Name.Equals(body.Name))
                    {
                        if (body != null)
                        {
                            Logger.Debug("{0}: {1}", body.Name, body.Centroid);
                            drone._trackedObject = body;
                            drone._droneLostCount = 0;
                        }
                        else
                        {

                            drone._droneLostCount += 1;
                        }
                    }
                }
            }
        }


        private void StopServices()
        {
            foreach (Drone drone in this.Drones)
            {
                drone.StopThread(drone._flightThread, "Drone controller flight interrupted", "Flight thread stopped");
                drone._flightThread = null;

                drone.StopThread(drone._droneControlThread, "Drone control loop interrupted", "Control loop thread stopped");
                drone._droneControlThread = null;

                drone.StopThread(drone._positionLoggingThread, "Position logging interrupted", "Position logging thread stopped");
                drone._positionLoggingThread = null;

                drone.Disconnect(drone.Id);

                Logger.Info("Drone: " + drone.Name + " Services stopped");

                drone.IsStarted = false;
                drone._cancelToken.Cancel();
                drone._cancelToken = new CancellationTokenSource();
            }
        }


        public byte[] CallDroneControlApi(string path, byte[] data, int timeoutSeconds = 5, bool forceWait = false)
        {
            Logger.Info($"Calling {_droneApiUrl}/{path}");
            var timeout = timeoutSeconds * 1000;

            var byteContent = new ByteArrayContent(data);
            byteContent.Headers.Add("Content-type", "application/x-protobuf");

            var t = _droneApiClient.PostAsync($"{_droneApiUrl}/{path}", byteContent);

            if (forceWait) t.Wait(timeout);
            else t.Wait(timeout, _cancelToken.Token);

            try
            {
                if (!_cancelToken.IsCancellationRequested)
                {
                    Logger.Debug($"Reading drone control result from {path}");
                    var readTask = t.Result.Content.ReadAsByteArrayAsync();

                    if (forceWait)
                    {
                        readTask.Wait(timeout);
                        Logger.Debug("Drone control result read");
                    }
                    else
                    {
                        readTask.Wait(timeout, _cancelToken.Token);
                    }
                    return readTask.Result;
                }
            }
            catch (AggregateException)
            {
                Logger.Error("Drone api call request canceled");
            }
            return new byte[0];
        }


        private void Disconnect(int droneId)
        {
            try
            {
                Logger.Info("Disconnecting from drone {0}", droneId);
                var disconnectParams = new DisconnectParams() { DroneId = droneId };
                CallDroneControlApi("disconnect", disconnectParams.ToByteArray(), forceWait: true);
                Logger.Info("Disconnected from drone {0}", droneId);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not disconnect from drone; cause {0}", e.Message);
            }
        }

        public void StartDrone(string DroneName)
        {
            Logger.Debug("StartDrone Method called!");
        }

        public List<string> GetDroneNames()
        {
            Logger.Debug("StartDrone Method called!");
            return null;
        }

        HaptiOS.DroneControl IDroneManager.GetFlyCommand(int droneId)
        {
            Logger.Debug("GetFlyCommand Method called!");

            foreach (Drone drone in Drones)
            {
                if (droneId == drone.Id)
                {
                    var command = new HaptiOS.DroneControl()
                    {
                        DroneId = droneId,
                        Roll = drone._roll,
                        Pitch = drone._pitch,
                        Yaw = drone._yaw,
                        VerticalMovement = drone._verticalMovement
                    };
                    return command;
                }

            }

            //TODO: log flight command here....
            return null;
        }

        public void StopDrone(string DroneName)
        {
            Logger.Debug("StopDrones Method called!");
        }

        public bool IsStartedDrone(string droneName)
        {
            Logger.Debug("StartDrone Method called!");
            return false;
        }
    }
}
