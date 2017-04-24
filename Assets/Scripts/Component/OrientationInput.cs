using UnityEngine;
using UnityEngine.Events;

public interface IOrientationInput
{
	OrientationEvent OnOrientationChange { get; }
}

public class OrientationEvent: UnityEvent {}
public class OrientationInput: MonoBehaviour, IOrientationInput
{
	public OrientationEvent OnOrientationChange { get { return onOrientationChange; } }
	readonly OrientationEvent onOrientationChange = new OrientationEvent();
	DeviceOrientation currentOrientation;
	int screenWidth;
	int screenHeight;

	void Awake()
	{
		ReadScreenSize();
	}
	
	void Update () 
	{
		if (screenWidth != Screen.width && screenHeight != Screen.height)
		{
			ReadScreenSize();
			onOrientationChange.Invoke();
		}
	}

	void ReadScreenSize()
	{
		screenWidth = Screen.width;
		screenHeight = Screen.height;
	}
}
