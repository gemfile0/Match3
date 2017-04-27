using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelItem: MonoBehaviour 
{
	[SerializeField]
	Button button;
	public Text title;
	public Action callback;
	
	void Start()
	{
		button.onClick.AddListener(() => {
			if (callback != null) { callback(); }
		});
	}

	void Destroy()
	{
		button.onClick.RemoveAllListeners();
		callback = null;
	}
}
