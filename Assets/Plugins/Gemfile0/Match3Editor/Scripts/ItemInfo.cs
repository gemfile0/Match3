#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemInfo: MonoBehaviour 
{
	public int ID;
	Vector3 size;

	void OnEnable()
	{
		size = GetComponent<SpriteRenderer>().bounds.size;
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(transform.localPosition, size);
	}
}
#endif
