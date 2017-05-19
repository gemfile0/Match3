using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class EventAllMissionAchieved: UnityEvent<CompletedRuleInfo> {};

public class CompletedRuleInfo
{
    public RuleModel ruleModel;
    public Action onShowGotoNext;
    public Action onMovesComsumed;
}

public class RuleView: BaseView<RuleModel, RuleController<RuleModel>>  
{
    public EventAllMissionAchieved OnAllMissionAchieved = new EventAllMissionAchieved();
    
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
    CompletedRuleInfo completedRuleInfo;
    
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
        completedRuleInfo = new CompletedRuleInfo {
            onMovesComsumed = () => {
                OnPhaseNext();
            },
            onShowGotoNext = () => {
                firework.Play();
                Invoke("GoToNext", 2);
            }
        };
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        missions = null;
        completedRuleInfo = null;
    }

	public void PassTheLevelModel(LevelModel levelModel)
    {
        Model.levelModel = levelModel;
    }

    public void OnPhaseNext()
    {
        Controller.OnPhaseNext();
        ValidateInfomation();
        if (!Model.hasCompleted && Model.movesLeft == 0) 
        {
            SuggestRetry();
        }
    }

    public void OnGemRemoved(int gemID)
    {
        if (Model.hasCompleted) { return; }

        Controller.OnGemRemoved(gemID);
        ValidateInfomation();
        if (Model.hasCompleted) 
        {
            completedRuleInfo.ruleModel = Model;
            OnAllMissionAchieved.Invoke(completedRuleInfo);
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
