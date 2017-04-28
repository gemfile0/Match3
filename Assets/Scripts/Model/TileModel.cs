[System.Serializable]
public enum TileType 
{
    Immovable = 0, Movable = 1
}

[System.Serializable]
public class TileModel 
{
    public TileType type;
    public TileModel(TileType type) 
    {
        this.type = type;
    }
}

static class TileModelFactory 
{
    public static TileModel Get(TileType tileType) 
    {
        var tileModel = new TileModel(tileType);
        return tileModel;
    }
}