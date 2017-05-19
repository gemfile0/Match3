[System.Serializable]
public struct MissionModel
{
    public int gemID;
    public int howMany;
}

[System.Serializable]
public class LevelModel 
{
    public int[] gems;
    public int[] tiles;
    public int[] gravities;
    public MissionModel[] missions;
    public int[] availableGemTypes;
    public int moves;
    public int cols;
    public int rows;
}