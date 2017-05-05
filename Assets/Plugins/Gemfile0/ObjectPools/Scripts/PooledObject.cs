using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledObject: MonoBehaviour {

	public ObjectPool Pool { get; set; }
	[System.NonSerializedAttribute]
	ObjectPool poolInstanceForPrefab;

	public virtual void ReturnToPool()
	{
		 if (Pool) { Pool.AddObject(this); }
		 else { Destroy(gameObject); }
	}

	public T GetPooledInstance<T>() where T: PooledObject
	{
		if (!poolInstanceForPrefab)
		{
			poolInstanceForPrefab = ObjectPool.GetPool(this);
		}
		return (T)poolInstanceForPrefab.GetObject();
	}
}
