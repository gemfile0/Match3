[System.Serializable]
public enum TileType 
{
    Immovable = 0, Movable = 1, Breakable = 2
}

[System.Serializable]
public class TileModel: BaseModel 
{
    public TileType Type { get; private set; }
    public Position Position { get; private set; }
    public TileModel(TileType type, Position position) 
    {
        Type = type;
        Position = position;
    }
}

static class TileModelFactory 
{
    public static TileModel Get(TileType tileType, Position position) 
    {
        var tileModel = new TileModel(tileType, position);
        return tileModel;
    }
}