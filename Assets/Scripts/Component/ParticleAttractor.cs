using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleAttractor: PooledObject 
{
	public float Duration {
		get { return ps.main.duration; }
	}
	Action onComplete;
	ParticleSystem ps;
	ParticleSystem.Particle[] m_Particles;
	public Transform target;
	public float speed = 5f;
	int numParticlesAlive;
	Color32 color;
	float currentTime;
	
	void Awake () 
	{
		ps = GetComponent<ParticleSystem>();
		if (!GetComponent<Transform>())
		{
			GetComponent<Transform>();
		}
		m_Particles = new ParticleSystem.Particle[ps.main.maxParticles];
	}

	void OnDestroy()
	{
		m_Particles = null;
		onComplete = null;
	}

    public void SetPositions(Vector3 sourcePosition, Vector3 targetPosition)
    {
		transform.position = new Vector3(sourcePosition.x, sourcePosition.y, sourcePosition.z - 1);
		target.position = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z - 1);
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
		ps.Play();
		
		currentTime = 0;
		while (true)
		{
			float progress = currentTime / ps.main.duration;
			
			numParticlesAlive = ps.GetParticles(m_Particles);
			for (int i = 0; i < numParticlesAlive; i++)
			{
				m_Particles[i].position = Vector3.LerpUnclamped(
					m_Particles[i].position, target.position, progress
				);
			}
			ps.SetParticles(m_Particles, numParticlesAlive);

			if (progress >= 1.4) {
				if (onComplete != null) { onComplete(); }
				ReturnToPool();
				yield break;
			}
			
			currentTime += Time.deltaTime;
			yield return null;
		}
	}
}
