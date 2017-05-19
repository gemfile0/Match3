using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RuleModel: BaseModel  
{
    public LevelModel levelModel;
    public int movesLeft;
    public List<MissionModel> missionsLeft;

    public RuleModel()
    {

    }

    public override void Setup()
    {
        movesLeft = levelModel.moves;
        missionsLeft = new List<MissionModel>(levelModel.missions);
    }

    public override void Kill()
    {
        levelModel = null;
        missionsLeft = null;
    }
}
