﻿using System;
using System.Collections.Generic;
using UnityEngine;

public interface IGameModel 
{
    GemModel[,] GemModels { get; set; }
    List<GemType> MatchingTypes { get; }
    int Rows { get; }
    int Cols { get; }
}

[System.Serializable]
public class GameModel: BaseModel, IGameModel 
{
    public GemModel[,] GemModels 
    { 
        get {
            UnityEngine.Assertions.Assert.IsNotNull(gemModels);
            return gemModels; 
        } 
        set {
            gemModels = value;
        }
    }
    GemModel[,] gemModels;

    public TileModel[,] TileModels 
    { 
        get {
            UnityEngine.Assertions.Assert.IsNotNull(tileModels);
            return tileModels; 
        } 
        set {
            tileModels = value;
        }
    }
    TileModel[,] tileModels;
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

    public GameModel() 
    {
        currentTurn = 0;
    }

    public override void Setup() 
    {
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
        Position.levelModel = levelModel;

        var gems = levelModel.gems;
        var count = 0;
        gemModels = new GemModel[Rows, Cols];
        for (var row = Rows-1 ; row >= 0; row -= 1) 
        {
            for (var col = 0; col < Cols; col += 1) {
                var gemIndex = row * Cols + col;

                var colByCount = count % Cols;
                var rowByCount = count / Cols;
                gemModels[rowByCount, colByCount] = GemModelFactory.Get(
                    (GemType)gems[gemIndex], new Position(colByCount, rowByCount)
                );
                count++;
            }
        }

        var tiles = levelModel.tiles;
        count = 0;
        tileModels = new TileModel[Rows, Cols];
        for (var row = Rows-1 ; row >= 0; row -= 1) 
        {
            for (var col = 0; col < Cols; col += 1) {
                var gemIndex = row * Cols + col;

                var colByCount = count % Cols;
                var rowByCount = count / Cols;
                tileModels[rowByCount, colByCount] = TileModelFactory.Get(
                    (TileType)tiles[gemIndex]
                );
                count++;
            }
        }
    }

    public override void Destroy()
    {
        allwayMatchLineModels.Clear();
        allwayMatchLineModels = null;

        positiveMatchLineModels.Clear();
        positiveMatchLineModels = null;

        matchingTypes.Clear();
        matchingTypes = null;

        levelData = null;
        
    }
}
