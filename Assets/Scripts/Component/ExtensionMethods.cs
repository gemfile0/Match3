using System.Collections.Generic;
using UnityEngine;
using System;

public static class ExtensionMethods 
{
	public static Bounds GetBounds(this GameObject gameObject) 
	{
		var bounds = new Bounds (gameObject.transform.position, Vector3.zero);
		foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>()) 
		{
			bounds.Encapsulate(renderer.bounds);
		}
		return bounds;
	}
}   