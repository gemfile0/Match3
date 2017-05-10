[System.Serializable]
public enum GravityType 
{
    Nil = 0, Up = 1, Down, Left, Right
}

[System.Serializable]
public class GravityModel: BaseModel 
{
    public GravityType Type 
    { 
        get { return type; }
        private set { 
            type = value; 
            Name = value.ToString(); 
        }
    }
    GravityType type;
    public Position Position { get; private set; }
    public string Name { get; private set; }
    public int[] vector;
    public GravityModel(GravityType type, Position position) 
    {
        Type = type;
        Position = position;

        switch (type)
        {
            case GravityType.Nil:
            vector = new int[]{ 0, 0 };
            break; 

            case GravityType.Up:
            vector = new int[]{ 0, 1 };
            break;

            case GravityType.Down:
            vector = new int[]{ 0, -1 };
            break;
            
            case GravityType.Left:
            vector = new int[]{ -1, 0 };
            break;
            
            case GravityType.Right:
            vector = new int[]{ 1, 0 };
            break;
        }
    }
}

static class GravityModelFactory 
{
    public static GravityModel Get(GravityType gravityType, Position position) 
    {
        var gravityModel = new GravityModel(gravityType, position);
        return gravityModel;
    }
}