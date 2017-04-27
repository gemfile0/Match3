using UnityEngine;

public class LobbyScene: BaseScene 
{
	[SerializeField]
	Transform levelItemContainer;
	
#if DIABLE_LOG
	void Awake()
	{
		Debug.logger.logEnabled=false;
	}
#endif

	void Start() 
	{
		MakeLevelBlockToLoad();
	}
	
	void MakeLevelBlockToLoad()
	{
		var levelIndex = 1;
		var sceneLoader = GameObject.Find("SceneLoader").GetComponent<SceneLoader>();
		while (true)
		{
			var currentLevel = levelIndex;
			var levelItem = levelItemContainer.Find("LevelItem" + currentLevel);
			if (levelItem == null) { break; }

			levelItem.GetComponent<LevelItem>().callback = () => {
				PlayerPrefs.SetInt("LatestLevel", currentLevel);
				sceneLoader.Load("LevelScene");
			};

			levelIndex++;
		}
	}
}
