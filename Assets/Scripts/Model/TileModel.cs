[System.Serializable]
public enum TileType 
{
    Immovable = 0, Movable = 1, 
    UP, DOWN, LEFT, RIGHT
}

[System.Serializable]
public class TileModel 
{
    public TileType type;
    public int[] gravity;
    public TileModel(TileType type) 
    {
        this.type = type;

        switch (type)
        {
            case TileType.UP:
            gravity = new int[]{ 0, 1 };
            break;

            case TileType.DOWN:
            gravity = new int[]{ 0, -1 };
            break;
            
            case TileType.LEFT:
            gravity = new int[]{ -1, 0 };
            break;
            
            case TileType.RIGHT:
            gravity = new int[]{ 1, 0 };
            break;
        }
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