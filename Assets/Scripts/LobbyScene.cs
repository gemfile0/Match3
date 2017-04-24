using UnityEngine;

public class LobbyScene: BaseScene 
{
	[SerializeField]
	SceneLoader sceneLoader;

	[SerializeField]
	Transform blockContainer;
	
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
		while (true)
		{
			var currentLevel = levelIndex;
			var levelBlock = blockContainer.Find("LevelBlock" + currentLevel);
			if (levelBlock == null) { break; }

			levelBlock.GetComponent<LevelBlock>().callback = () => {
				// currentLevel
				PlayerPrefs.SetInt("LatestLevel", currentLevel);
				sceneLoader.Load("LevelScene");
			};

			levelIndex++;
		}
	}
}
