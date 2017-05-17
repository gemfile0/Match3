using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RuleModel: BaseModel  
{
    public LevelModel levelModel;
    public int leftMoves;
    public List<MissionModel> leftMissions;

    public RuleModel()
    {

    }

    public override void Setup()
    {
        leftMoves = levelModel.moves;
        leftMissions = new List<MissionModel>(levelModel.missions);
    }

    public override void Destroy()
    {

    }
}
