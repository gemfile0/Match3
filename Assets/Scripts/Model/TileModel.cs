[System.Serializable]
public enum TileType {
    Blocked = 0, Empty = 1, ChocoGem = 4, Spawnee = 7, Spawner = 8
}

[System.Serializable]
public class TileModel {
    public TileType type;
    public TileModel(int type) {
        this.type = (TileType)type;
    }
}