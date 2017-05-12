using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool: MonoBehaviour 
{
	PooledObject prefab;
	List<PooledObject> availableObjects = new List<PooledObject>();

	public PooledObject GetObject()
	{
		PooledObject pooledObject;
		int lastAvailableIndex = availableObjects.Count - 1;
		if (lastAvailableIndex >= 0)
		{
			pooledObject = availableObjects[lastAvailableIndex];
			availableObjects.RemoveAt(lastAvailableIndex);
			pooledObject.gameObject.SetActive(true);
		}
		else
		{
			pooledObject = Instantiate<PooledObject>(prefab);
			pooledObject.transform.SetParent(transform, false);
			pooledObject.Pool = this;
		}
		
		return pooledObject;
	}

	public void AddObject(PooledObject pooledObject)
	{
		pooledObject.gameObject.SetActive(false);
		pooledObject.transform.SetParent(transform, true);
		availableObjects.Add(pooledObject);
	}

	public static ObjectPool GetPool(PooledObject prefab)
	{
		GameObject gameObject;
		ObjectPool objectPool;
		if (Application.isEditor)
		{
			gameObject = GameObject.Find(prefab.name + "Pool");
			if (gameObject) {
				objectPool = gameObject.GetComponent<ObjectPool>();
				if (gameObject) { return objectPool; }
			}
		}

		var root = GameObject.Find("ObjectPool");
		if (root == null) 
		{ 
			root = new GameObject("ObjectPool"); 
			// DontDestroyOnLoad(root);
		}

		gameObject = new GameObject(prefab.name + "Pool");
		gameObject.transform.SetParent(root.transform);
		objectPool = gameObject.AddComponent<ObjectPool>();
		objectPool.prefab = prefab;
		return objectPool;
	}
}
