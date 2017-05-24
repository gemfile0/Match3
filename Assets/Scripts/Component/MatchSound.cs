using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AudioItem
{
    public string key;
    public AudioClip value;
}

struct KeyInPlaying
{
	public string key;
	public float startTime;
}

public class MatchSound: MonoBehaviour 
{
	public static MatchSound Instance = null;     
	public float lowPitchRange = .95f;              
	public float highPitchRange = 1.05f;            
	[SerializeField] List<AudioItem> audioClipList;
	Dictionary<string, AudioClip> audioClipDict;
	Stack<AudioSource> poolOfAudioSource;
	List<KeyInPlaying> keysInPlaying;
 
	void Awake ()
	{
		Instance = this;
		audioClipDict = new Dictionary<string, AudioClip>();
		foreach (AudioItem audioItem in audioClipList) 
		{
			audioClipDict.Add(audioItem.key, audioItem.value);
    	}
		poolOfAudioSource = new Stack<AudioSource>();

		var countOfPool = 10;
		while (countOfPool > 0)
		{
			poolOfAudioSource.Push(GetNewOne());
			countOfPool -= 1;
		}

		keysInPlaying = new List<KeyInPlaying>();
	}
	
	public void Play(string key, float delay = 0f)
	{
		AudioClip audioClip;
		if (audioClipDict.TryGetValue(key, out audioClip))
		{
			float startTime = Time.time;

			if (!keysInPlaying.Exists(keyInPlaying =>
				startTime == keyInPlaying.startTime && key == keyInPlaying.key
			)) {
				var keyInPlaying = new KeyInPlaying { key=key, startTime=startTime };
				keysInPlaying.Add(keyInPlaying);
				StartCoroutine(StartPlaying(audioClip, delay, keyInPlaying));
			}
		}
	}

	IEnumerator StartPlaying(AudioClip audioClip, float delay, KeyInPlaying keyInPlaying)
	{
		if (delay != 0) { yield return new WaitForSeconds(delay); }
		
		AudioSource audioSource = (poolOfAudioSource.Count > 0) ? poolOfAudioSource.Pop() : GetNewOne();
		audioSource.clip = audioClip;
		audioSource.Play();

		yield return new WaitForSeconds(audioSource.clip.length);
		poolOfAudioSource.Push(audioSource);
		keysInPlaying.RemoveAt(keysInPlaying.FindIndex(finding =>
			finding.startTime == keyInPlaying.startTime && finding.key == keyInPlaying.key
		));
	}

	AudioSource GetNewOne()
	{
		return gameObject.AddComponent<AudioSource>();
	}
}