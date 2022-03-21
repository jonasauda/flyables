using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace HaptiOS.Src.Udp
{
    public class UdpReceiver : IUdpReceiver
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public event EventHandler<byte[]> OnDataReceived;

        private UdpClient _receiver;

        private int _localPort;

        private CancellationTokenSource _cancelToken = new CancellationTokenSource();

        /// <summary>
        /// Used to determine if this client has started the listening thread
        /// and is active.
        /// </summary>
        private volatile bool _isRunning;

        /// <inheritdoc />
        /// <summary>
        /// Connects this instance to a running udp sender instance if this
        /// client is not already running. If it is, nothing is done.
        /// Be sure to call <code>Disconnect()</code> before a new
        /// connection should be established.
        /// </summary>
        /// <param name="localPort">Local port to receive packages on</param>
        public void StartListening(int localPort)
        {
            if (_isRunning) return;

            _localPort = localPort;

            var runner = new Thread(Receive) { IsBackground = true };
            runner.Start();
        }

        private void Receive()
        {
            _isRunning = true;
            try
            {
                // start the receiver
                _receiver = new UdpClient(_localPort);
                Logger.Info("Udp receiver thread started for port {0}", _localPort);
            }
            catch (SocketException e)
            {
                // stop if any exception occured, for example
                // if the port is already in use
                _isRunning = false;

                Logger.Error("Could not create udp client on port {0}, cause: {1}", _localPort, e.Message);
                return;
            }

            try
            {
                var me = new IPEndPoint(IPAddress.Any, _localPort);

                // run until Disconnect() is called
                while (!_cancelToken.IsCancellationRequested)
                {
                    if (_receiver.Available > 0)
                    {
                        var data = _receiver?.Receive(ref me);
                        OnDataReceived?.Invoke(this, data);
                    }
                    Thread.Sleep(0);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            finally
            {
                _receiver?.Close();
                _cancelToken = new CancellationTokenSource();
                _isRunning = false;
            }
            Logger.Info("Udp receiver thread stopped for port {0}", _localPort);
        }

        public void Stop()
        {
            _cancelToken.Cancel();
            _receiver.Close();
            _receiver = null;
        }
    }
}