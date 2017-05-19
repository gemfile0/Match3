using System.Text;
using UnityEngine;

public class LevelScene: BaseScene 
{
	[SerializeField]
	GameView gameView;

	[SerializeField]
	RuleView ruleView;

	[SerializeField]
	ModalPanel modalPanel;

	protected override void Awake()
	{
		base.Awake();

		StringBuilder sb = new StringBuilder();
		sb.AppendFormat(Literals.level_0, ReadLevelIndex());

		TextAsset levelData = Resources.Load(sb.ToString()) as TextAsset;
		var levelModel = JsonUtility.FromJson<LevelModel>(levelData.text);

		gameView.PassTheLevelModel(levelModel);
		ruleView.PassTheLevelModel(levelModel);

		gameView.OnGemRemoved.AddListener(ruleView.OnGemRemoved);
		gameView.OnPhaseNext.AddListener(ruleView.OnPhaseNext);
		modalPanel.OnVisbileChanged.AddListener(gameView.OnModalVisibleChanged);
	}

	void OnDestroy()
	{
		gameView.OnGemRemoved.RemoveAllListeners();
		gameView.OnPhaseNext.RemoveAllListeners();
		modalPanel.OnVisbileChanged.RemoveAllListeners();
	}

	public void LoadLobbyScene()
	{
		var sceneLoader = GameObject.Find(Literals.SceneLoader).GetComponent<SceneLoader>();
		sceneLoader.Load(Literals.LobbyScene);
	}

	protected virtual int ReadLevelIndex()
	{
		return PlayerPrefs.GetInt(Literals.LatestLevel);
	}
}
