using System;
using HaptiOS.Src.Serialization;
using HaptiOS.Src.Udp;
using Microsoft.Extensions.Configuration;
using VinteR.Model.Gen;

namespace HaptiOS.Src.Vinter
{
    public class VinterClient : IVinterClient
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IUdpReceiver _vinterReceiver;
        private readonly IConfiguration _config;
        private readonly IDeserializer<MocapFrame> _mocapFrameDeserializer;
        private bool _started;

        public event EventHandler<MocapFrame> OnMocapFrameReceived;

        public VinterClient(IConfiguration config, IDeserializer<MocapFrame> mocapFrameDeserializer, IUdpReceiver vinterReceiver)
        {
            _config = config;
            _mocapFrameDeserializer = mocapFrameDeserializer;
            _vinterReceiver = vinterReceiver;
        }

        public void Start()
        {
            if (_started) return;

            _started = true;
            
            // connect to vinter and bind eventhandler
            var vinterReceiverPort = _config.GetValue<int>("vinter:local.port");
            _vinterReceiver.OnDataReceived += VinterReceiverFrameAvailable;
            _vinterReceiver.StartListening(vinterReceiverPort);
            Logger.Info("VinteR Client started listening!");
        }

        public void Stop()
        {
            if (OnMocapFrameReceived?.GetInvocationList().Length > 0)
            {
                Logger.Warn("Not stopping because OnMocapFrameReceived not empty");
                return;
            }

            _vinterReceiver.OnDataReceived -= VinterReceiverFrameAvailable;
            _vinterReceiver.Stop();

            _started = false;

            Logger.Info("Stopped");
        }

        private void VinterReceiverFrameAvailable(object sender, byte[] data)
        {
            //Logger.Info("VinterReceiverFrameAvailable");
            var mocapFrame = _mocapFrameDeserializer.Deserialize(data);
            OnMocapFrameReceived?.Invoke(this, mocapFrame);
        }
    }
}