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

	public static GOTween GOLocalMove(this Transform transform, Vector3 endValue, float duration)
	{
		return GOTween.To(
			() => transform.localPosition, 
			value => transform.localPosition = value, 
			endValue,
			duration
		);
	}
}   