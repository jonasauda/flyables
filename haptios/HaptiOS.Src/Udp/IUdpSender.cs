using System;
using System.Net;

namespace HaptiOS.Src.Udp
{
    /// <summary>
    /// An udp sender is able to send raw bytes of data to an end point.
    /// </summary>
    public interface IUdpSender : IDisposable
    {
        /// <summary>
        /// Send raw bytes of data to given endpoint
        /// </summary>
        /// <param name="data"></param>
        /// <param name="receiver"></param>
        void Send(byte[] data, IPEndPoint receiver);
    }
}