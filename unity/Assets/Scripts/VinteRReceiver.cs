using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using VinteR.Model.Gen;

public class VinterReceiver : MonoBehaviour
{
	[Tooltip("The Port to listen on. Must be identical to configured receiver port in VinteR")]
	public int port = 3457;
    private IPEndPoint _optiTRackEndPoint;
    private UdpClient _optiTrackClient;
    private Thread _optiTrackListener;
    private MocapFrame _currentMoCapFrame;

    private CancellationTokenSource _cancellationToken;

    private void Start()
    {
        Debug.Log("Starting OptiTrack Listener...");
		_optiTRackEndPoint = new IPEndPoint(IPAddress.Any, port);
        _optiTrackClient = new UdpClient(_optiTRackEndPoint);
        _optiTrackListener = new Thread(ReceiveOptiTrackData)
        {
            IsBackground = true
        };
        _optiTrackListener.Start();
        _cancellationToken = new CancellationTokenSource();
        Debug.Log("Done!");
    }

    private void ReceiveOptiTrackData()
    {
        Debug.Log("Listening...");
        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                //Debug.Log(OptiTrackClient.ToString());
                var data = _optiTrackClient.Receive(ref _optiTRackEndPoint);
                _currentMoCapFrame = MocapFrame.Parser.ParseFrom(data);
            }
            catch (Exception e)
            {
                Debug.LogError("Receive data error " + e.Message);
                _optiTrackClient.Close();
                return;
            }
            Thread.Sleep(1);
        }
    }

    public MocapFrame GetCurrentMoCapFrame()
    {
        return _currentMoCapFrame?.Clone();
    }

    private void OnDestroy()
    {   
        _cancellationToken.Cancel();
        _optiTrackListener.Abort();
        if (_optiTrackClient != null)
            _optiTrackClient.Close();
        Debug.Log("Disconnected from server");
    }

    private void OnApplicationQuit()
    {
        _cancellationToken.Cancel();
        _optiTrackListener.Abort();
        if (_optiTrackClient != null)
            _optiTrackClient.Close();
    }
}
