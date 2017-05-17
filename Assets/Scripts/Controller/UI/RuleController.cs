using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuleController<M> : BaseController<M>
    where M: RuleModel
{
    internal void ShowMissions()
    {
		foreach (var mission in Model.levelModel.missions)
		{
        	Debug.Log("Mission : " + mission.gemID + ", " + mission.howMany);
		}
    }

    public void OnPhaseNext()
    {
        if (Model.leftMoves > 0) { Model.leftMoves -= 1; }
    }

    internal void OnGemRemoved(int gemID)
    {
        var leftMissions = Model.leftMissions;
        for (var i = 0; i < leftMissions.Count; i++)
        {
            var leftMission = leftMissions[i];
            if (leftMission.gemID == gemID) { 
                if (leftMission.howMany > 0) { leftMission.howMany -= 1; }
                leftMissions[i] = leftMission;
            }
        }

        Model.leftMissions = leftMissions;
    }
}
