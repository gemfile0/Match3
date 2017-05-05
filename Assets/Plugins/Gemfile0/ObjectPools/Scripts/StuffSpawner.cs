using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StuffSpawner : MonoBehaviour 
{
	public Stuff[] stuffPrefabs;
	public float velocity;
	float timeSinceLastSpawn;

	public FloatRange timeBetweenSpawns, scale, randomVelocity, angularVelocity;
	float currentSpawnDelay;

	public Material stuffMaterial;
	
	void FixedUpdate()
	{
		timeSinceLastSpawn += Time.deltaTime;
		if (timeSinceLastSpawn >= currentSpawnDelay)
		{
			timeSinceLastSpawn -= currentSpawnDelay;
			currentSpawnDelay = timeBetweenSpawns.RandomInRange;
			SpawnStuff();
		}
	}

	void SpawnStuff()
	{
		Stuff prefab = stuffPrefabs[Random.Range(0, stuffPrefabs.Length)];
		Stuff spawn = prefab.GetPooledInstance<Stuff>();
		spawn.transform.localPosition = transform.position;
		spawn.transform.localScale = Vector3.one * scale.RandomInRange;
		spawn.transform.localRotation = Random.rotation;
		
		spawn.Body.velocity = transform.up * velocity + Random.onUnitSphere * randomVelocity.RandomInRange;
		spawn.Body.angularVelocity = Random.onUnitSphere * angularVelocity.RandomInRange;

		spawn.SetMaterial(stuffMaterial);
	}
	
}
