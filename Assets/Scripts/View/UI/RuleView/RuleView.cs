using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuleView: BaseView<RuleModel, RuleController<RuleModel>>  
{
	public Moves moves;
	public List<Mission> missions;
    
    public override void Start()
	{
		base.Start();

        Controller.ShowMissions();
        ValidateInfomation();
    }

	public void PassTheLevelModel(LevelModel levelModel)
    {
        Model.levelModel = levelModel;
    }

    public void OnPhaseNext()
    {
        Controller.OnPhaseNext();
        ValidateInfomation();
        if (Model.leftMoves == 0) {
            // ShowPopup();
        }
    }

    public void OnGemRemoved(int gemID)
    {
        Controller.OnGemRemoved(gemID);
        ValidateInfomation();
        if (IsAllMissionAchieved()) {
            // ShowPopup();   
        }
    }

    bool IsAllMissionAchieved()
    {
        var result = true;
        var missionLefts = Model.leftMissions;
        foreach (var missionLeft in missionLefts)
        {
            if (missionLeft.howMany > 0) { result = false; }
        }
        return result;
    }

    void ValidateInfomation()
    {
        moves.SetMoves(Model.leftMoves);

        var missionLefts = Model.leftMissions;
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
