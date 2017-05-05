using UnityEngine;
using System.Collections.Generic;

public static class ResourceCache 
{
	static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

	static public void LoadAll(string path) 
	{
		Resources.LoadAll(path, typeof(GameObject)).ForEach(resource => {
			cache[resource.name] = (GameObject)resource;
		});
	}

	static public void Load(string path)
	{
		var resource = Resources.Load(path, typeof(GameObject));
		cache[resource.name] = (GameObject)resource;
	}

	static public GameObject Get(string key) 
	{
		return cache[key];
	}

	static public GameObject Instantiate(string key, Transform parent = null)
	{
		var instance = Object.Instantiate<GameObject>(cache[key]);
		instance.name = key;
		if (parent) { instance.transform.SetParent(parent); }
		return instance;
	}

	static public T Instantiate<T>(string key, Transform parent = null) 
		where T: PooledObject
	{
		// Debug.Log("Instantiate : " + key + ", " + prefab);
		var prefab = cache[key].GetComponent<T>();
		T instance = prefab.GetPooledInstance<T>();
		instance.transform.name = key;

		if (parent) { instance.transform.SetParent(parent); }
		return instance;
	}
}