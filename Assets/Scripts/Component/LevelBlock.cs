using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelBlock : MonoBehaviour 
{
	[SerializeField]
	Button button;
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
