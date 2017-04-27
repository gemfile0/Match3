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

	static public GameObject Get(string key) 
	{
		return cache[key];
	}

	static public GameObject Instantiate(string key, Transform parent = null) 
	{
		// Debug.Log("Instantiate : " + key);
		var instance = Object.Instantiate<GameObject>(cache[key]);
		instance.name = key;

		if (parent) 
		{
			instance.transform.SetParent(parent);
		}
		return instance;
	}
}