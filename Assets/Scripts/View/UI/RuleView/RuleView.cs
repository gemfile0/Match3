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
    
    [SerializeField] Moves movesView;
    [SerializeField] List<Mission> missionViews;
    [SerializeField] ParticleSystem firework;

    int currentLevel;
    string levelText;
    SceneLoader sceneLoader;
    const int END_OF_LEVEL = 8;
    CompletedRuleInfo completedRuleInfo;
    Dictionary<int, Color32> colorByGemType;
    
    public override void Start()
	{
		base.Start();

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
                MatchSound.Instance.Play("Win");
                MatchSound.Instance.Play("Firework", .59f);
                Invoke("GoToNext", 2f);
            }
        };

        colorByGemType = new Dictionary<int, Color32>() {
            { 10, new Color32(191,   0,   9, 255) },
            { 20, new Color32(  0,  71, 172, 255) },
            { 30, new Color32( 61, 171,   0, 255) },
            { 40, new Color32(168,  49, 185, 255) },
            { 50, new Color32(249, 160,  16, 255) },
            { 60, new Color32(243, 217,  16, 255) },
        };
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        missionViews = null;
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

    public void OnGemRemoved(int gemType, Vector3 sourcePosition, Transform parent)
    {
        if (Model.hasCompleted) { return; }

        var indexOfGemRemoved = Controller.OnGemRemoved(gemType);
        var durationOfEffect = 0f;
        
        if (indexOfGemRemoved != -1) 
        {
            var particleAttractor = ResourceCache.Instantiate<ParticleAttractor>(Literals.ParticleAttractor, parent);
            particleAttractor.SetPositions(
                sourcePosition, 
                missionViews[indexOfGemRemoved].Icon.transform.position
            );
            particleAttractor.SetColor(colorByGemType[gemType]);
            particleAttractor.Play();
            particleAttractor.OnComplete(() => {
                ValidateInfomation();
            });
            durationOfEffect = particleAttractor.Duration;
        }
        
        if (Model.hasCompleted) 
        {
            completedRuleInfo.ruleModel = Model;
            OnAllMissionAchieved.Invoke(completedRuleInfo);
        }
    }

    public void SuggestRetry()
    {
        ModalPanel.Show(
            levelText, 
            Literals.Retry,
            () => {
                sceneLoader.Load(Literals.LevelScene);
                MatchSound.Instance.Play("Accept");
            },
            () => {
                MatchSound.Instance.Play("Back");
            }
        );
    }

    void GoToNext()
    {
        ModalPanel.Show(
            levelText,
            Literals.Next,
            () => {
                var nextLevel = currentLevel + 1;
                if (nextLevel > END_OF_LEVEL) { nextLevel = 1; }
                PlayerPrefs.SetInt(Literals.LatestLevel, nextLevel);
                sceneLoader.Load(Literals.LevelScene);
                MatchSound.Instance.Play("Accept");
            },
            () => {
                MatchSound.Instance.Play("Back");
            }
        );
    }

    void ValidateInfomation()
    {
        movesView.SetMoves(Model.movesLeft);

        var missionLefts = Model.missionsLeft;
        for (var i = 0; i < missionViews.Count; i++)
        {
            var missionView = missionViews[i];

            if (i < missionLefts.Count) {
                missionView.SetMission(missionLefts[i]);
            } else {
                missionView.Hide();
            }
        }
    }
}
