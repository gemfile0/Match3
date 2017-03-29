using System;

public interface IGemModel {
    GemType Type { get; set; }
    Position Position { get; set; }
    string Name { get; }
}

[System.Serializable]
public enum GemType {
    Nil = -1, Blocked = 0, Empty = 1, ChocoGem = 4, SuperGem = 5, Spawnee = 7, Spawner = 8, 
    RedGem = 10, BlueGem = 20, GreenGem = 30, PurpleGem = 40, OrangeGem = 50, YellowGem = 60, 
}

[System.Serializable]
public class GemModel: BaseModel {
    static Int64 GEM_ID = 0;
    static Int64 SEQUENCE_ID = 0;
    public GemType Type {
        set { 
            type = value; 
            name = type.ToString();
        }
        get { return type; }
    }
    GemType type;
    public string Name { 
        get { return name + specialKey; }
    }
    string name;
    public Position Position { 
        set { 
            position = value;
            sequence = SEQUENCE_ID++;
        }
        get { return position; }
    }
    Position position;
    public Int64 id;
    public Int64 sequence;
    public string specialKey;
    public int hp;
    
    public GemModel(GemType type, Position position) {
        Type = type;
        Position = position;
        id = GEM_ID++;
    }

    public override string ToString() {
        return string.Format("{0}: {1}", type, position.ToString());
    }
}

public class GemInfo {
    public Position position;
    public Int64 id;
}

static class GemModelFactory {
    public static GemModel Get(GemType gemType, Position position) {
        string gemName = gemType.ToString();

        switch (gemType) {
            case GemType.ChocoGem:
            case GemType.SuperGem:
            case GemType.RedGem:
            case GemType.BlueGem:
            case GemType.GreenGem:
            case GemType.PurpleGem:
            case GemType.OrangeGem:
            case GemType.YellowGem:
            gemName = GemType.Empty.ToString();
            break;

            case GemType.Spawnee:
            gemType = GemType.Empty;
            break;
        }
        
        var cardModel = (GemModel)Activator.CreateInstance(
            Type.GetType(gemName + "GemModel"),
            gemType,
            position
        );
        return cardModel;
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
