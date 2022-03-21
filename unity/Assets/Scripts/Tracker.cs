using System;
using System.Linq;
using UnityEngine;

public class Tracker : MonoBehaviour
{
	[Header("General")]
	[Tooltip("GameObject with Vinter Receiver Script")]
	public VinterReceiver vinterReceiver;
	[Tooltip("Motive Name of the RigidBody")]
	public string motiveName;
	[Tooltip("Position offset from the tracked Centroid")]
	public Vector3 offset;
	[Tooltip("If the tracked object should be rotated")]
	public bool positionOnly;
	[Tooltip("If the tracked object is a LeapMotion Hand Rig")]
	public bool isLeapHands;

	[Header("Dampening")]
	[Tooltip("If and how to dampen position and rotation")]
	public DampeningFunction dampeningFunction = DampeningFunction.None;
	[Tooltip("Number of Frames to dampen over")]
	public int dampeningBufferSize;

	public enum DampeningFunction
	{
		None,
		Mean,
		Median
	}
	
	[Header("Map Init Object")]
	[Tooltip("If true, the Map will be initially set to the position of this object, used to move the play area to a specific object")]
	public bool isInitPoint;
	[Tooltip("Initial map offset")]
	public Vector3 initOffset;
	[Tooltip("The Map to initiate")]
	public GameObject map;
		
	private Vector3 _position;
	private Vector3 _rotation;
	
	private int _frameCounter;
	private Vector3[] _positionBuffer;
	private Vector3[] _rotationBuffer;
	private int _lastDampeningBufferSize;
	
	private bool _initMapPosition = true;
	private bool _isStartSet = true;

	// Use this for initialization
	private void Start () {
	
		_frameCounter = 0;
		
		_positionBuffer = new Vector3[dampeningBufferSize];
		_rotationBuffer = new Vector3[dampeningBufferSize];
	}

	private void FixedUpdate()
	{
		var moCapFrame = vinterReceiver.GetCurrentMoCapFrame();
		if (moCapFrame != null)
		{
			var body = moCapFrame.Bodies.SingleOrDefault(b => b.Name.Equals(motiveName));
			if (body != null)
			{
				// Extract position and rotation from MoCapFrame
				//Assuming z forward, x left, y up. invert axis to change
				_position = new Vector3(-body.Centroid.X * 0.001f + offset.x, body.Centroid.Y * 0.001f + offset.y,
					body.Centroid.Z * 0.001f + offset.z);

				var quaternion = new Quaternion(-body.Rotation.X, body.Rotation.Y, body.Rotation.Z, -body.Rotation.W);
				_rotation = new Vector3(quaternion.eulerAngles.x, quaternion.eulerAngles.y, quaternion.eulerAngles.z);


				if (isInitPoint && _initMapPosition)
				{
					// init the Map to the position of the tracked object
					_initMapPosition = false;

					map.transform.position = _position + initOffset;
					map.transform.rotation = Quaternion.Euler(0, quaternion.eulerAngles.y, 0);
				}

				switch (dampeningFunction)
				{
					case DampeningFunction.Mean:
						WriteToBuffer();
						Mean();
						break;
					case DampeningFunction.Median:
						WriteToBuffer();
						Median();
						break;
					default:
						SetTransform();
						break;
				}
			}
		}
	}

	private void WriteToBuffer()
	{
		// Reset the arrays when the number of frames to damp over is changed
		if (dampeningBufferSize != _lastDampeningBufferSize)
		{
			_positionBuffer = new Vector3[dampeningBufferSize];
			_rotationBuffer = new Vector3[dampeningBufferSize];
		}
			
		// insert new position and rotation in the buffer
		_frameCounter %= dampeningBufferSize;
		_positionBuffer[_frameCounter] = _position;
		_rotationBuffer[_frameCounter] = _rotation;
		_lastDampeningBufferSize = dampeningBufferSize;
		_frameCounter++;
	}
	
	private void SetTransform()
	{
		transform.localPosition = _position;
		if (!positionOnly && !isLeapHands)
		{
			transform.localRotation = Quaternion.Euler(_rotation);
		}
		else if (isLeapHands)
		{
			transform.localPosition = new Vector3(_position.x,0, _position.z);
			transform.localRotation = Quaternion.Euler(0, _rotation.y, 0);
		}
		else if (_isStartSet)
		{
			transform.localPosition = _position;
			transform.localRotation = Quaternion.Euler(0, _rotation.y, 0);
			_isStartSet = false;
		}
	}

	private void Mean()
	{
		var posX = 0f;
		var posY = 0f;
		var posZ = 0f;
		foreach (var position in _positionBuffer)
		{
			posX += position.x;
			posY += position.y;
			posZ += position.z;
		}
		_position = new Vector3(posX, posY, posZ) / dampeningBufferSize;
                				
		var rotX = 0f;
		var rotY = 0f;
		var rotZ = 0f;
		foreach (var rotation in _rotationBuffer)
		{
			rotX += rotation.x;
			rotY += rotation.y;
			rotZ += rotation.z;
		}
		_rotation = new Vector3(rotX, rotY, rotZ) / dampeningBufferSize;
		SetTransform();
	}

	private void Median()
	{
		var posX = new float[_positionBuffer.Length];
		var posY = new float[_positionBuffer.Length];
		var posZ = new float[_positionBuffer.Length];
		
		var rotX = new float[_positionBuffer.Length];
		var rotY = new float[_positionBuffer.Length];
		var rotZ = new float[_positionBuffer.Length];
		
		for (var i = 0; i < _positionBuffer.Length; i++)
		{
			posX[i] = _positionBuffer[i].x;
			posY[i] = _positionBuffer[i].y;
			posZ[i] = _positionBuffer[i].z;
			
			rotX[i] = _rotationBuffer[i].x;
			rotY[i] = _rotationBuffer[i].y;
			rotZ[i] = _rotationBuffer[i].z;
		}
		
		Array.Sort(posX);
		Array.Sort(posY);
		Array.Sort(posZ);
		
		Array.Sort(rotX);
		Array.Sort(rotY);
		Array.Sort(rotZ);
		
		
		var m = _positionBuffer.Length / 2;
		
		if (_positionBuffer.Length % 2 == 0)
		{
			_position = new Vector3(
				(posX[m] + posX[m-1])/2,
				(posY[m] + posY[m-1])/2,
				(posZ[m] + posZ[m-1])/2
				);
			
			_rotation =new Vector3(
				(rotX[m] + rotX[m-1])/2,
				(rotY[m] + rotY[m-1])/2,
				(rotZ[m] + rotZ[m-1])/2
				);
		}
		else
		{
			_position = new Vector3(posX[m], posY[m], posZ[m]);
			_rotation = new Vector3(rotX[m], rotY[m], rotZ[m]);
		}
		SetTransform();
	}
}
