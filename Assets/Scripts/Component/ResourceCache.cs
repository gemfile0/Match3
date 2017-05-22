using UnityEngine;
using System.Collections.Generic;

public static class ResourceCache 
{
	static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
	static List<string> loadedPath = new List<string>();

	static public void LoadAll(string path) 
	{
		if (loadedPath.Contains(path)) { return; }

		loadedPath.Add(path);
		foreach (var resource in Resources.LoadAll(path, typeof(GameObject)))
		{
			cache[resource.name] = (GameObject)resource;
		}
	}

	static public void Load(string path)
	{
		var resource = Resources.Load(path, typeof(GameObject));
		cache[resource.name] = (GameObject)resource;
	}

	static public GameObject Instantiate(string key, Transform parent = null, bool worldPositionStays = true)
	{
		var instance = Object.Instantiate<GameObject>(cache[key]);
		instance.name = key;
		if (parent) { instance.transform.SetParent(parent, worldPositionStays); }
		return instance;
	}

	static public T Instantiate<T>(string key, Transform parent = null) 
		where T: PooledObject
	{
		// Debug.Log("Instantiate : " + key);
		var prefab = cache[key].GetComponent<T>();
		T instance = prefab.GetPooledInstance<T>();
		instance.transform.name = key;

		if (parent) { instance.transform.SetParent(parent); }
		return instance;
	}
}