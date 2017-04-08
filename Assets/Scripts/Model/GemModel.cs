using System;
using System.Collections.Generic;

public interface IGemModel 
{
    GemType Type { get; set; }
    Position Position { get; set; }
    string Name { get; }
    bool IsFalling { set; }
}

[System.Serializable]
public enum GemType 
{
    Nil = -1, BlockedGem = 0, EmptyGem = 1, ChocoGem = 4, SuperGem = 5, SpawneeGem = 7, SpawnerGem = 8, 
    RedGem = 10,    RedGemC = 11,   RedGemH = 12,   RedGemV = 13,
    BlueGem = 20,   BlueGemC= 21,   BlueGemH= 22,   BlueGemV= 23,
    GreenGem = 30,  GreenGemC=31,   GreenGemH=32,   GreenGemV=33,
    PurpleGem = 40, PurpleGemC=41,  PurpleGemH=42,  PurpleGemV=43,
    OrangeGem = 50, OrangeGemC=51,  OrangeGemH=52,  OrangeGemV=53,
    YellowGem = 60, YellowGemC=61,  YellowGemH=62,  YellowGemV=63
}

[System.Serializable]
public class GemModel: BaseModel 
{
    static Int64 GEM_ID = 0;
    static Int64 SEQUENCE_ID = 0;
    public GemType Type 
    {
        set { 
            type = value; 
            name = type.ToString();
        }
        get { return type; }
    }
    GemType type;
    public string Name 
    { 
        get { return name + specialKey; }
    }
    string name;
    public Position Position 
    { 
        set { 
            position = value;
            sequence = SEQUENCE_ID++;
        }
        get { return position; }
    }
    [UnityEngine.SerializeField]
    Position position;
    public Int64 id;
    public Int64 markedBy;
    public Int64 sequence;
    public string specialKey;
    public int endurance;
    public bool IsFalling 
    {
        set { 
            if (isFalling != value) {
                callbacksOnFalling.ForEach(callback => {
                    callback(value);
                });
            }
            isFalling = value; 
        }
    }
    bool isFalling;
    List<Action<bool>> callbacksOnFalling;

    public GemModel(GemType type, Position position) 
    {
        Type = type;
        Position = position;
        id = GEM_ID++;
        callbacksOnFalling = new List<Action<bool>>();
    }

    public override string ToString() 
    {
        return string.Format("{0}: {1}: {2}", id, type, position.ToString());
    }

    public void SubscribeFalling(Action<bool> callback)
    {
        callbacksOnFalling.Add(callback);
    }
}

public class GemInfo {
    public Position position;
    public Int64 id;
}

static class GemModelFactory {
    public static GemModel Get(GemType gemType, Position position) {
        string className = gemType.ToString();
        var specialKey = "";

        int rawGemType = (int)gemType;
        if (rawGemType >= 10) {
            var baseGemType = (GemType)(rawGemType - (rawGemType % 10));
            specialKey = gemType.ToString().Replace(baseGemType.ToString(), "");
            gemType = baseGemType;
        }

        switch (gemType) {
            case GemType.SuperGem:
            case GemType.ChocoGem:
            case GemType.RedGem:
            case GemType.BlueGem:
            case GemType.GreenGem:
            case GemType.PurpleGem:
            case GemType.OrangeGem:
            case GemType.YellowGem:
            className = GemType.EmptyGem.ToString();
            break;

            case GemType.SpawneeGem:
            gemType = GemType.EmptyGem;
            break;
        }
        
        var gemModel = (GemModel)Activator.CreateInstance(
            Type.GetType(className + "Model"),
            gemType,
            position
        );
        gemModel.specialKey = specialKey;
        SetEndurance(gemModel);

        return gemModel;
    }

    static void SetEndurance(GemModel gemModel) {
        int endurance = 0;
        switch(gemModel.Type) {
            case GemType.ChocoGem:
            endurance = 4;
            break;
        }
        
        switch(gemModel.specialKey) {
            case "H":
            case "V":
            endurance = int.MaxValue;
            break;
        }
        
        gemModel.endurance = endurance;
    }
}

public interface IBlockable {}
public interface IMovable {}
public class BlockedGemModel: GemModel, IBlockable {
    public BlockedGemModel(GemType type, Position position): base(type, position) {}
}

public class SpawnerGemModel: GemModel, IBlockable {
    public int[] spawnTo = new int[]{0, -1};
    public SpawnerGemModel(GemType type, Position position): base(type, position) {}
}

public class SpawneeGemModel: GemModel {
    public SpawneeGemModel(GemType type, Position position): base(type, position) {}
}

public class EmptyGemModel: GemModel, IMovable {
    public EmptyGemModel(GemType type, Position position): base(type, position) {}
}