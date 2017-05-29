using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameModel: BaseModel 
{
    public GemModel[,] GemModels 
    { 
        get {
            UnityEngine.Assertions.Assert.IsNotNull(gemModels);
            return gemModels; 
        } 
    }
    GemModel[,] gemModels;
    public List<GemModel[,]> HistoryOfGemModels 
    { 
        get {
            UnityEngine.Assertions.Assert.IsNotNull(historyOfGemModels);
            return historyOfGemModels; 
        } 
    }
    List<GemModel[,]> historyOfGemModels;
    public TileModel[,] TileModels 
    { 
        get {
            UnityEngine.Assertions.Assert.IsNotNull(tileModels);
            return tileModels; 
        } 
    }
    TileModel[,] tileModels;
    public GravityModel[,] GravityModels 
    { 
        get {
            UnityEngine.Assertions.Assert.IsNotNull(gravityModels);
            return gravityModels; 
        } 
    }
    GravityModel[,] gravityModels;
    public List<MatchLineModel> allwayMatchLineModels;
    public List<MatchLineModel> positiveMatchLineModels;
    public List<GemType> MatchingTypes 
    {
        get { return matchingTypes; }   
    }
    List<GemType> matchingTypes;
    public Int64 currentTurn;

    public int Rows 
    {
        get { 
            UnityEngine.Assertions.Assert.IsNotNull(levelModel);
            return levelModel.rows; 
        }
    }
    public int Cols 
    {
        get { 
            UnityEngine.Assertions.Assert.IsNotNull(levelModel);
            return levelModel.cols; 
        }
    }
    public TextAsset levelData;
    public LevelModel levelModel;
    public List<int[]> offsetsCanSwap;

    public GameModel() 
    {
        currentTurn = 0;
    }

    public override void Setup() 
    {
        allwayMatchLineModels = new List<MatchLineModel> {
            new MatchLineModel(-2, 0, 3, 1),
            new MatchLineModel( 0,-2, 1, 3),
            new MatchLineModel(-1,-1, 2, 2),
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

        offsetsCanSwap = new List<int[]> {
			new int[] { 0,  1},
			new int[] { 0, -1},
			new int[] { 1,  0},
			new int[] {-1,  0}
		};
        
        Position.Setup(levelModel);

        matchingTypes = new List<GemType>();
        foreach (var gemTypeAvailable in levelModel.gemTypesAvailable)
        {
            matchingTypes.Add((GemType)gemTypeAvailable);
        }

        var gems = levelModel.gems;
        var tiles = levelModel.tiles;
        var gravities = levelModel.gravities;
        var count = 0;

        historyOfGemModels = new List<GemModel[,]>();
        
        gemModels = new GemModel[Rows, Cols];
        tileModels = new TileModel[Rows, Cols];
        gravityModels = new GravityModel[Rows, Cols];
        for (var row = Rows-1 ; row >= 0; row -= 1) 
        {
            for (var col = 0; col < Cols; col += 1) {
                var gemIndex = row * Cols + col;

                var colByCount = count % Cols;
                var rowByCount = count / Cols;
                var position = Position.Get(colByCount, rowByCount);
                gemModels[rowByCount, colByCount] = GemModelFactory.Get(
                    (GemType)gems[gemIndex], position
                );
                tileModels[rowByCount, colByCount] = TileModelFactory.Get(
                    (TileType)tiles[gemIndex], position
                );
                gravityModels[rowByCount, colByCount] = GravityModelFactory.Get(
                    (GravityType)gravities[gemIndex], position
                );
                count++;
            }
        }
    }

    public override void Kill()
    {
        allwayMatchLineModels = null;
        positiveMatchLineModels = null;
        matchingTypes = null;
        levelModel = null;
        levelData = null;
        gemModels = null;
        tileModels = null;
        gravityModels = null;
        historyOfGemModels = null;
    }
}
