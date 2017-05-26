using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleAttractor: PooledObject 
{
	public float Duration {
		get { return rootPS.main.duration; }
	}
	Action onComplete;
	ParticleSystem rootPS;
	ParticleSystem.Particle[] m_Particles;
	public Transform target;
	public float speed = 5f;
	int numParticlesAlive;
	Color32 color;
	float currentTime;
	
	void Awake () 
	{
		rootPS = GetComponent<ParticleSystem>();
		if (!GetComponent<Transform>())
		{
			GetComponent<Transform>();
		}
		m_Particles = new ParticleSystem.Particle[rootPS.main.maxParticles];
	}

	void OnDestroy()
	{
		m_Particles = null;
		onComplete = null;
	}

    public void SetPositions(Vector3 sourcePosition, Vector3 targetPosition)
    {
		transform.position = new Vector3(sourcePosition.x, sourcePosition.y, sourcePosition.z - 1);
		target.position = new Vector3(targetPosition.x, targetPosition.y, sourcePosition.z - 1);
    }

	public void SetColor(Color color)
	{
		var main = GetComponent<ParticleSystem>().main;
		main.startColor = color;
	}

	public void OnComplete(Action callback) 
	{
		this.onComplete = callback;
	}

	public void Play()
	{
		StartCoroutine(StartPlay());
	}

	IEnumerator StartPlay()
	{
		rootPS.Play();

		var totalDuration = 0f;
		foreach (var ps in GetComponentsInChildren<ParticleSystem>())
		{
			totalDuration += ps.main.duration;
		}

		var currentTime = 0f;
		while (true)
		{
			float progressOfRoot = currentTime / rootPS.main.duration;
			float progressOfAll = currentTime / totalDuration;

			numParticlesAlive = rootPS.GetParticles(m_Particles);
			for (int i = 0; i < numParticlesAlive; i++)
			{
				m_Particles[i].position = Vector3.LerpUnclamped(
					m_Particles[i].position, target.position, progressOfRoot
				);
			}
			rootPS.SetParticles(m_Particles, numParticlesAlive);

			if (progressOfRoot >= 1.0 && onComplete != null) {
				onComplete();
				onComplete = null;
			}

			if (progressOfAll >= 1.0) {
				ReturnToPool();
				yield break;
			}
			
			currentTime += Time.deltaTime;
			yield return null;
		}
	}
}
