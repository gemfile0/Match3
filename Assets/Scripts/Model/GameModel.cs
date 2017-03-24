﻿using System.Collections.Generic;
using UnityEngine;

public interface IGameModel {
    GemModel[,] GemModels { get; set; }
    List<GemType> MatchingTypes { get; }
    List<GemModel> EmtpyGemModels { get; set; }
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
    public LevelModel levelModel;
    public TileModel[,] tileModels;

    public GameModel() {
        
    }

    public override void Setup() {
        matchLineModels = new List<MatchLineModel> {
            new MatchLineModel(3, 1),
            new MatchLineModel(1, 3),
            new MatchLineModel(2, 2)
        };

        matchingTypes = new List<GemType> {
			GemType.RedGem, GemType.GreenGem, GemType.BlueGem, 
			GemType.OrangeGem, GemType.PurpleGem, GemType.YellowGem
		};
        
        var textAsset = Resources.Load<TextAsset>("level_1");
        levelModel = JsonUtility.FromJson<LevelModel>(textAsset.text);
        var tiles = levelModel.tiles;

        tileModels = new TileModel[Rows, Cols];
        for(var row = Rows-1 ; row >= 0; row -= 1) {
            for(var col = 0; col < Cols; col += 1) {
                Debug.Log(row + ", " + col);
                var index = row + col;
                tileModels[row, col] = new TileModel(tiles[index]);
            }
        }
    }
}
