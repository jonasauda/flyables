using HaptiOS.Src.PID;
using HaptiOS.Src.Serialization;
using HaptiOS.Src.Udp;
using HaptiOS.Src.Vinter;
using Ninject.Modules;
using VinteR.Model.Gen;
using HaptiOS.Src.DroneControl;
using HaptiOS.Src.RealWorldRpc;

namespace HaptiOS.Src
{
    public class HaptiosModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IDroneManager>().To<DroneManager>().InSingletonScope();
            Bind<IUdpSender>().To<UdpSender>();
            Bind<IUdpReceiver>().To<UdpReceiver>();
            Bind<IVinterClient>().To<VinterClient>().InSingletonScope();
            Bind<IDeserializer<MocapFrame>>().To<MocapFrameDeserializer>();
            Bind<IDeserializer<GameObject>>().To<GameObjectDeserializer>();
            Bind<IPIDController>().To<PIDController>();
            Bind<IRealWorldServicer>().To<RealWorldServicer>();
        }
    }
}