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
		foreach(var updater in updaters.ToList())
		{
			updater.Update();
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
	public static Func<float, float> Smoothstep = (t) => t*t * (3f - 2f*t);
}

public class GOSequence: IUpdater
{
	GORoot root;
	Dictionary<int, SequenceItem> items;
	private float startTime;
	private int id;
	Func<float, float> Ease;
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
	}
	
	public void Kill()
	{
		foreach(var item in items.Values)
		{
			item.watcher.Kill();
		}
		items.Clear();
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
					items.Remove(id);
				}
			}
		});
	}

	public void Insert(float atTime, GOTween gTween)
	{
		var id = GetID();
		gTween.OnComplete(() => {
			// items.Remove(id);
		});
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
		foreach(var item in items.Values.ToList())
		{
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
	private static GORoot root;
	private bool hasCompleted;
	private bool hasStarted;
	private Action UpdateEndValue;
	private Action<bool, float> UpdateCurrentValue;
	public float Duration 
	{
		get { return duration; }
	}
	private float duration;
	private List<Action> OnCompletes;
	private float previous;
	
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
		};
		goTween.duration = duration;
		return goTween;
	}

	public void Kill()
	{
		UpdateEndValue = null;
		UpdateCurrentValue = null;
		OnCompletes.Clear();
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
}
