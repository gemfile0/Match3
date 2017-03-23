using System;

public interface IGemModel
{
    GemType Type { get; set; }
}

[System.Serializable]
public enum GemType {
    Empty = 0, RedGem, BlueGem, GreenGem, PurpleGem, OrangeGem, YellowGem
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
