using System.Text;
using UnityEngine;

public class LevelScene: BaseScene 
{
	[SerializeField]
	GameView gameView;

	protected override void Awake()
	{
		base.Awake();

		var levelIndex = PlayerPrefs.GetInt(Literals.LatestLevel);
		StringBuilder sb = new StringBuilder();
		sb.AppendFormat(Literals.level_0, levelIndex);

		TextAsset levelData = Resources.Load(sb.ToString()) as TextAsset;
		gameView.PassTheLevelData(levelData);
	}

	public void LoadLobbyScene()
	{
		var sceneLoader = GameObject.Find(Literals.SceneLoader).GetComponent<SceneLoader>();
		sceneLoader.Load(Literals.LobbyScene);
	}
}
