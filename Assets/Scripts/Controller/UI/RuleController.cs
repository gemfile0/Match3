using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuleController<M> : BaseController<M>
    where M: RuleModel
{
    internal void ShowMissions()
    {
		// foreach (var mission in Model.levelModel.missions)
		// {
        // 	Debug.Log("Mission : " + mission.gemID + ", " + mission.howMany);
		// }
    }

    public void OnPhaseNext()
    {
        if (Model.movesLeft > 0) { Model.movesLeft -= 1; }
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

    public void OnGemRemoved(int gemID)
    {
        var missionsLeft = Model.missionsLeft;
        for (var i = 0; i < missionsLeft.Count; i++)
        {
            var missionLeft = missionsLeft[i];
            if (missionLeft.gemID == gemID) { 
                if (missionLeft.howMany > 0) { missionLeft.howMany -= 1; }
                missionsLeft[i] = missionLeft;
            }
        }

        Model.missionsLeft = missionsLeft;
        Model.hasCompleted = IsAllMissionAchieved();
    }
}
