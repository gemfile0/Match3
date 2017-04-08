[System.Serializable]
public enum TileType 
{
    Blocked = 0, Empty = 1, ChocoGem = 4, Spawnee = 7, Spawner = 8,
    RedGem = 10, BlueGem = 20, GreenGem = 30, PurpleGem = 40, OrangeGem = 50, YellowGem = 60, 
}

[System.Serializable]
public class TileModel 
{
    public TileType type;
    public TileModel(int type) 
    {
        this.type = (TileType)type;
    }
}