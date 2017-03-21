[System.Serializable]
public class GemModel: BaseModel {
    public string name;
    public Position position;
    
    public GemModel(string name, Position position) {
        this.name = name;
        this.position = position;
    }
}
