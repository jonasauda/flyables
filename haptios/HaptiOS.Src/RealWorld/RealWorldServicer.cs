using System;
using System.Threading.Tasks;

namespace HaptiOS.Src.RealWorldRpc
{
    public class RealWorldServicer : RealWorld.RealWorldBase, IRealWorldServicer
    {
        public event EventHandler<GameObject> OnStart;
        public event EventHandler<GameObject> OnStop;

        public override Task<Empty> Start(GameObject gameObject, Grpc.Core.ServerCallContext context)
        {
            OnStart?.Invoke(this, gameObject);
            return Task.FromResult(new Empty() {Status = 200});
        }

        public override Task<Empty> Stop(GameObject gameObject, Grpc.Core.ServerCallContext context)
        {
            OnStop?.Invoke(this, gameObject);
            return Task.FromResult(new Empty() {Status = 200});
        }
    }
}