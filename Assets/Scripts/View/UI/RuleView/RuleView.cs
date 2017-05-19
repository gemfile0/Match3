using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class RuleView: BaseView<RuleModel, RuleController<RuleModel>>  
{
    [SerializeField]
	Moves moves;
    [SerializeField]
	List<Mission> missions;
    [SerializeField]
    ModalPanel modalPanel;
    [SerializeField]
    ParticleSystem firework;
    int currentLevel;
    string levelText;
    SceneLoader sceneLoader;
    const int END_OF_LEVEL = 8;
    
    public override void Start()
	{
		base.Start();

        Controller.ShowMissions();
        ValidateInfomation();
        currentLevel = PlayerPrefs.GetInt(Literals.LatestLevel);

        var sb = new StringBuilder();
		sb.AppendFormat(Literals.Level0, currentLevel);
		levelText = sb.ToString();

        sceneLoader = GameObject.Find(Literals.SceneLoader).GetComponent<SceneLoader>();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        missions = null;
    }

	public void PassTheLevelModel(LevelModel levelModel)
    {
        Model.levelModel = levelModel;
    }

    public void OnPhaseNext()
    {
        Controller.OnPhaseNext();
        ValidateInfomation();
        if (Model.movesLeft == 0) 
        {
            SuggestRetry();
        }
    }

    public void OnGemRemoved(int gemID)
    {
        Controller.OnGemRemoved(gemID);
        ValidateInfomation();
        if (IsAllMissionAchieved()) 
        {
            firework.Play();
            Invoke("GoToNext", 2);
        }
    }

    void SuggestRetry()
    {
        modalPanel.Choice(
            levelText, 
            Literals.Retry,
            () => {
                sceneLoader.Load(Literals.LevelScene);
            },
            () => {
                sceneLoader.Load(Literals.LobbyScene);
            }
        );
    }

    void GoToNext()
    {
        modalPanel.Choice(
            levelText,
            Literals.Next,
            () => {
                var nextLevel = currentLevel + 1;
                if (nextLevel > END_OF_LEVEL) { nextLevel = 1; }
                PlayerPrefs.SetInt(Literals.LatestLevel, nextLevel);
                sceneLoader.Load(Literals.LevelScene);
            },
            () => {
                sceneLoader.Load(Literals.LobbyScene);
            }
        );
    }

    bool IsAllMissionAchieved()
    {
        var result = true;
        var missionLefts = Model.missionsLeft;
        foreach (var missionLeft in missionLefts)
        {
            if (missionLeft.howMany > 0) { result = false; }
        }
        return result;
    }

    void ValidateInfomation()
    {
        moves.SetMoves(Model.movesLeft);

        var missionLefts = Model.missionsLeft;
        for (var i = 0; i < missions.Count; i++)
        {
            var missionView = missions[i];

            if (i < missionLefts.Count) {
                missionView.SetMission(missionLefts[i]);
            } else {
                missionView.Hide();
            }
        }
    }
}
