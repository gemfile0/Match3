using System.Collections.Generic;

public interface IGameModel {
    GemModel[,] GemModels { get; set; }
    List<GemType> MatchingTypes { get; }
    List<GemModel> EmtpyGemModels { get; set; }
}

[System.Serializable]
public class GameModel: BaseModel, IGameModel {
    public int rows;
    public int cols;
    public GemModel[,] GemModels { 
        get {
            UnityEngine.Assertions.Assert.IsNotNull(gemModels);
            return gemModels; 
        } 
        set {
            gemModels = value;
        }
    }
    GemModel[,] gemModels;
    public List<MatchLineModel> matchLineModels;
    public List<GemModel> EmtpyGemModels { 
        get { return emtpyGemModels; }
        set { emtpyGemModels = value; }
    }
    List<GemModel> emtpyGemModels;
    public List<GemType> MatchingTypes {
        get { return matchingTypes; }   
    }
    List<GemType> matchingTypes;

    public GameModel() {
        matchLineModels = new List<MatchLineModel> {
            new MatchLineModel(3, 1),
            new MatchLineModel(1, 3),
            new MatchLineModel(2, 2)
        };

        matchingTypes = new List<GemType> {
			GemType.RedGem, GemType.GreenGem, GemType.BlueGem, 
			GemType.OrangeGem, GemType.PurpleGem, GemType.YellowGem
		};
    }
}
