using UnityEngine;

public class LobbyScene: BaseScene 
{
	[SerializeField]
	LevelSelectionPanel levelSelectionPanel;
	
#if DIABLE_LOG
	protected override void Awake()
	{
		base.Awake();

		if (Application.platform == RuntimePlatform.Android
			|| Application.platform == RuntimePlatform.IPhonePlayer)
		{
			Debug.logger.logEnabled=false;
		}
	}
#endif

	void Start() 
	{
		LetLevelItemToLoad();
	}
	
	void LetLevelItemToLoad()
	{
		var levelIndex = 1;
		var sceneLoader = GameObject.Find("SceneLoader").GetComponent<SceneLoader>();
		levelSelectionPanel.Setup();
		while (true)
		{
			var currentLevel = levelIndex;
			var levelItem = levelSelectionPanel.GetLevelItem(levelIndex);
			if (levelItem == null) { break; }

			levelItem.callback = () => {
				PlayerPrefs.SetInt("LatestLevel", currentLevel);
				sceneLoader.Load("LevelScene");
			};

			levelIndex++;
		}
	}
}
