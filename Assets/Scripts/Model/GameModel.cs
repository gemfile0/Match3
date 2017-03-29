using System.Collections.Generic;
using UnityEngine;

public interface IGameModel {
    GemModel[,] GemModels { get; set; }
    List<GemType> MatchingTypes { get; }
    int Rows { get; }
    int Cols { get; }
}

[System.Serializable]
public class GameModel: BaseModel, IGameModel {
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
    public List<MatchLineModel> allwayMatchLineModels;
    public List<MatchLineModel> positiveMatchLineModels;
    public List<GemType> MatchingTypes {
        get { return matchingTypes; }   
    }
    List<GemType> matchingTypes;

    public int Rows {
        get { 
            UnityEngine.Assertions.Assert.IsNotNull(levelModel);
            return levelModel.rows; 
        }
    }
    public int Cols {
        get { 
            UnityEngine.Assertions.Assert.IsNotNull(levelModel);
            return levelModel.cols; 
        }
    }
    public TextAsset levelData;
    public LevelModel levelModel;

    public GameModel() {
        
    }

    public override void Setup() {
        allwayMatchLineModels = new List<MatchLineModel> {
            new MatchLineModel(-2, 0, 3, 1),
            new MatchLineModel(0, -2, 1, 3),
            new MatchLineModel(-1, -1, 2, 2)
        };

        positiveMatchLineModels = new List<MatchLineModel> {
            new MatchLineModel(0, 0, 5, 1),
            new MatchLineModel(0, 0, 1, 5),
            new MatchLineModel(0, 0, 4, 1),
            new MatchLineModel(0, 0, 1, 4),
            new MatchLineModel(0, 0, 2, 2),
            new MatchLineModel(0, 0, 3, 1),
            new MatchLineModel(0, 0, 1, 3),
        };

        matchingTypes = new List<GemType> {
			GemType.RedGem, GemType.GreenGem, GemType.BlueGem, 
			GemType.OrangeGem, GemType.PurpleGem, GemType.YellowGem
		};
        
        levelModel = JsonUtility.FromJson<LevelModel>(levelData.text);
        var tiles = levelModel.tiles;

        Position.Cols = Cols;
        Position.Rows = Rows;

        var count = 0;
        gemModels = new GemModel[Rows, Cols];
        for(var row = Rows-1 ; row >= 0; row -= 1) {
            for(var col = 0; col < Cols; col += 1) {
                var tileIndex = row * Cols + col;

                var colByCount = count % Cols;
                var rowByCount = count / Cols;
                gemModels[rowByCount, colByCount] = GemModelFactory.Get(
                    (GemType)tiles[tileIndex], new Position(colByCount, rowByCount)
                );
                count++;
            }
        }

    }
}
