using System;
using VinteR.Model.Gen;

namespace HaptiOS.Src.Vinter
{
    public interface IVinterClient
    {
        event EventHandler<MocapFrame> OnMocapFrameReceived;

        void Start();

        void Stop();
    }
}