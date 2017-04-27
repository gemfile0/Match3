using UnityEngine;

public class TestScene: BaseScene 
{
	protected override void Awake()
	{
		ResourceCache.LoadAll("");
	}
}
