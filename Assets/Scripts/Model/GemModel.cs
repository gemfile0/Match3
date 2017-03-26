using System;

public interface IGemModel {
    GemType Type { get; set; }
}

[System.Serializable]
public enum GemType {
    Nothing = -1, Blocked = 0, Empty = 1, Spawner, Spawnee,
    RedGem, BlueGem, GreenGem, PurpleGem, OrangeGem, YellowGem
}

[System.Serializable]
public class GemModel: BaseModel {
    static Int64 GEM_ID = 0;
    public GemType Type { 
        set { 
            type = value; 
            name = type.ToString();
        }
        get { return type; }
    }
    GemType type;
    public string name;
    public Position position;
    public Int64 id;
    
    public GemModel(GemType type, Position position) {
        Type = type;
        this.position = position;
        id = GEM_ID++;
    }
}

public class GemInfo {
    public Position position;
    public Int64 id;
}

static class GemModelFactory {
    public static GemModel Get(TileType tileType, Position position) {
        var gemType = GemType.Nothing;
        switch (tileType) {
			case TileType.Normal:
            case TileType.Spawnee:
				gemType = GemType.Empty;
				break;

			case TileType.Nil:
				gemType = GemType.Blocked;
				break;

			case TileType.Spawner:
				gemType = GemType.Spawner;
				break;
		}
        
        var cardModel = (GemModel)Activator.CreateInstance(
            Type.GetType(tileType + "GemModel"),
            gemType,
            position
        );
        return cardModel;
    }
}

public interface IBlockable {}
public interface IMovable {}
public class NilGemModel: GemModel, IBlockable {
    public NilGemModel(GemType type, Position position): base(type, position) {
        
    }
}

public class NormalGemModel: GemModel, IMovable {
    public NormalGemModel(GemType type, Position position): base(type, position) {
        
    }
}

public class SpawneeGemModel: GemModel {
    public SpawneeGemModel(GemType type, Position position): base(type, position) {
        
    }
}

public class SpawnerGemModel: GemModel, IBlockable {
    public int[] spawnTo = new int[]{0, -1};
    public SpawnerGemModel(GemType type, Position position): base(type, position) {
        
    }
}