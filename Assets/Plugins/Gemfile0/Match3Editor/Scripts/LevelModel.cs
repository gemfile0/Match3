[System.Serializable]
public struct MissionModel
{
    public int gemID;
    public int howMany;
}

[System.Serializable]
public class LevelModel 
{
    public int cols;
    public int rows;
    public int moves;
    public int[] gems;
    public int[] tiles;
    public int[] gravities;
    public MissionModel[] missions;
    public int[] gemTypesAvailable;
}