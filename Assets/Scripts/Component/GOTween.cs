using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IUpdater
{	
	void Update();
}

public interface IWatcher
{
	void Lerp(float current);
	void Kill();
}

public class GORoot: MonoBehaviour
{
	List<IUpdater> updaters = new List<IUpdater>();
	public void AddUpdater(IUpdater updater)
	{
		updaters.Add(updater);
	}

	public void RemoveUpdater(IUpdater updater)
	{
		updaters.Remove(updater);
	}
	
	void Update()
	{
		for (var i = 0; i < updaters.Count; i++)
		{
			updaters[i].Update();
		}
	}
}

class SequenceItem
{
	public int id;
	public float atTime;
	public float duration;
	public IWatcher watcher;
}

public class GOEase
{
	public static Func<float, float> SmootherStep = (t) => t*t*t * (t * (6f*t - 15f) + 10f);
	public static Func<float, float> SmoothStep = (t) => t*t * (3f - 2f*t);
	public static Func<float, float> EaseOut = (t) => Mathf.Sin(t * Mathf.PI * 0.5f);
	public static Func<float, float> EaseIn = (t) => 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
	public static Func<float, float> Linear = (t) => t;
	public static Func<float, float, float, float, float> EaseOutQuad = 
		(t, b, c, d) => {
			t /= d;
			return -c * t*(t-2) + b;
		};
}

public class GOSequence: IUpdater
{
	GORoot root;
	Dictionary<int, SequenceItem> items;
	private float startTime;
	private int id;
	Func<float, float> Ease;
	public bool IsComplete {
		get { return hasCompleted; }
	}
	bool hasCompleted;

	public float Duration
	{
		get {
			var result = 0f;
			foreach(var item in items.Values)
			{
				var current = item.atTime + item.duration;
				result = Mathf.Max(result, current);
			}

			return result;
		}
	}

	public GOSequence(GORoot root)
	{
		this.root = root;
		this.items = new Dictionary<int, SequenceItem>();
		this.startTime = Time.time;
		this.id = 0;
		this.hasCompleted = false;
		this.Ease = GOEase.Linear;
	}
	
	public void Kill()
	{
		foreach(var item in items.Values)
		{
			item.watcher.Kill();
		}
		items = null;
		Ease = null;
		root.RemoveUpdater(this);
	}

	public void InsertCallback(float atTime, Action callback)
	{
		var id = GetID();
		items.Add(id, new SequenceItem {
			id = id,
			atTime = atTime, 
			watcher = new GOTweenCallback { 
				callback = () => {
					callback();
				}
			}
		});
	}

	public void Insert(float atTime, GOTween gTween)
	{
		var id = GetID();
		items.Add(id, new SequenceItem {
			id = id,
			atTime = atTime,
			duration = gTween.Duration,
			watcher = gTween
		});
	}

	public void Update()
	{
		var duration = Duration;
		if (duration == 0) { return; } 

		var currentTime = (Time.time - startTime) / duration;
		// Debug.Log(Time.time + ", " + startTime + ", " + duration + ", " + currentTime);
		if (currentTime > 1) 
		{
			currentTime = 1;
			hasCompleted = true;
		}

		currentTime = Ease(currentTime) * duration;
		for(var i = 0; i < items.Count; i++)
		{
			var item = items[i];
			if (currentTime >= item.atTime) {
				item.watcher.Lerp(currentTime - item.atTime);
			}
		}

		if (hasCompleted) { Kill(); }
	}

	public GOSequence SetEase(Func<float, float> gEase)
	{
		Ease = gEase;
		return this;
	}

	private int GetID()
	{
		var issued = id;
		id += 1;
		return issued;
	}
}

public class GOTweenCallback: IWatcher
{
	public Action callback;
	private bool hadCalled;

	public GOTweenCallback()
	{
		hadCalled = false;
	}

	public void Kill()
	{
		callback = null;
	}

	public void Lerp(float current)
	{
		if (hadCalled) { return; }

		if (callback != null) { callback(); }
		hadCalled = true;
	}
}

public class GOTween: MonoBehaviour, IWatcher
{
	public float Duration 
	{
		get { return duration; }
	}

	static GORoot root;
	bool hasCompleted;
	bool hasStarted;
	Action UpdateEndValue;
	Action<bool, float> UpdateCurrentValue;
	float duration;
	List<Action> OnCompletes;
	float previous;
	Func<float, float> Ease;
	
	static GOTween()
	{
		root = new GameObject("[GOTween]").AddComponent<GORoot>();
		DontDestroyOnLoad(root);
	}

	void Awake()
	{
		hasCompleted = false;
		OnCompletes = new List<Action>();
		previous = 0f;
		Ease = GOEase.Linear;
	}

	public static GOSequence Sequence()
	{
		var sequence = new GOSequence(root);
		root.AddUpdater(sequence);
		return sequence;
	}

	public static GOTween To<T>(Func<T> getter, Action<T> setter, T endValue, float duration)
	{
		var goTween = root.gameObject.AddComponent<GOTween>();
		Type type = getter().GetType();
		if (type == typeof(Vector3))
		{
			Vector3 endValueConverted = (Vector3)Convert.ChangeType(endValue, typeof(Vector3));
			Vector3 startValue = Vector3.zero;
			goTween.UpdateCurrentValue = (bool initializing, float current) => {
				if (initializing) {
					startValue = (Vector3)Convert.ChangeType(getter(), typeof(Vector3));
				}

				Vector3 offset = endValueConverted - startValue;
				Vector3 currentValue = startValue + offset * current / duration;
				setter((T)Convert.ChangeType(currentValue, typeof(T)));
			};
		}
		goTween.UpdateEndValue = () => {
			setter(endValue);
			Destroy(goTween);
		};
		goTween.duration = duration;
		return goTween;
	}

	public void Kill()
	{
		UpdateEndValue = null;
		UpdateCurrentValue = null;
		OnCompletes = null;
		Ease = null;
		Destroy(this);
	}

	public void Lerp(float current)
	{
		if (hasCompleted) { return; }
		if (!hasStarted) 
		{
			hasStarted = true;
			UpdateCurrentValue(true, current);
			return;
		}

		if (current < previous) { current = previous; }
		if (current > duration) { current = duration; hasCompleted = true; }
		previous = current;

		current = Ease(current / duration) * duration;
		UpdateCurrentValue(false, current);
		if (hasCompleted)
		{
			UpdateEndValue();
			foreach(var onComplete in OnCompletes) {
				onComplete();
			}
		}
	}

	public void OnComplete(Action callback)
	{
		OnCompletes.Add(callback);
	}

	public GOTween SetEase(Func<float, float> gEase)
	{
		Ease = gEase;
		return this;
	}
}
