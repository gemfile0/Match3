using System.Collections.Generic;

[System.Serializable]
public class GameModel: BaseModel {
    public int rows;
    public int cols;
    public Dictionary<int, GemModel> gemModels;
    public List<MatchLineModel> matchLineModels;

    public GameModel() {
        gemModels = new Dictionary<int, GemModel>();
        matchLineModels = new List<MatchLineModel> {
            new MatchLineModel(3, 1),
            new MatchLineModel(1, 3),
            new MatchLineModel(2, 2)
        };
    }
}
