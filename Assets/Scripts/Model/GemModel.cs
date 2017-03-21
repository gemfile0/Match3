public interface IGemModel
{
    GemType Type { get; set; }
}

public enum GemType {
    Empty = 0, RedGem, BlueGem, GreenGem, PurpleGem, CyanGem, YellowGem
}

[System.Serializable]
public class GemModel: BaseModel {
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
    
    public GemModel(GemType type, Position position) {
        Type = type;
        this.position = position;
    }
}
