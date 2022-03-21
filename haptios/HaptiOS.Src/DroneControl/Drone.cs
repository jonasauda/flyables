using HaptiOS.Src.DroneControl;
using HaptiOS.Src.PID;
using System;
using System.Numerics;
using System.Threading;
using VinteR.Model.Gen;
using Google.Protobuf;
using NLog;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.IO;


namespace HaptiOS.Src.Drones
{
    class Drone
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        public GameObject _virtualObject;
        public MocapFrame.Types.Body _trackedObject;

        public string Name;
        public int Id;

        private float _MaxTilt;

        public int _yaw;
        public int _pitch;
        public int _roll;
        public int _verticalMovement;

        private const int MaxLostCount = 10;

        private bool IsDroneLost => _droneLostCount > MaxLostCount;


        public CancellationTokenSource _cancelToken;



        private readonly IPIDController _pitchControl;
        private readonly IPIDController _yawControl;
        private readonly IPIDController _heightControl;
        private readonly IPIDController _rollControl;

        private readonly IConfiguration _config;



        public int _droneLostCount;
        public Thread _flightThread;
        public Thread _droneControlThread;
        public Thread _positionLoggingThread;

        private float _currentYawAngleDiff;

        private readonly float _minAnglePitchStart;
        private readonly float _maxAnglePitchStart;
        private readonly float _minAngleRollStart;
        private readonly float _maxAngleRollStart;


        private const float MinusHalfOfPi = -MathF.PI / 2;
        private const float HalfOfPi = MathF.PI / 2;

        private DroneManager DroneManager;

        internal void SetVirtualObject(GameObject gameObject)
        {
            this._virtualObject = gameObject;
        }

        public const int RetryConnectTimeMs = 10 * 1000;


        private int CommandCount = 0;

        // this is where the drone front points to
        Vector3 frontViewingDirection = Vector3.UnitZ;
        // this is where the drone's right side points to
        Vector3 sideViewingDirection = -Vector3.UnitX;


        public bool IsStarted { get; internal set; }

        public Drone(string name, IConfiguration config, DroneManager droneManager)
        {

            _config = config;

            this.Name = name;
            this.DroneManager = droneManager;
            this._MaxTilt = _config.GetValue<int>("flight.controller:drones:" + this.Name + ":max.tilt");

            this.Id = _config.GetValue<int>("flight.controller:drones:" + this.Name + ":id");

            _pitchControl = new PIDController();
            _yawControl = new PIDController();
            _heightControl = new PIDController();
            _rollControl = new PIDController();

            InitHeightControl();
            InitYawControl();
            InitPitchControl();
            InitRollControl();

            _cancelToken = new CancellationTokenSource();

            _minAnglePitchStart = _config.GetValue<int>("flight.controller:drones:" + this.Name + ":pitch:min.start.angle.deg");
            _maxAnglePitchStart = _config.GetValue<int>("flight.controller:drones:" + this.Name + ":pitch:max.start.angle.deg");
            _minAngleRollStart = _config.GetValue<int>("flight.controller:drones:" + this.Name + ":roll:min.start.angle.deg");
            _maxAngleRollStart = _config.GetValue<int>("flight.controller:drones:" + this.Name + ":roll:max.start.angle.deg");


            var prefix = "flight.controller:drones:" + this.Name + ":auto.assignment";
            var isAutoAssignmentEnabled = _config.GetValue<bool>($"{prefix}:enabled");
            if (isAutoAssignmentEnabled)
            {
                var trackedObjectName = _config.GetValue<string>($"{prefix}:tracked.object.name");
                var virtualObjectName = _config.GetValue<string>($"{prefix}:virtual.object:name");
                var x = _config.GetValue<int>($"{prefix}:virtual.object:x");
                var y = _config.GetValue<int>($"{prefix}:virtual.object:y");
                var z = _config.GetValue<int>($"{prefix}:virtual.object:z");

                _trackedObject = new MocapFrame.Types.Body()
                {
                    Name = trackedObjectName,
                    Centroid = new MocapFrame.Types.Body.Types.Vector3(),
                    Rotation = new MocapFrame.Types.Body.Types.Quaternion()
                };
                _virtualObject = new GameObject()
                {
                    Id = virtualObjectName,
                    Position = new GameObject.Types.Vector3() { X = x, Y = y, Z = z },
                    Rotation = new GameObject.Types.Quaternion() { X = 0, Y = 0, Z = 0, W = 1 },
                    Eulers = new GameObject.Types.Eulers() { X = 0, Y = 0, Z = 0 }
                };
            }


            Logger.Info("Init drone: " + name + " id: " + this.Id);
        }

        public void Start()
        {
            if (IsStarted)
            {
                Logger.Warn("Drone controller already running");
                return;
            }
            StartFlight();
        }

        public void StartFlight()
        {
            if (_flightThread != null)
            {
                Logger.Warn("Flight already started");
                return;
            }

            if (_cancelToken.IsCancellationRequested)
            {
                Logger.Warn("Cancel requested, not starting");
                return;
            }

            if (_virtualObject == null || _trackedObject == null)
            {
                Logger.Warn("No assignment from virtual to physical object");
                return;
            }

            _flightThread = new Thread(FlightThreadRunner);
            _flightThread.Start();

            Logger.Info("Flight thread started for: " + this.Name);

            IsStarted = true;
        }

        private void FlightThreadRunner()
        {

            var connected = false;
            while (!_cancelToken.IsCancellationRequested && !connected)
            {
                var connectParams = new ConnectParams()
                {
                    DroneId = this.Id,
                    NumTries = 3,
                    MaxTilt = (int)_MaxTilt
                };

                Logger.Info("Connection Params: " + connectParams);
                var connectResponse = Connect(connectParams);
                connected = connectResponse.Connected;
                connected = true;
                if (connected)
                {
                    Logger.Info("Connected to drone {0}", this.Id);


                    //LogFlightStart();

                    /*
                     * Use the splash event so that the control loop
                     * is started after the logging thread has started
                     */
                    /*
                   var splash = new ManualResetEvent(false);
                   _positionLoggingThread = new Thread(PositionLoggingLoop);
                   _positionLoggingThread.Start(splash);
                   splash.WaitOne();
                   */

                    _droneControlThread = new Thread(ControlLoop);
                    _droneControlThread.Start();
                }
                else
                {
                    Logger.Info("Could not connect to {0} retrying in {1} sec", this.Id, RetryConnectTimeMs / 1000);
                    try
                    {
                        Thread.Sleep(RetryConnectTimeMs);
                    }
                    catch (Exception e)
                    {
                        Logger.Info("Interrupted on sleep");
                        Logger.Error(e);
                    }
                }
            }
        }

        private void OnRealWorldStart(object sender, GameObject gameObject)
        {
            Logger.Info("Start called by rpc service");
            if (IsStarted)
            {
                Logger.Warn("Drone controller already running");
                return;
            }
            _virtualObject = gameObject;
            StartFlight();
        }

        private void OnRealWorldStop(object sender, GameObject gameObject)
        {
            Logger.Info("Stop called by rpc service");
            _cancelToken.Cancel();
            _cancelToken = new CancellationTokenSource();
        }


        private void InitPidController(IPIDController controller,
            double kp, double ki, double kd,
            double windupGuard,
            double outMin, double outMax,
            int sampleTimeMs)
        {

            Logger.Info("controller: " + kp + " " + ki + " " + kd + " " + outMin + " " + outMax + " " + sampleTimeMs);
            controller.Init(kp, ki, kd, outMin, outMax, sampleTimeMs);
            if (windupGuard > 0)
            {
                controller.WindupGuard = windupGuard;
            }
        }
        private void InitHeightControl()
        {
            Logger.Info("InitHeightControl");
            InitPidController(_heightControl,
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":height:kp"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":height:ki"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":height:kd"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":height:windup.guard"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":height:out.min"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":height:out.max"),
                _config.GetValue<int>("flight.controller:drones:" + this.Name + ":height:sample.time.millis")
            );
        }

        private void InitPitchControl()
        {
            InitPidController(_pitchControl,
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":pitch:kp"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":pitch:ki"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":pitch:kd"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":pitch:windup.guard"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":pitch:out.min"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":pitch:out.max"),
                _config.GetValue<int>("flight.controller:drones:" + this.Name + ":pitch:sample.time.millis")
            );
        }

        private void InitYawControl()
        {
            InitPidController(_yawControl,
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":yaw:kp"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":yaw:ki"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":yaw:kd"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":yaw:windup.guard"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":yaw:out.min"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":yaw:out.max"),
                _config.GetValue<int>("flight.controller:drones:" + this.Name + ":yaw:sample.time.millis")
            );
        }

        private void InitRollControl()
        {
            InitPidController(_rollControl,
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":roll:kp"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":roll:ki"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":roll:kd"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":roll:windup.guard"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":roll:out.min"),
                _config.GetValue<double>("flight.controller:drones:" + this.Name + ":roll:out.max"),
                _config.GetValue<int>("flight.controller:drones:" + this.Name + ":roll:sample.time.millis")
            );
        }


        private ConnectResult Connect(ConnectParams connectParams)
        {
            try
            {
                Logger.Info("Connecting to drone {0}", connectParams);
                var response = this.DroneManager.CallDroneControlApi("connect", connectParams.ToByteArray());
                return ConnectResult.Parser.ParseFrom(response);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not connect to drone; cause: {0}", e.Message);
                return new ConnectResult() { Connected = false };
            }
        }

        private void TakeOff(int droneId)
        {
            Logger.Info("Taking off drone id: {0}", droneId);
            var timeout = new Timeout() { Seconds = 2 };
            var takeoffParams = new TakeOffParams()
            {
                DroneId = this.Id,
                Timeout = timeout
            };
            this.DroneManager.CallDroneControlApi("takeoff", takeoffParams.ToByteArray());

            this.DroneManager._unityRpcClient.OnTakeOff(_virtualObject);

            Logger.Info("Drone id: " + this.Id + " took off!");
        }

        private void Land(int droneId)
        {
            try
            {
                Logger.Info("Landing drone {0}", droneId);
                var timeout = new Timeout() { Seconds = 2 };
                var landParams = new LandParams()
                {
                    DroneId = this.Id,
                    Timeout = timeout
                };
                this.DroneManager.CallDroneControlApi("land", landParams.ToByteArray(), forceWait: true);

                this.DroneManager._unityRpcClient.OnLand(_virtualObject);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not land drone; cause {0}", e.Message);
            }
        }

        public void Disconnect(int droneId)
        {
            try
            {
                Logger.Info("Disconnecting from drone {0}", droneId);
                var disconnectParams = new DisconnectParams() { DroneId = this.Id };
                this.DroneManager.CallDroneControlApi("disconnect", disconnectParams.ToByteArray(), forceWait: true);
                Logger.Info("Disconnected from drone {0}", droneId);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not disconnect from drone; cause {0}", e.Message);
            }
        }

        private void ControlLoop()
        {
            Logger.Info("Control started");
            TakeOff(this.Id);

            while (!_cancelToken.IsCancellationRequested)
            {
                // do some heavy work
                CalculateMovement();
                //Logger.Info("Calcualting movement");
                // see thread sleep doc for value of 0
                Thread.Sleep(0);
            }
            Land(this.Id);
        }

        private void CalculateMovement()
        {
            if (IsDroneLost)
            {
                _yaw = _pitch = _roll = _verticalMovement = 0;
                return;
            }

            _yaw = CalculateYaw();
            _pitch = CalculatePitch();
            _roll = CalculateRoll();
            _verticalMovement = CalculateVerticalMovement();

            if (CommandCount % 100 == 0)
            {
                var ip = _config.GetValue<string>("flight.controller:drone_control_bridge_ip");
                var port = _config.GetValue<int>("flight.controller:drone_control_bridge_port");

                string droneApiUrl = $"http://{ip}:{port}/setcommand";
                WebRequest request = WebRequest.Create(droneApiUrl);
                // Set the Method property of the request to POST.
                request.ContentType = "application/json";
                request.Method = "POST";

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json =
                        "{\"DroneId\":\"" + this.Id + "\",\"Roll\":\"" + _roll + "\",\"Pitch\":\"" + _pitch + "\",\"Yaw\":\"" + _yaw + "\",\"VerticalMovement\":\"" + _verticalMovement + "\"}";
                    Logger.Info(json);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                }
                var httpResponse = (HttpWebResponse)request.GetResponse();
                CommandCount = 1;
                Logger.Info("DroneName: " + this.Name + " roll: " + _roll + " pitch: " + _pitch + " yaw: " + _yaw + " ver mov: " + _verticalMovement);
            }
            CommandCount++;

        }

        private int CalculatePitch()
        {
            var angleDiffDeg = _currentYawAngleDiff.ToDegrees();
            if (angleDiffDeg < _minAnglePitchStart ||
                angleDiffDeg > _maxAnglePitchStart)
            {
                return 0;
            }

            var position = GetCurrentPosition();
            var goal = GetDesiredPosition();
            var rotation = _trackedObject.Rotation.ToNumerics();

            // Rotate the viewing direction according to the tracked object rotation.
            var localViewingDirection = Vector3.Transform(frontViewingDirection, rotation);

            /*
            the viewing direction has to be adjusted so that the distance
            between the position and the goal is correctly calculated.
            */
            var adjustment = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 90f.ToRadians());
            var perpendicularViewingDirection = Vector3.Transform(localViewingDirection, adjustment);

            var isAhead = IsAhead(position, localViewingDirection, goal);
            var multiplier = isAhead ? -1 : 1;

            /*
            calculate the distance between the current position and the
            line that uses the goal as anchor point and the viewing direction
            as direction vector
            https://www.mathematik-oberstufe.de/vektoren/a/abstand-punkt-gerade-lfdpkt.html
             */
            var distance = position.DistanceToLine(goal, perpendicularViewingDirection);

            return multiplier * Convert.ToInt32(_pitchControl.Update(distance, 0));
        }

        private int CalculateRoll()
        {
            var angleDiffDeg = _currentYawAngleDiff.ToDegrees();
            if (angleDiffDeg < _minAngleRollStart ||
                angleDiffDeg > _maxAngleRollStart)
            {
                return 0;
            }

            var position = GetCurrentPosition();
            var goal = GetDesiredPosition();
            var rotation = _trackedObject.Rotation.ToNumerics();

            Logger.Info("position: " + (int)position.X + ", " + (int)position.Y + ", " + (int)position.Z);

            // Rotate the side according to the tracked object rotation.
            var localViewingDirection = Vector3.Transform(sideViewingDirection, rotation);

            /*
            the viewing direction has to be adjusted so that the distance
            between the position and the goal is correctly calculated.
            */
            var adjustment = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 90f.ToRadians());
            var perpendicularViewingDirection = Vector3.Transform(localViewingDirection, adjustment);

            var isAhead = IsAhead(position, localViewingDirection, goal);
            var multiplier = isAhead ? -1 : 1;

            /*
            calculate the distance between the current position and the
            line that uses the goal as anchor point and the viewing direction
            as direction vector
            https://www.mathematik-oberstufe.de/vektoren/a/abstand-punkt-gerade-lfdpkt.html
             */
            var distance = position.DistanceToLine(goal, perpendicularViewingDirection);

            return multiplier * Convert.ToInt32(_rollControl.Update(distance, 0));
        }

        private int CalculateVerticalMovement()
        {
            var pv = GetCurrentHeight();
            var sp = GetDesiredHeight();

            //Logger.Info("Current Height: " + pv + " Desired height: " + sp);

            // no movement required if the error is nan
            if (double.IsNaN(pv) || double.IsNaN(sp)) return 0;

            return Convert.ToInt32(_heightControl.Update(pv, sp));
        }

        private int CalculateYaw()
        {
            if (_trackedObject == null || _virtualObject == null)
            {
                return 0;
            }

            var srcRotation = _trackedObject.Rotation.ToNumerics();
            var dstRotation = _virtualObject.Rotation.ToNumerics();

            //Logger.Info("srcRotation: " + srcRotation + " dstRotation: " + dstRotation);

            // Rotate the viewing direction according to the tracked object rotation.
            var localViewingDirection = Vector3.Transform(frontViewingDirection, srcRotation);

            // same goes to the destination object
            var dstViewDirection = Vector3.UnitZ;
            dstViewDirection = Vector3.Transform(dstViewDirection, dstRotation);

            // calculate the angle after rotate and translate
            _currentYawAngleDiff = localViewingDirection.AngleBetween(dstViewDirection);

            var pv = _currentYawAngleDiff.Translate(-MathF.PI, MathF.PI,
                Convert.ToSingle(_yawControl.OutputMin), Convert.ToSingle(_yawControl.OutputMax));

            // the final angle should be zero
            const int sp = 0;

            // no movement required if the error is nan
            if (double.IsNaN(pv) || double.IsNaN(sp)) return 0;

            // invert steering otherwise the drone navigates to the opposite direction
            int yaw = Convert.ToInt32(-1 * _yawControl.Update(pv, sp));
            //Console.WriteLine("yaw= " + yaw);
            return yaw;
        }

        private static bool IsAhead(Vector3 origin, Vector3 viewingDirection, Vector3 goal)
        {
            /*
            Transform the goal so it can ce treaded as if the origin is
            inside the zero point.
            */
            var v = goal - origin;

            var angle = viewingDirection.AngleBetween(v);

            /*
            If the angle between both vector is larger than -90 degrees
            and less than 90 degress the goal is ahead of the origin
            */
            return angle > MinusHalfOfPi && angle < HalfOfPi;
        }


        private double GetCurrentHeight()
        {
            return _trackedObject?.Centroid.Y ?? double.NaN;
        }

        private double GetDesiredHeight()
        {
            return _virtualObject?.Position.Y ?? double.NaN;
        }

        private Vector3 GetCurrentPosition()
        {
            if (_trackedObject != null)
            {
                return new Vector3(_trackedObject.Centroid.X,
                    _trackedObject.Centroid.Y,
                    _trackedObject.Centroid.Z);
            }

            return Vector3.Zero;
        }

        private Vector3 GetDesiredPosition()
        {
            if (_virtualObject != null)
            {
                return new Vector3(_virtualObject.Position.X,
                    _virtualObject.Position.Y,
                    _virtualObject.Position.Z);
            }

            return Vector3.Zero;
        }


        public void StopThread(Thread t, string warningMessage, string successMessage = null)
        {
            try
            {
                if (t != null && t.IsAlive)
                {
                    t.Join();
                }

                if (successMessage != null)
                {
                    Logger.Debug(successMessage);
                }
            }
            catch (ThreadInterruptedException)
            {
                Logger.Warn(warningMessage);
            }
        }

    }



}
