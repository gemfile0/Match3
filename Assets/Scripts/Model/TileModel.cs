[System.Serializable]
public enum TileType {
    Nil = 0, Normal
}

[System.Serializable]
public class TileModel {
    public TileType type;
    public TileModel(int type) {
        this.type = (TileType)type;
    }
}