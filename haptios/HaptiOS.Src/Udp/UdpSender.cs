using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HaptiOS.Src.Udp
{
    public class UdpSender : IUdpSender
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly UdpClient _udpServer;

        public UdpSender()
        {
            _udpServer = new UdpClient();
        }

        public void Send(byte[] data, IPEndPoint receiver)
        {
            /*
             * Use a task instead of a thread
             *
             * https://stackoverflow.com/questions/4130194/what-is-the-difference-between-task-and-thread
             */
            Task.Factory.StartNew(() =>
            {
                Logger.Debug("Sending to {0}:{1}", receiver.Address, receiver.Port);
                try
                {
                    _udpServer.Send(data, data.Length, receiver);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Could not send data to {0}", receiver.Address);
                }
            });
        }

        public void Dispose()
        {
            _udpServer?.Close();
            _udpServer?.Dispose();
            Logger.Info("Udp sender disposed");
        }
    }
}