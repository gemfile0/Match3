using System.Collections.Generic;

[System.Serializable]
public class GameModel: BaseModel {
    public int rows;
    public int cols;
    public Dictionary<int, GemModel> gemModels;

    public GameModel() {
        gemModels = new Dictionary<int, GemModel>();
    }
}
