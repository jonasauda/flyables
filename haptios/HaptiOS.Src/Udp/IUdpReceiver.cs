using System;

namespace HaptiOS.Src.Udp
{
    /// <summary>
    /// Classes that implement the <code>IUdpReceiver</code> listen on a specific
    /// port for messages from a specified sender endpoint. Received data can
    /// be accessed through an event handler.
    /// </summary>
    public interface IUdpReceiver
    {
        /// <summary>
        /// Called when a message is received from the remote server
        /// </summary>
        event EventHandler<byte[]> OnDataReceived;

        /// <summary>
        /// Start listening for messages. This is normally done inside a separate
        /// thread for long running operations. Be sure to call <see cref="Stop"/>
        /// when the receiver is not needed anymore.
        /// </summary>
        /// <param name="localPort">Port the <code>UdpClient</code> is listening on</param>
        void StartListening(int localPort);

        /// <summary>
        /// Stops listening for remote messages
        /// </summary>
        void Stop();
    }
}