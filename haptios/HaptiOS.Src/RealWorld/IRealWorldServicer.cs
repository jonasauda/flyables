using System;

namespace HaptiOS.Src.RealWorldRpc
{
    public interface IRealWorldServicer
    {
        event EventHandler<GameObject> OnStart;
        event EventHandler<GameObject> OnStop;
    }
}