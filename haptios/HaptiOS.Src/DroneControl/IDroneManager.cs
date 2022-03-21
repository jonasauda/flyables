using System;
using System.Collections.Generic;

namespace HaptiOS.Src.DroneControl
{
    public interface IDroneManager
    {
        bool IsStarted { get; }

        bool IsStartedDrone(String droneName);

        void Start();

        void Stop();

        void StartDrone(string DroneName);

        void StopDrone(string DroneName);

        List<string> GetDroneNames();

        HaptiOS.DroneControl GetFlyCommand(int droneId);

    }
}
