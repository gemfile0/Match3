using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NucleonSpawner : MonoBehaviour 
{
	public Nucleon[] nucleonPrefabs;
	public float timeBetweenSpawns;
	public float spawnDistance;
	float timeSinceLastSpawn;
	
	void FixedUpdate()
	{
		timeSinceLastSpawn += Time.deltaTime;
		if (timeSinceLastSpawn >= timeBetweenSpawns) {
			timeSinceLastSpawn -= timeBetweenSpawns;
			SpawnNucleon();
		}
	}

	void SpawnNucleon()
	{
		Nucleon prefab = nucleonPrefabs[Random.Range(0, nucleonPrefabs.Length)];
		Nucleon spawn = Instantiate<Nucleon>(prefab);
		spawn.transform.localPosition = Random.onUnitSphere * spawnDistance;
	}
}
