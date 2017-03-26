[System.Serializable]
public enum TileType {
    Nil = 0, Normal = 1, Spawnee = 7, Spawner = 8
}

[System.Serializable]
public class TileModel {
    public TileType type;
    public TileModel(int type) {
        this.type = (TileType)type;
    }
}