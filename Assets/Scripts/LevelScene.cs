using UnityEngine;

public class LevelScene: BaseScene 
{
	[SerializeField]
	GameView gameView;

	protected override void Awake()
	{
		base.Awake();

		var levelIndex = PlayerPrefs.GetInt("LatestLevel");
		TextAsset levelData = Resources.Load("level_" + levelIndex) as TextAsset;
		gameView.PassTheLevelData(levelData);
	}

	public void LoadLobbyScene()
	{
		var sceneLoader = GameObject.Find("SceneLoader").GetComponent<SceneLoader>();
		sceneLoader.Load("LobbyScene");
	}
}
