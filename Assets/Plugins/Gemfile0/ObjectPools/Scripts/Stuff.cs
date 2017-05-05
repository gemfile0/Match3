using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Stuff: PooledObject 
{
	public Rigidbody Body { get; private set; }
	MeshRenderer[] meshRenderers;

	public void SetMaterial(Material m) 
	{
		for (int i = 0; i < meshRenderers.Length; i++)
		{
			meshRenderers[i].material = m;
		}
	}

	void Awake () 
	{
		Body = GetComponent<Rigidbody>();	
		meshRenderers = GetComponentsInChildren<MeshRenderer>();
	}

	void OnTriggerEnter(Collider enteredCollider)
	{
		if (enteredCollider.CompareTag("Kill Zone"))
		{
			ReturnToPool();
		}
	}

	void OnLevelWasLoaded()
	{
		ReturnToPool();
	}
}
